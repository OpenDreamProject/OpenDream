"""Compile production list ownership and range filtering bodies with inert adapters."""
import argparse
from contextlib import contextmanager
import os
from pathlib import Path
import subprocess
import tempfile
import time


@contextmanager
def temporary_probe():
    temporary = tempfile.TemporaryDirectory(prefix='opendream-memory-lists-')
    try:
        yield temporary.name
    finally:
        # Windows scanners can briefly retain a handle after the probe exits.
        for attempt in range(20):
            try:
                temporary.cleanup()
                break
            except PermissionError:
                if attempt == 19:
                    raise
                time.sleep(0.1)

ROOT = Path(__file__).resolve().parents[2]


def body(source, marker):
    start = source.index(marker)
    opening = source.index('{', start)
    depth = 1
    end = opening + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


STUBS = r'''
using System;
using System.Collections.Generic;
using System.Linq;
public class Ref { public int Count=1; public void IncRef()=>Count++; public void DecRef(){if(--Count<0)throw new Exception("negative refs");} }
public class Appearance : IDisposable { public int Invisibility; public float[] Transform=new float[6]; public void Dispose(){} }
public class AtomManager { public Appearance MustGetAppearance(DreamObjectAtom a)=>a.Appearance; }
public class Definition { public AtomManager AtomManager=new(); }
public class DreamObjectAtom : Ref {
 public Definition ObjectDefinition=new(); public Appearance Appearance=new(); public static int Copies;
 public DreamValue GetVariable(string name){Copies++;return new DreamValue(new Appearance{Invisibility=Appearance.Invisibility});}
}
public class DreamObjectArea : DreamObjectAtom {}
public class Cell { public DreamObjectArea Area=new(); }
public class DreamObjectTurf : DreamObjectAtom {public Cell Cell=new();}
public readonly struct DreamValue : IDisposable {
 public readonly object? Value; public DreamValue(object? v)=>Value=v; public static DreamValue Null=>new(null);
 public void IncRef(){if(Value is Ref r)r.IncRef();} public void DecRef(){if(Value is Ref r)r.DecRef();} public void Dispose()=>DecRef();
 public bool TryGetValueAsInteger(out int n){n=Value is int i?i:0;return Value is int;}
 public bool TryGetValueAsDreamList(out DreamList list){list=(Value as DreamList)!;return list!=null;}
 public bool TryGetValueAsDreamObject<T>(out T obj) where T:class {obj=(Value as T)!;return obj!=null;}
 public Appearance MustGetValueAsAppearance()=>(Appearance)Value!;
}
public class DreamList : Ref {
 private List<DreamValue> _values=new(); private Dictionary<DreamValue,DreamValue>? _associativeValues;
 public bool ContainsValue(DreamValue v)=>_values.Contains(v); public List<DreamValue> GetValues()=>_values;
 public void UpdateTracyContentsMemory(){}
 METHODS
}
public class Program {
 static int checks; static void Check(bool v,string s){checks++;if(!v)throw new Exception(s);}
 static void Main(){
  var a=new Ref();var b=new Ref();var list=new DreamList();list.AddValue(new(a));list.AddValue(new(b));
  for(int i=0;i<1000;i++)list.Swap(1,2);
  Check(a.Count==2&&b.Count==2,"swap must release both owned reads");
  list.Swap(1,1);Check(a.Count==2&&b.Count==2,"same-index swap ownership");
  try{list.Swap(1,9);}catch(ArgumentOutOfRangeException){}
  Check(a.Count==2&&b.Count==2,"failed swap releases first owned read");
  var old=new Ref();var current=new Ref();var dst=new DreamList();var src=new DreamList();
  dst.SetValue(new("key"),new(old));src.SetValue(new("key"),new(current));
  for(int i=0;i<1000;i++)using(var result=dst.OperatorAppend(new(src))){}
  Check(old.Count==1,"append releases replaced association");Check(current.Count==3,"append same association stays balanced");
  using(var result=src.OperatorAppend(new(src))){}
  Check(current.Count==3,"self append stays balanced");
  TestRange(); Console.WriteLine("PASS "+checks+" ownership/filter assertions");
 }
 static void TestRange(){
  var center=new DreamObjectAtom();var rangeList=new DreamList();var seenAreas=new HashSet<DreamObjectArea>();
  RANGE
  var area=new DreamObjectArea();var t1=new DreamObjectTurf();t1.Cell.Area=area;var t2=new DreamObjectTurf();t2.Cell.Area=area;
  t1.Appearance.Invisibility=100;t2.Appearance.Invisibility=101;
  var before=DreamObjectAtom.Copies;AddToList(new(t1));AddToList(new(t2));AddToList(new(t1));
  Check(rangeList.GetValues().Count==3,"range visibility threshold and area deduplication");
  Check(ReferenceEquals(rangeList.GetValues()[0].Value,t1)&&ReferenceEquals(rangeList.GetValues()[1].Value,area),"range ordering");
  Check(DreamObjectAtom.Copies==before,"range filter must not clone appearances");
 }
}
'''


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet', required=True)
    parser.add_argument('--control', choices=['swap', 'append', 'range'])
    parser.add_argument('--baseline', help='Immutable Git revision; must fail expected regression assertions')
    args = parser.parse_args()
    def source(path):
        if args.baseline:
            return subprocess.check_output(['git', 'show', f'{args.baseline}:{path}'], cwd=ROOT, text=True)
        return (ROOT / path).read_text()
    lists = source('OpenDreamRuntime/Objects/Types/DreamList.cs')
    methods = '\n'.join(body(lists, marker) for marker in [
        'public virtual DreamValue GetValue(', 'public virtual void SetValue(',
        'public virtual void AddValue(', 'public void Swap(', 'public override DreamValue OperatorAppend('
    ]).replace('public override DreamValue OperatorAppend', 'public DreamValue OperatorAppend')
    if args.control in ['swap', 'append']:
        marker = 'public void Swap(' if args.control == 'swap' else 'public override DreamValue OperatorAppend('
        baseline = subprocess.check_output(['git', 'show', 'b1a452e7:OpenDreamRuntime/Objects/Types/DreamList.cs'], cwd=ROOT, text=True)
        original = body(lists, marker).replace('public override DreamValue OperatorAppend', 'public DreamValue OperatorAppend')
        restored = body(baseline, marker).replace('public override DreamValue OperatorAppend', 'public DreamValue OperatorAppend')
        methods = methods.replace(original, restored)
    native = source('OpenDreamRuntime/Procs/Native/DreamProcNativeHelpers.cs')
    if args.control == 'range':
        native = subprocess.check_output(['git', 'show', 'b1a452e7:OpenDreamRuntime/Procs/Native/DreamProcNativeHelpers.cs'], cwd=ROOT, text=True)
    filtering = body(body(native, 'public static DreamList HandleRange('), 'void AddToList(')
    with temporary_probe() as temp:
        path = Path(temp)
        (path / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup></Project>')
        (path / 'Program.cs').write_text(STUBS.replace('METHODS', methods).replace('RANGE', filtering))
        env = dict(os.environ, DOTNET_CLI_HOME=str(path / 'cli'), DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
                   DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_TELEMETRY_OPTOUT='1',
                   DOTNET_NOLOGO='1', DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false')
        result = subprocess.run([args.dotnet, 'run', '--project', str(path / 'Probe.csproj'), '-c', 'Release', '--nologo'], env=env, text=True, capture_output=True)
        combined = result.stdout + result.stderr
        if args.baseline or args.control:
            expected = {'swap': 'swap must release both owned reads', 'append': 'append releases replaced association', 'range': 'range filter must not clone appearances'}.get(args.control, 'swap must release both owned reads')
            if result.returncode == 0 or expected not in combined:
                raise RuntimeError('Baseline did not fail expected ownership assertion: ' + combined)
            print('PASS negative control rejected: ' + expected)
        elif result.returncode or 'PASS ' not in combined:
            raise RuntimeError(combined)
        else:
            print(combined.strip())


if __name__ == '__main__':
    main()

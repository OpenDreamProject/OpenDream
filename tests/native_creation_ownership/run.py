"""Dedicated CI-only native runner. Local pure checks never launch the SDK.

Each matrix job owns a fresh checkout. Source variants are applied only there;
all native observations must pass the strict original-log/TRX classifiers.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import xml.etree.ElementTree as ET
import zipfile
import control

ROOT=control.ROOT
HERE=control.HERE
PROJECT='Content.IntegrationTests/Content.IntegrationTests.csproj'
MAX_FILE=32*1024*1024
MAX_ARCHIVE=256*1024*1024
NS={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
ERROR=re.compile(r'\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost[^\n]*(?:crash|exit)|(?:error|warning) (?:CS|DM|MSB|NU)\d+|Build FAILED|Failed to (?:compile|start|launch)',re.I)

def sha(raw):return hashlib.sha256(raw).hexdigest()
def module(name):
    s=importlib.util.spec_from_file_location('ownership_'+name,HERE/name/'verify.py')
    m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def authenticate():
    spec=json.loads((HERE/'source-manifest.json').read_text())
    for name,pin in spec['sources'].items():
        assert sha((ROOT/name).read_bytes().replace(b'\r\n',b'\n'))==pin,'source drift: '+name
    for name,pin in spec['originals'].items():
        raw=(HERE/'original'/name).read_bytes()
        assert sha(raw)==pin['sha256'],'historical source drift'
        assert hashlib.sha1(b'blob '+str(len(raw)).encode()+b'\0'+raw).hexdigest()==pin['git_blob'],'historical Git blob drift'
    return spec

def capture(names,output,category):
    records={}
    for name in names:
        source=ROOT/name
        assert not source.is_symlink() and source.is_file(),name
        target=output/category/name;target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(source,target)
        records[name]={'bytes':target.stat().st_size,'sha256':sha(target.read_bytes())}
    (output/(category+'-sha256.json')).write_text(json.dumps(records,indent=2)+'\n')

def archive(output):
    """ZIP every original loose log/TRX, including failure/partial evidence.

    GitHub upload excludes ignored loose log files; ZIP and receipts preserve
    their byte identity. The bounded archive is not a native success oracle.
    """
    files=sorted(p for p in output.rglob('*') if p.is_file() and p.suffix in ('.log','.trx'))
    records={};total=0
    archive_path=output/'original-logs-trx.zip'
    with zipfile.ZipFile(archive_path,'w',compression=zipfile.ZIP_DEFLATED) as z:
        for p in files:
            assert not p.is_symlink() and p.stat().st_size<=MAX_FILE,'oversized/nonregular original'
            raw=p.read_bytes();total+=len(raw);assert total<=MAX_ARCHIVE,'archive cap'
            name=p.relative_to(output).as_posix();z.writestr(name,raw)
            records[name]={'bytes':len(raw),'sha256':sha(raw)}
    receipt={'schema':1,'files':records,'zip_sha256':sha(archive_path.read_bytes())}
    (output/'original-logs-trx.json').write_text(json.dumps(receipt,indent=2)+'\n')
    with zipfile.ZipFile(archive_path) as z:
        assert z.namelist()==list(records)
        for name,record in records.items():
            raw=z.read(name)
            assert len(raw)==record['bytes'] and sha(raw)==record['sha256']
            assert raw==(output/name).read_bytes(),'original changed during archive'
    return receipt

def ordinary(raw,log,code):
    assert code==0 and len(raw)<=MAX_FILE and len(log)<=MAX_FILE
    root=ET.fromstring(raw);rows=root.findall('.//t:UnitTestResult',NS);defs=root.findall('.//t:TestDefinitions/t:UnitTest',NS)
    assert len(rows)==len(defs)==1
    row=rows[0];d=defs[0];method=d.find('t:TestMethod',NS)
    name='NoRuntimesTest';cls='Content.IntegrationTests.GameTests'
    assert row.get('testName')==name and row.get('outcome')=='Passed' and row.get('testId')==d.get('id')
    assert method is not None and method.get('className')==cls and method.get('name')==name
    counters=root.findall('.//t:ResultSummary/t:Counters',NS);assert len(counters)==1
    c=counters[0]
    assert all(c.get(k)==v for k,v in {'total':'1','executed':'1','passed':'1','failed':'0'}.items())
    known={'total','executed','passed','failed','completed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','warning','pending','inProgress','passedButRunAborted'}
    assert set(c.attrib)<=known
    assert all(v=='0' for k,v in c.attrib.items() if k not in ('total','executed','passed','failed','completed'))
    assert c.get('completed','0') in ('0','1')
    assert not root.findall('.//t:ErrorInfo',NS) and not root.findall('.//t:RunInfo',NS)
    combined=log+'\n'+'\n'.join(root.itertext())
    assert not ERROR.search(combined)
    assert 'Test Run Successful.' in log and re.search(r'\bPassed NoRuntimesTest(?:\s|\[)',log)
    assert not any((node.text or '').strip() for node in root.findall('.//t:StdErr',NS))
    return True

def command(args,path,env,timeout=900):
    # Absolute SDK path is supplied by setup-dotnet; argument arrays never use a shell.
    record={'argv':args,'exit':None,'timeout':False}
    try:
        with path.open('wb') as stream:
            process=subprocess.Popen(args,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
            try:record['exit']=process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                record['timeout']=True
                os.killpg(process.pid,signal.SIGTERM)
                try:process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid,signal.SIGKILL);process.wait(timeout=10)
                record['exit']=process.returncode
                raise
    finally:
        (path.with_suffix('.exit.json')).write_text(json.dumps(record)+'\n')
    return record['exit']

def test(dotnet,output,kind,mode,env):
    names={'resource':('SavefileResourceBytesTests','ResourceIdentityAndSharedBytes'),
           'matrix':('NativeMatrixOwnershipTests','FastRejectionsAndTransfers'),
           'constructors':('NativeMatrixOwnershipTests','ConstructorModePreservesRuntimeDelivery'),
           'typesof':('TypesOfOwnershipTests','RejectedTypesOfReleasesCreator'),
           'runtime':('GameTests','NoRuntimesTest')}
    cls,name=names[kind]
    # Preserve the authenticated typesof adapter's exact results boundary.
    directory=output/'typesof-results' if kind=='typesof' else output/kind
    directory.mkdir()
    path=directory/(kind+'.trx');logpath=directory/(kind+'.log')
    code=command([dotnet,'test',PROJECT,'--configuration','Release','--no-build','--filter',
        'FullyQualifiedName=Content.IntegrationTests.'+cls+'.'+name,
        '--logger','trx;LogFileName='+path.name,'--results-directory',str(directory),'-v','n'],logpath,env)
    raw=path.read_text(encoding='utf-8-sig');log=logpath.read_text(encoding='utf-8',errors='strict')
    root=ET.fromstring(raw)
    assert not any((node.text or '').strip() for node in root.findall('.//t:StdErr',NS)) and not root.findall('.//t:RunInfo',NS),'unexpected runner diagnostics'
    counters=root.findall('.//t:ResultSummary/t:Counters',NS);assert len(counters)==1
    known={'total','executed','passed','failed','completed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','warning','pending','inProgress','passedButRunAborted'}
    assert set(counters[0].attrib)<=known,'unknown runner counter'
    assert all(value=='0' for key,value in counters[0].attrib.items() if key not in ('total','executed','passed','failed','completed'))
    if kind=='resource':module('resource').verify(raw,log,'baseline' if mode=='resource-baseline' else mode if mode=='hash-only' else 'positive',code)
    elif kind=='typesof':module('typesof').verify(path,'baseline' if mode=='typesof-baseline' else 'positive',code,log)
    elif kind in ('matrix','constructors'):
        module('matrix').classify(raw,log,code,'baseline' if mode=='matrix-baseline' else mode if mode in ('omit-creator','omit-arguments') else 'positive',kind=='constructors')
    else:ordinary(raw,log,code)
    (directory/'accepted.json').write_text(json.dumps({'mode':mode,'test':cls+'.'+name,'exit':code,'trx_sha256':sha(path.read_bytes()),'log_sha256':sha(logpath.read_bytes())})+'\n')

def run(dotnet,mode,output):
    assert os.environ.get('GITHUB_ACTIONS')=='true','native runner requires a fresh GitHub Actions checkout; local execution is disabled'
    assert os.name=='posix','registered native runner requires Linux process-group cleanup'
    assert Path(dotnet).is_absolute() and Path(dotnet).is_file(),'absolute setup-dotnet path required'
    assert not output.exists(),'fresh output directory required'
    assert subprocess.check_output(['git','status','--porcelain'],cwd=ROOT).strip()==b'','clean CI checkout required'
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT).decode().strip()
    assert head==os.environ['GITHUB_SHA'],'GitHub checkout revision mismatch'
    spec=authenticate();output.mkdir(parents=True)
    env=dict(os.environ,COMPlus_gcServer='1')
    if mode in ('matrix-baseline','omit-creator','omit-arguments'):
        env['MATRIX_OWNERSHIP_MODE']='baseline' if mode=='matrix-baseline' else mode
    else:env.pop('MATRIX_OWNERSHIP_MODE',None)
    receipt={'head':head,'github_sha':os.environ['GITHUB_SHA'],'pr_head':os.environ.get('OWNERSHIP_PR_HEAD'),
             'base_head':spec['base_head'],'mode':mode,'native':'pending'}
    (output/'run.json').write_text(json.dumps(receipt,indent=2)+'\n')
    try:
        source={p:(ROOT/p).read_bytes() for p in control.SPEC['candidate_sources']}
        for name,raw in control.mutate(source,mode).items():(ROOT/name).write_bytes(raw)
        capture(spec['sources'],output,'prepared-source')
        for name in ('controls.json','source-manifest.json'):
            shutil.copyfile(HERE/name,output/name)
        assert command([dotnet,'--info'],output/'sdk-info.log',env,60)==0
        assert command(['git','submodule','status','--recursive'],output/'submodules.log',env,60)==0
        assert command([dotnet,'build',PROJECT,'--configuration','Release','--property','WarningLevel=4'],output/'build.log',env)==0,'build failed'
        assert not ERROR.search((output/'build.log').read_text()),'build diagnostics'
        capture(('bin/Content.IntegrationTests/Content.IntegrationTests.dll','bin/Content.IntegrationTests/Content.IntegrationTests.pdb',
                 'OpenDreamRuntime/bin/Release/net10.0/OpenDreamRuntime.dll','OpenDreamRuntime/bin/Release/net10.0/OpenDreamRuntime.pdb',
                 'bin/DMCompiler/DMCompiler.dll'),output,'binaries')
        kinds=['resource','matrix','typesof','constructors'] if mode=='positive' else ['resource' if mode in ('resource-baseline','hash-only') else 'typesof' if mode=='typesof-baseline' else 'matrix']
        for kind in kinds:test(dotnet,output,kind,mode,env)
        # Independent testhost process on every source variant, unchanged game project.
        test(dotnet,output,'runtime',mode,env)
        receipt['native']='accepted by strict original log/TRX classifiers'
    except BaseException as error:
        receipt['native']='rejected or incomplete'
        receipt['failure_type']=type(error).__name__
        receipt['failure']=str(error)
        raise
    finally:
        (output/'run.json').write_text(json.dumps(receipt,indent=2)+'\n')
        archive(output)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--dotnet',required=True);p.add_argument('--mode',required=True,choices=control.SPEC['modes']);p.add_argument('--output',required=True,type=Path)
    a=p.parse_args();run(a.dotnet,a.mode,a.output.resolve())

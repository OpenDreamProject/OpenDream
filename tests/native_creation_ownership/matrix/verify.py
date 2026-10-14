"""Fail-closed evidence parser; synthetic parser tests are not runtime proof."""
import argparse,re,xml.etree.ElementTree as ET
from pathlib import Path
NS={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
ERROR=re.compile(r'\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost[^\n]*(?:crash|exit)|MATRIX_COMPILE:|(?:error|warning) (?:CS|DM|MSB)\d+|Build FAILED|Failed to (?:compile|start|launch)',re.I)
def classify(raw,log,code,mode,constructor=False):
 assert mode in ('positive','baseline','omit-creator','omit-arguments') and code==0
 assert len(raw)<=8*1024*1024 and len(log)<=8*1024*1024
 root=ET.fromstring(raw);results=root.findall('t:Results/t:UnitTestResult',NS);definitions=root.findall('t:TestDefinitions/t:UnitTest',NS)
 assert len(results)==len(definitions)==1
 r=results[0];d=definitions[0];method=d.find('t:TestMethod',NS)
 name='ConstructorModePreservesRuntimeDelivery' if constructor else 'FastRejectionsAndTransfers'
 assert r.get('outcome')=='Passed' and r.get('testName')==name and r.get('testId')==d.get('id')
 assert method is not None and method.get('className')=='Content.IntegrationTests.NativeMatrixOwnershipTests' and method.get('name')==name
 counters=root.find('t:ResultSummary/t:Counters',NS);assert counters is not None
 for k,v in {'total':1,'executed':1,'passed':1,'failed':0}.items():assert int(counters.get(k,'-1'))==v
 for k in ('error','timeout','aborted','inconclusive','notRunnable','disconnected','warning'):assert int(counters.get(k,'0'))==0
 assert not root.findall('.//t:ErrorInfo',NS) and not root.findall('t:ResultSummary/t:RunInfos/t:RunInfo[@outcome="Error"]',NS)
 stdout=r.findtext('t:Output/t:StdOut',default='',namespaces=NS)
 assert not ERROR.search(log+'\n'+'\n'.join(root.itertext()))
 if constructor:
  marker='MATRIX_CONSTRUCTORS_COMPLETE new=1 error=1 partial=1';assert stdout.count(marker)==1
  diagnostic='MATRIX_EXPECTED_RUNTIME: Invalid arguments used to create matrix null 2 3 4 5 6'
  # Genuine world.Error delivery must be in the original engine runner log.
  assert log.count(diagnostic)==1
  assert len(re.findall(r'MATRIX_EXPECTED_RUNTIME:',log))==1
 else:
  rows=re.findall(r'(?m)^MATRIX_CASE (null|string|datum|list) ([0-6]) matrices=(\d+) expected=(\d+) owner=(\d+) expectedOwner=(\d+)\s*$',stdout)
  assert len(rows)==28
  identities=[(k,str(p)) for k in ('null','string','datum','list') for p in range(7)]
  assert [(r[0],r[1]) for r in rows]==identities
  for kind,pos,matrices,expected,owner,expected_owner in rows:
   m=100 if mode in ('baseline','omit-creator') else 0
   o=0 if kind in ('null','string') else 1+(100*(6 if pos=='0' else 1) if mode in ('baseline','omit-arguments') else 0)
   assert int(matrices)==int(expected)==m and int(owner)==int(expected_owner)==o
  generic=re.findall(r'(?m)^MATRIX_GENERIC (datum|list) owner=(\d+) expected=(\d+)\s*$',stdout)
  assert len(generic)==2 and [r[0] for r in generic]==['datum','list']
  expected=201 if mode in ('baseline','omit-arguments') else 1
  assert all(int(r[1])==int(r[2])==expected for r in generic)
  assert stdout.count(f'MATRIX_FAST_COMPLETE mode={mode} cases=28 iterations=100 generic=2 success=1')==1
 return True
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('trx',type=Path);p.add_argument('mode');p.add_argument('exit',type=int);p.add_argument('--log',type=Path,required=True);p.add_argument('--constructors',action='store_true');a=p.parse_args();classify(a.trx.read_text(),a.log.read_text(),a.exit,a.mode,a.constructors)

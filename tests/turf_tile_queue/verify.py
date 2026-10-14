"""Classify original dedicated fixture TRX and logs without rewriting evidence."""
import argparse,re,xml.etree.ElementTree as ET
from pathlib import Path
NS={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
CLASS='Content.IntegrationTests.TurfTileQueueTests'
TEST='PendingTilesPreserveEmptyTransitions'
ERROR=re.compile(r'\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost.*(?:crash|exit)|Isolated DM fixture compilation failed|error (?:CS|MSB|NU)\d+',re.I)
def verify(raw,log,mode,exit_code):
    assert mode in ('positive','baseline','unsafe-dedup')
    tree=ET.fromstring(raw);rows=tree.findall('.//t:UnitTestResult',NS)
    assert len(rows)==1 and rows[0].get('testName')==TEST
    row=rows[0];definitions=tree.findall('.//t:TestDefinitions/t:UnitTest',NS)
    assert len(definitions)==1 and definitions[0].get('id')==row.get('testId')
    method=definitions[0].find('t:TestMethod',NS)
    assert method is not None and method.get('className')==CLASS and method.get('name')==TEST
    counts=tree.find('t:ResultSummary/t:Counters',NS);assert counts is not None
    assert counts.get('total')==counts.get('executed')=='1'
    for key,value in counts.attrib.items():
        if key in ('total','executed','passed','failed'): continue
        assert key in ('completed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','warning','pending','inProgress','passedButRunAborted') and value=='0'
    assert not tree.findall('.//t:RunInfo',NS)
    assert not ERROR.search(log+'\n'+'\n'.join(tree.itertext()))
    stdout=row.findtext('t:Output/t:StdOut','',NS)
    markers=[line.strip() for line in stdout.splitlines() if 'TILE_QUEUE_' in line]
    peak,retained,area=(20000,32768,4096) if mode=='baseline' else (1,0,512)
    empty,barriers=('False','False') if mode=='unsafe-dedup' else ('True','True')
    assert markers==[
        f'TILE_QUEUE_OBSERVATION: repeated=60000 peak={peak} retained={retained} area_peak={area}',
        f'TILE_QUEUE_PARITY: anchored_empty={empty} baseline_empty=True barriers={barriers}',
        'TILE_QUEUE_COMPLETE: production routes, real grids, transforms, levels, drains and final tiles checked']
    passed=mode=='positive';outcome='Passed' if passed else 'Failed'
    assert row.get('outcome')==outcome and exit_code==(0 if passed else 1)
    assert counts.get('passed')==('1' if passed else '0') and counts.get('failed')==('0' if passed else '1')
    assert re.search(r'\b'+outcome+r' '+TEST+r'(?:\s|\[)',log)
    assert ('Test Run Successful.' if passed else 'Test Run Failed.') in log
    errors=tree.findall('.//t:ErrorInfo',NS)
    if passed: assert not errors
    else:
        assert len(errors)==1
        message=row.findtext('t:Output/t:ErrorInfo/t:Message','',NS)
        expected=('TILE_QUEUE_BOUNDED','TILE_QUEUE_RELEASED','TILE_QUEUE_AREA_BOUNDED') if mode=='baseline' else ('TILE_QUEUE_EMPTY_BARRIERS',)
        found=re.findall(r'TILE_QUEUE_[A-Z_]+',message)
        assert tuple(found)==expected, 'foreign or missing negative-control assertion'
        assert re.findall(r'Expected:\s*(\w+)\s+But was:\s*(\w+)',message)==([('1','20000'),('0','32768'),('512','4096')] if mode=='baseline' else [('True','False')])
        stack=row.findtext('t:Output/t:ErrorInfo/t:StackTrace','',NS)
        assert CLASS+'.Check' in stack
        assert 'TurfTileQueueTests.cs' in stack
        assert not re.search(r'(?i)InvalidOperationException|NullReferenceException|System\.IO|TargetInvocationException',message+stack)
    return True
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('trx',type=Path);p.add_argument('mode');p.add_argument('exit_code',type=int);p.add_argument('--log',type=Path,required=True);a=p.parse_args();verify(a.trx.read_text(encoding='utf-8-sig'),a.log.read_text(),a.mode,a.exit_code)

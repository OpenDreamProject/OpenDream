"""Classify original TRX and raw output conservatively, including named controls."""
import argparse
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

NS = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
CLASS = 'Content.IntegrationTests.ResourceOutputAllocationTests'
TESTS = {'AllocationWorkloads','BytesErrorsCache','SavefileRoundTrip'}

def verify(trx, log, mode, exit_code, output):
    assert len(trx) <= 32*1024*1024 and len(log) <= 32*1024*1024
    assert not re.search(r'\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost.*(?:crash|exit)|error (?:CS|MSB|NU)\d+',log,re.I)
    tree = ET.fromstring(trx)
    rows = tree.findall('.//t:UnitTestResult',NS)
    assert len(rows)==3 and {r.attrib['testName'] for r in rows}==TESTS
    definitions=tree.findall('.//t:UnitTest/t:TestMethod',NS)
    assert len(definitions)==3 and all(d.attrib['className']==CLASS for d in definitions)
    units = tree.findall('.//t:TestDefinitions/t:UnitTest',NS)
    assert len(units)==3 and {d.find('t:TestMethod',NS).get('name') for d in units}==TESTS
    by_id = {d.get('id'):d for d in units}
    assert len(by_id)==3 and all(r.get('testId') in by_id and by_id[r.get('testId')].find('t:TestMethod',NS).get('name')==r.get('testName') for r in rows)
    assert not tree.findall('.//t:RunInfo',NS)
    assert not any((n.text or '').strip() for n in tree.findall('.//t:StdErr',NS))
    assert not re.search(r'\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost.*(?:crash|exit)|(?:error|warning) (?:CS|DM|MSB|NU)\d+', '\n'.join(tree.itertext())+'\n'+log,re.I)
    failures = {'positive':{},'baseline':{'AllocationWorkloads':'OUTPUT_ALLOCATION_SCALING'},
                'platform-newline':{'BytesErrorsCache':'OUTPUT_BYTES_CRLF','SavefileRoundTrip':'OUTPUT_BYTES_CRLF'},
                'no-small-path':{'AllocationWorkloads':'OUTPUT_SMALL_ALLOCATION'}}[mode]
    counters=tree.findall('.//t:ResultSummary/t:Counters',NS);assert len(counters)==1
    counts=counters[0]
    assert counts.get('total')==counts.get('executed')=='3'
    assert counts.get('passed')==str(3-len(failures)) and counts.get('failed')==str(len(failures))
    for key,value in counts.attrib.items():
        if key in ('total','executed','passed','failed'):continue
        assert key in ('completed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','warning','pending','inProgress','passedButRunAborted')
        assert value=='0' or (key=='completed' and value=='3')
    assert exit_code==(1 if failures else 0)
    assert ('Test Run Failed.' if failures else 'Test Run Successful.') in log
    assert len(tree.findall('.//t:ErrorInfo',NS))==len(failures)
    for row in rows:
        name=row.attrib['testName']; failed=name in failures
        assert row.attrib['outcome']==('Failed' if failed else 'Passed')
        if failed:
            message=row.findtext('t:Output/t:ErrorInfo/t:Message','',NS)
            stack=row.findtext('t:Output/t:ErrorInfo/t:StackTrace','',NS)
            assert failures[name] in message and 'Assert.That(' in message and 'Expected:' in message and 'But was:' in message
            assert CLASS in stack and name in stack
            assert not re.search(r'Exception|timeout|Multiple failures',message,re.I)
    bench = next(r for r in rows if r.attrib['testName']=='AllocationWorkloads')
    stdout=bench.findtext('t:Output/t:StdOut','',NS)
    metrics=[json.loads(s) for s in re.findall(r'OUTPUT_ALLOCATION_METRIC (\{[^\r\n]+\})',stdout)]
    assert [m['chars'] for m in metrics]==[16,1024,4096,4097,1048576,8388608]
    assert all(m['count']==8 and m['allocated']>0 and m['seconds']>=0 and len(m['collections'])==3 for m in metrics)
    assert all(m['allocated']/8 < 2*m['chars']+1024 for m in metrics if m['chars']<=4096)==(mode!='no-small-path')
    assert (metrics[-1]['allocated']/8 >= 8388608)==(mode=='baseline')
    (output/'verified-metrics.json').write_text(json.dumps({'mode':mode,'metrics':metrics,'expected_failures':failures},indent=2))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('mode');a=p.parse_args()
    verify((a.output/'output.trx').read_text(encoding='utf-8-sig'),(a.output/'output.log').read_text(),a.mode,
           int((a.output/'test-exit-code.txt').read_text()),a.output)

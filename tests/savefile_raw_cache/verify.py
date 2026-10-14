"""Strict original native TRX/log classification; no native execution."""
import argparse
from pathlib import Path
import re
import xml.etree.ElementTree as ET

NS={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
CLASS='Content.IntegrationTests.SavefileRawCacheTests'
METHOD='DiskReadLifetimeAndIdentity'
BAD=re.compile(r'\[ERRO\]|Unhandled exception|OutOfMemoryException|runtime error|test run aborted|testhost.*(?:crash|exit)',re.I)
ROWS={'valid-open':1,'valid-close':1,'malformed-unique':16,'malformed-repeat':1,'deleted-close':1,'serialize-failure':1,'append-failure':1}

def frames(stack):
    assert stack.strip(), 'missing fixture stack'
    assert CLASS+'.Check' in stack and 'Content.IntegrationTests/SavefileRawCacheTests.cs' in stack
    allowed = (
        re.escape(CLASS)+r'\.Check\([^\r\n]*\)',
        re.escape(CLASS)+r'\.DiskReadLifetimeAndIdentity\(\)',
        re.escape(CLASS)+r'\.<>c__DisplayClass\d+_\d+\.<(?:Check|DiskReadLifetimeAndIdentity)>b__\d+\(\)',
        r'NUnit\.Framework\.Assert\.Multiple\(TestDelegate testDelegate\)',
        r'Robust\.UnitTesting\.RobustIntegrationTest\.IntegrationGameLoop\.(?:SingleThreadRunUntilEmpty|Run)\(\)',
        r'Robust\.UnitTesting\.RobustIntegrationTest\.ServerIntegrationInstance\._serverMain\(\)',
        r'System\.Threading\.Thread\.StartHelper\.Callback\(Object state\)',
        r'Robust\.UnitTesting\.RobustIntegrationTest\.IntegrationInstance\.WaitIdleImplAsync\(Boolean throwOnUnhandled, CancellationToken cancellationToken\)',
        r'Robust\.UnitTesting\.RobustIntegrationTest\.IntegrationInstance\.WaitAssertion\(Action assertion\)',
    ) + tuple(re.escape(frame) for frame in (
        'NUnit.Framework.Internal.TaskAwaitAdapter.GenericAdapter`1.BlockUntilCompleted()',
        'NUnit.Framework.Internal.MessagePumpStrategy.NoMessagePumpStrategy.WaitForCompletion(AwaitAdapter awaiter)',
        'NUnit.Framework.Internal.AsyncToSyncAdapter.Await[TResult](TestExecutionContext context, Func`1 invoke)',
        'NUnit.Framework.Internal.AsyncToSyncAdapter.Await(TestExecutionContext context, Func`1 invoke)',
        'NUnit.Framework.Internal.Commands.TestMethodCommand.RunTestMethod(TestExecutionContext context)',
        'NUnit.Framework.Internal.Commands.TestMethodCommand.Execute(TestExecutionContext context)',
        'NUnit.Framework.Internal.Execution.SimpleWorkItem.<>c__DisplayClass3_0.<PerformWork>b__0()',
        'NUnit.Framework.Internal.ContextUtils.<>c__DisplayClass1_0`1.<DoIsolated>b__0(Object _)',
    ))
    numbered = 0
    for line in stack.splitlines():
        if not line.strip(): continue
        if line.strip() == '--- End of stack trace from previous location ---': continue
        number = re.match(r'^(\d+)\)\s+(?=at )', line)
        if number:
            numbered += 1
            assert numbered == 1 and number[1] == '1', 'unexpected numbered assertion stack'
            line = line[number.end():]

        match=re.fullmatch(r'\s*at (.+?)(?: in (.+):line \d+)?\s*',line)
        assert match and any(re.fullmatch(pattern,match[1]) for pattern in allowed), 'foreign stack frame'
        if match[2]:
            path=match[2].replace('\\','/')
            assert path.endswith(('Content.IntegrationTests/SavefileRawCacheTests.cs','RobustToolbox/Robust.UnitTesting/RobustIntegrationTest.cs')), 'foreign source frame'

def verify(path,mode,exit_code,log):
    assert mode in ('positive','baseline','omit-release','drop-inmemory')
    root=ET.parse(path).getroot();rows=root.findall('.//t:UnitTestResult',NS);defs=root.findall('.//t:TestDefinitions/t:UnitTest',NS)
    assert len(rows)==len(defs)==1
    row=rows[0];definition=defs[0];method=definition.find('t:TestMethod',NS)
    assert row.get('testName')==METHOD and row.get('testId')==definition.get('id')
    assert method is not None and method.get('className')==CLASS and method.get('name')==METHOD
    counters=root.find('.//t:ResultSummary/t:Counters',NS);assert counters is not None
    assert counters.get('total')==counters.get('executed')=='1'
    for key in ('error','timeout','aborted','inconclusive','notRunnable','disconnected','warning','notExecuted'):
        assert int(counters.get(key,'0'))==0
    for key,value in counters.attrib.items():
        if key not in ('total','executed','passed','failed'):
            assert key in ('completed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','warning','pending','inProgress','passedButRunAborted') and value=='0'
    assert not root.findall('.//t:RunInfo',NS)
    combined=log+'\n'+'\n'.join(root.itertext());assert not BAD.search(combined)
    stdout=row.findtext('t:Output/t:StdOut','',NS)
    observations=re.findall(r'^RAW_CACHE_ROW (\S+) bytes=(\d+) expected=(\d+) paths=(\d+)\s*$',stdout,re.M)
    assert len(observations)==len(ROWS) and {r[0] for r in observations}==set(ROWS)
    assert len(re.findall(r'^RAW_CACHE_ROW\b',stdout,re.M))==len(observations)
    retain=mode in ('baseline','omit-release')
    for name,actual,expected,paths in observations:
        assert actual==expected and int(paths)==ROWS[name]
        n=int(actual)
        if name=='malformed-unique': assert n==(1048576 if retain else 0)
        elif name=='malformed-repeat': assert n==(65536 if retain else 0)
        elif name=='valid-close': assert n==0
        else: assert (n>0)==retain and n<=65536
    errors=root.findall('.//t:ErrorInfo',NS)
    if mode=='drop-inmemory':
        assert exit_code==1 and row.get('outcome')=='Failed' and len(errors)==1
        assert counters.get('failed')=='1' and counters.get('passed')=='0'
        message=errors[0].findtext('t:Message','',NS)
        parts=re.split(r'\r?\n\s*at ',message,maxsplit=1)
        assertion=r'RAW_CACHE_EMBEDDED_UNCHANGED\s+Assert\.That\(embedded\.ResourceData, Is\.SameAs\(embeddedBytes\)\)\s+Expected: same as < 1, 2, 3 >\s+But was:  null'
        assert re.fullmatch(r'(?:Multiple failures or warnings in test:\s*1\)\s*)?'+assertion+r'\s*',parts[0].strip()),message
        if len(parts)==2:frames('at '+parts[1])
        frames(errors[0].findtext('t:StackTrace','',NS))
        assert 'Failed '+METHOD in log and 'Test Run Failed.' in log
        if 'Build FAILED.' in log:
            assert log.count('Build FAILED.')==1 and re.search(r'Build FAILED\.\s+0 Warning\(s\)\s+0 Error\(s\)\s+Time Elapsed [\d:.]+\s*$',log)
        assert 'RAW_CACHE_COMPLETE' not in stdout
    else:
        assert exit_code==0 and row.get('outcome')=='Passed' and not errors
        assert counters.get('failed')=='0' and counters.get('passed')=='1'
        assert 'Passed '+METHOD in log and 'Test Run Successful.' in log and 'Build FAILED.' not in log
        completions=re.findall(r'^RAW_CACHE_COMPLETE mode=(\S+) unique=16 size=65536 buckets=clean identity=preserved\s*$',stdout,re.M)
        assert completions==[mode]
    return True

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('trx',type=Path);p.add_argument('mode');p.add_argument('exit_code',type=int);p.add_argument('--log',type=Path,required=True);a=p.parse_args();assert verify(a.trx,a.mode,a.exit_code,a.log.read_text());print('raw cache strict native classification passed')

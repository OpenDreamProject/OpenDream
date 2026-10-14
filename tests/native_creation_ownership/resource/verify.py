"""Conservative source-only classifier. New native message spellings fail closed."""
import argparse
from pathlib import Path
import re
import xml.etree.ElementTree as ET

NS={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
CLASS='Content.IntegrationTests.SavefileResourceBytesTests'
TEST='ResourceIdentityAndSharedBytes'
COMPLETE='RESOURCE_BYTES_COMPLETE: identities, lookups, downloads, contents and cleanup checked'
ERROR=re.compile(r'\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost.*(?:crash|exit)|Isolated DM fixture compilation failed|error (?:CS|MSB|NU)\d+',re.I)

def frames(stack):
    assert stack.strip(), 'missing fixture stack'
    assert CLASS+'.CheckResources' in stack and 'Content.IntegrationTests/SavefileResourceBytesTests.cs' in stack
    allowed = (
        re.escape(CLASS)+r'\.CheckResources\([^\r\n]*\)',
        re.escape(CLASS)+r'\.ResourceIdentityAndSharedBytes\(\)',
        re.escape(CLASS)+r'\.<>c__DisplayClass\d+_\d+\.<(?:CheckResources|ResourceIdentityAndSharedBytes)>b__\d+\(\)',
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
            assert path.endswith(('Content.IntegrationTests/SavefileResourceBytesTests.cs','RobustToolbox/Robust.UnitTesting/RobustIntegrationTest.cs')), 'foreign source frame'

def verify(raw, log, mode, exit_code):
    assert mode in ('positive','baseline','hash-only')
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
        assert key in ('completed','error','timeout','aborted','inconclusive','notRunnable','notExecuted','disconnected','warning','pending','inProgress','passedButRunAborted') and value=='0', 'unknown/nonzero runner counter'
    assert not tree.findall('.//t:RunInfo',NS), 'runner diagnostics'
    assert not ERROR.search(log+'\n'+'\n'.join(tree.itertext()))
    stdout=row.findtext('t:Output/t:StdOut','',NS)
    arrays=3000 if mode=='baseline' else 1
    expected=f'RESOURCE_BYTES_OBSERVATION: reads=3000 arrays={arrays} bytes={arrays*65536} ids=3000'
    collision={'positive':'checked','baseline':'historical','hash-only':'mismatch'}[mode]
    markers=[expected,'RESOURCE_BYTES_COLLISION: '+collision,COMPLETE]
    actual=[line.strip() for line in stdout.splitlines() if 'RESOURCE_BYTES_' in line]
    assert actual==markers, 'missing/duplicate/changed fixture observation'
    passed=mode=='positive';outcome='Passed' if passed else 'Failed'
    assert row.get('outcome')==outcome and exit_code==(0 if passed else 1)
    assert counts.get('passed')==('1' if passed else '0') and counts.get('failed')==('0' if passed else '1')
    assert re.search(r'\b'+outcome+r' '+TEST+r'(?:\s|\[)',log)
    assert ('Test Run Successful.' if passed else 'Test Run Failed.') in log
    errors=tree.findall('.//t:ErrorInfo',NS)
    if passed:
        assert not errors and 'Build FAILED.' not in log
    else:
        assert len(errors)==1
        message=row.findtext('t:Output/t:ErrorInfo/t:Message','',NS)
        # Do not erase arbitrary Build FAILED text. Only VSTest's exact zero-error footer is harmless.
        if 'Build FAILED.' in log:
            assert log.count('Build FAILED.')==1 and re.search(r'Build FAILED\.\s+0 Warning\(s\)\s+0 Error\(s\)\s+Time Elapsed [\d:.]+\s*$',log)
        marker,expression,expected_value,actual_value=('RESOURCE_BYTES_SHARED','arrays, Is.EqualTo(1)','1','3000') if mode=='baseline' else ('RESOURCE_BYTES_COLLISION_SAFE','collisionCorrect, Is.True','True','False')
        assertion=re.escape(marker)+r'\s+Assert\.That\('+re.escape(expression)+r'\)\s+Expected:\s*'+expected_value+r'\s+But was:\s*'+actual_value
        # One failed assertion only; embedded stacks are classified separately.
        parts=re.split(r'\r?\n\s*at ',message,maxsplit=1)
        plain=parts[0].strip()
        assert re.fullmatch(r'(?:Multiple failures or warnings in test:\s*1\)\s*)?'+assertion+r'\s*',plain), 'unexpected assertion'
        if len(parts)==2: frames('at '+parts[1])
        frames(row.findtext('t:Output/t:ErrorInfo/t:StackTrace','',NS))
    return True

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('trx',type=Path);p.add_argument('mode',choices=['positive','baseline','hash-only']);p.add_argument('exit_code',type=int);p.add_argument('--log',type=Path,required=True);a=p.parse_args()
    verify(a.trx.read_text(encoding='utf-8-sig'),a.log.read_text(),a.mode,a.exit_code)

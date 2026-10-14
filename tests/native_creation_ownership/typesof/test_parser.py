"""Synthetic parser controls only. These do not compile or execute the engine."""
import copy
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
import verify as V
TAG = lambda name: '{' + V.NS['t'] + '}' + name
FRAME = 'at Content.IntegrationTests.TypesOfOwnershipTests.<>c__DisplayClass2_0.<RejectedTypesOfReleasesCreator>b__1() in /repo/Content.IntegrationTests/TypesOfOwnershipTests.cs:line 51'

def observations(mode):
    extra = 1000 if mode == 'baseline' else 0
    lines = [V.SUCCESS]
    for case in V.SPEC:
        lines.append(f'{case['id']}: owners=[1,1,1]; new={extra}; ref1={extra}; content={extra}; lists={11 + extra} expected=11; datums=22 expected=22')
    if mode == 'positive':
        lines.append(V.MARKER)
    return '\n'.join(lines)

def aggregate(embedded=False):
    blocks = [f'{i + 1}) {case['id']}_UNRETURNED_LISTS_1000\nAssert.That(fresh.Length, Is.EqualTo(0))\nExpected: 0\nBut was: 1000\n' + (FRAME + '\n' if embedded else '') for i, case in enumerate(V.SPEC)]
    return 'Multiple failures or warnings in test:\n' + '\n'.join(blocks)

def evidence(mode='positive', embedded=False):
    root = ET.Element(TAG('TestRun'))
    result = ET.SubElement(ET.SubElement(root, TAG('Results')), TAG('UnitTestResult'), testId='only', testName=V.TEST, outcome='Passed' if mode == 'positive' else 'Failed')
    out = ET.SubElement(result, TAG('Output'))
    stdout = observations(mode)
    ET.SubElement(out, TAG('StdOut')).text = stdout
    message = aggregate(embedded)
    if mode == 'baseline':
        error = ET.SubElement(out, TAG('ErrorInfo'))
        ET.SubElement(error, TAG('Message')).text = message
        ET.SubElement(error, TAG('StackTrace')).text = FRAME
    definition = ET.SubElement(ET.SubElement(root, TAG('TestDefinitions')), TAG('UnitTest'), id='only')
    ET.SubElement(definition, TAG('TestMethod'), className=V.CLASS, name=V.TEST)
    ET.SubElement(ET.SubElement(root, TAG('ResultSummary')), TAG('Counters'), total='1', executed='1', passed='1' if mode == 'positive' else '0', failed='0' if mode == 'positive' else '1', completed='0')
    details = '  Error Message:\n' + message + '\n  Stack Trace:\n' + FRAME + '\n' if mode == 'baseline' else ''
    log = details + stdout + f'\n{('Passed' if mode == 'positive' else 'Failed')} {V.TEST} [100 ms]\nTest Run {('Successful' if mode == 'positive' else 'Failed')}.\n'
    return (ET.ElementTree(root), log)

class ParserControls(unittest.TestCase):

    def check(self, tree, log, mode='positive', code=None):
        with tempfile.TemporaryDirectory(prefix='typesof-parser-pure-') as directory:
            path = Path(directory) / 'synthetic.trx'
            tree.write(path, encoding='utf-8')
            return V.verify(path, mode, (0 if mode == 'positive' else 1) if code is None else code, log)

    def test_exact_modes_and_embedded_stacks(self):
        for mode in ('positive', 'baseline'):
            for embedded in (False, True):
                with self.subTest(mode=mode, embedded=embedded):
                    self.assertTrue(self.check(*evidence(mode, embedded), mode))

    def test_wrong_exit_mode_or_fqn(self):
        for mode in ('positive', 'baseline'):
            for code in (-1, 2, 127, 1 if mode == 'positive' else 0):
                with self.subTest(mode=mode, code=code), self.assertRaises(AssertionError):
                    self.check(*evidence(mode), mode, code)
            tree, log = evidence(mode)
            tree.find('.//t:TestMethod', V.NS).set('className', 'Content.IntegrationTests.GameTests')
            with self.assertRaises(AssertionError):
                self.check(tree, log, mode)

    def test_runner_counters(self):
        for mode in ('positive', 'baseline'):
            for key, value in (('total', '2'), ('executed', '0'), ('completed', '2'), ('error', '1'), ('notExecuted', '1'), ('warning', '1'), ('unknown', '0'), ('failed', 'nine')):
                tree, log = evidence(mode)
                tree.find('.//t:Counters', V.NS).set(key, value)
                with self.subTest(mode=mode, key=key), self.assertRaises(AssertionError):
                    self.check(tree, log, mode)

    def test_missing_extra_results_or_definitions(self):
        for path in ('.//t:UnitTestResult', './/t:UnitTest', './/t:StdOut', './/t:Counters'):
            for mode in ('positive', 'baseline'):
                tree, log = evidence(mode)
                node = tree.find(path, V.NS)
                parent = next((p for p in tree.iter() if node in list(p)))
                parent.append(copy.deepcopy(node))
                with self.subTest(path=path, mode=mode), self.assertRaises(AssertionError):
                    self.check(tree, log, mode)

    def test_runtime_compile_runner_errors_rejected(self):
        for mode in ('positive', 'baseline'):
            for diagnostic in ('[ERRO] injected', 'Unhandled exception', 'Runtime occurred', 'test run aborted', 'testhost crash', 'Isolated DM fixture compilation failed', 'error CS0001', 'Build FAILED', '1 Error(s)', 'warning DM0001'):
                with self.subTest(mode=mode, error=diagnostic), self.assertRaises(AssertionError):
                    self.check(*(lambda e: (e[0], e[1] + '\n' + diagnostic))(evidence(mode)), mode)

    def test_exact_ownership_identity_content_bucket_rows(self):
        for mode in ('positive', 'baseline'):
            for mutate in (lambda s: s.replace('owners=[1,1,1]', 'owners=[2,1,1]', 1), lambda s: s.replace('; new=', '; new=9', 1), lambda s: s.replace('; ref1=', '; ref1=9', 1), lambda s: s.replace('; content=', '; content=9', 1), lambda s: s.replace('expected=11', 'expected=12', 1), lambda s: s.replace('datums=22', 'datums=23', 1), lambda s: s.replace('_type_first', '_foreign_first', 1)):
                tree, log = evidence(mode)
                node = tree.find('.//t:StdOut', V.NS)
                node.text = mutate(node.text)
                with self.subTest(mode=mode), self.assertRaises(AssertionError):
                    self.check(tree, log, mode)

    def test_row_missing_duplicate_order_and_unknown_marker(self):
        for mode in ('positive', 'baseline'):
            for kind in ('missing', 'duplicate', 'order', 'foreign', 'completion', 'success'):
                tree, log = evidence(mode)
                node = tree.find('.//t:StdOut', V.NS)
                lines = node.text.splitlines()
                if kind == 'missing':
                    lines.pop(1)
                elif kind == 'duplicate':
                    lines.insert(2, lines[1])
                elif kind == 'order':
                    lines[1], lines[2] = (lines[2], lines[1])
                elif kind == 'foreign':
                    lines.append('TYPESOF_FOREIGN')
                elif kind == 'completion':
                    lines.append(V.MARKER)
                else:
                    lines.append(V.SUCCESS)
                node.text = '\n'.join(lines)
                with self.subTest(mode=mode, kind=kind), self.assertRaises(AssertionError):
                    self.check(tree, log, mode)

    def test_original_log_must_match_trx(self):
        for mode in ('positive', 'baseline'):
            tree, log = evidence(mode)
            with self.assertRaises(AssertionError):
                self.check(tree, log.replace('ref1=', 'ref1=9', 1), mode)
            with self.assertRaises(AssertionError):
                self.check(tree, log + '\nPassed OtherTest [1 ms]', mode)

    def test_exact_aggregate_failures(self):
        for change in ('id', 'count', 'expression', 'number', 'order', 'warning'):
            tree, log = evidence('baseline')
            node = tree.find('.//t:Message', V.NS)
            old = node.text
            if change == 'id':
                new = old.replace('_UNRETURNED_LISTS_1000', '_OWNERS', 1)
            elif change == 'count':
                new = old.rsplit('18)', 1)[0]
            elif change == 'expression':
                new = old.replace('fresh.Length', 'fresh.Count', 1)
            elif change == 'number':
                new = old.replace('But was: 1000', 'But was: 999', 1)
            elif change == 'order':
                new = old.replace('1) TYPESOF', '2) TYPESOF', 1)
            else:
                new = old + '\n19) warning foreign'
            node.text = new
            log = log.replace(old, new)
            with self.subTest(change=change), self.assertRaises(AssertionError):
                self.check(tree, log, 'baseline')

    def test_unknown_frames_and_missing_fixture(self):
        for frame in ('at Foreign.Code.Run()', 'at Content.IntegrationTests.GameTests.NoRuntimesTest() in /repo/Content.IntegrationTests/GameTests.cs:line 1', 'at NUnit.Framework.Assert.Multiple(TestDelegate testDelegate)', ''):
            tree, log = evidence('baseline')
            tree.find('.//t:StackTrace', V.NS).text = frame
            log = log.replace(FRAME, frame)
            with self.subTest(frame=frame), self.assertRaises(AssertionError):
                self.check(tree, log, 'baseline')

    def test_numbered_assertion_stacks_match_blocks(self):
        tree, log = evidence('baseline', True)
        node = tree.find('.//t:StackTrace', V.NS)
        node.text = FRAME + '\n' + '\n'.join((f'{i + 1}) {FRAME}' for i in range(18)))
        self.assertTrue(self.check(tree, log, 'baseline'))
        node.text = node.text.replace('18) ', '19) ')
        with self.assertRaises(AssertionError):
            self.check(tree, log, 'baseline')

    def test_stderr_and_runinfo(self):
        tree, log = evidence()
        ET.SubElement(tree.find('.//t:Output', V.NS), TAG('StdErr')).text = 'foreign diagnostic'
        with self.assertRaises(AssertionError):
            self.check(tree, log)
        tree, log = evidence()
        ET.SubElement(tree.find('.//t:ResultSummary', V.NS), TAG('RunInfo'), outcome='Error')
        with self.assertRaises(AssertionError):
            self.check(tree, log)

    def test_exact_negative_footer_only(self):
        tree, log = evidence('baseline')
        log = log.rsplit('Test Run Failed.\n', 1)[0] + 'Test Run Failed.\nTotal tests: 1\n     Failed: 1\n Total time: 1.2 Seconds\n     1>Project "/repo/Content.IntegrationTests/Content.IntegrationTests.csproj" on node 1 (VSTest target(s)).\n     1>_VSTestConsole:\n         MSB4181: The "VSTestTask" task returned false but did not log an error.\n     1>Done Building Project "/repo/Content.IntegrationTests/Content.IntegrationTests.csproj" (VSTest target(s)) -- FAILED.\n\nBuild FAILED.\n    0 Warning(s)\n    0 Error(s)\n\nTime Elapsed 00:00:01.00\n'
        self.assertTrue(self.check(tree, log, 'baseline'))
        for modified in (log + 'Build FAILED', log.replace('0 Error(s)', '1 Error(s)'), log.replace('VSTest target(s)', 'Build target(s)'), log.replace('Total tests: 1', 'Total tests: 2')):
            with self.assertRaises(AssertionError):
                self.check(tree, modified, 'baseline')

    def test_negative_adapter_stdout_reprint_exact(self):
        tree, log = evidence('baseline')
        stdout = tree.find('.//t:StdOut', V.NS).text
        original = log
        log = original + '  Standard Output Messages:\n' + '\n'.join((' ' + line for line in stdout.splitlines())) + '\n\nResults File: /repo/typesof-results/typesof.trx\n'
        self.assertTrue(self.check(tree, log, 'baseline'))
        with self.assertRaises(AssertionError):
            self.check(tree, log.replace('new=1000', 'new=999', 1), 'baseline')
if __name__ == '__main__':
    unittest.main()

import importlib.util, json, unittest
from pathlib import Path
HERE = Path(__file__).resolve().parent

def module(name):
    s = importlib.util.spec_from_file_location(name, HERE / (name + '.py'))
    m = importlib.util.module_from_spec(s)
    s.loader.exec_module(m)
    return m
V = module('verify')

def evidence(mode='positive', constructor=False):
    name = 'ConstructorModePreservesRuntimeDelivery' if constructor else 'FastRejectionsAndTransfers'
    lines = []
    if constructor:
        lines = ['MATRIX_CONSTRUCTORS_COMPLETE new=1 error=1 partial=1']
    else:
        for kind in ('null', 'string', 'datum', 'list'):
            for pos in range(7):
                m = 100 if mode in ('baseline', 'omit-creator') else 0
                o = 0 if kind in ('null', 'string') else 1 + (100 * (6 if pos == 0 else 1) if mode in ('baseline', 'omit-arguments') else 0)
                lines.append(f'MATRIX_CASE {kind} {pos} matrices={m} expected={m} owner={o} expectedOwner={o}')
        n = 201 if mode in ('baseline', 'omit-arguments') else 1
        lines += [f'MATRIX_GENERIC {k} owner={n} expected={n}' for k in ('datum', 'list')]
        lines += [f'MATRIX_FAST_COMPLETE mode={mode} cases=28 iterations=100 generic=2 success=1']
    stdout = '\n'.join(lines)
    raw = f'<TestRun xmlns="{V.NS['t']}"><Results><UnitTestResult testId="x" testName="{name}" outcome="Passed"><Output><StdOut>{stdout}</StdOut></Output></UnitTestResult></Results><TestDefinitions><UnitTest id="x"><TestMethod className="Content.IntegrationTests.NativeMatrixOwnershipTests" name="{name}" /></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="1" failed="0" /></ResultSummary></TestRun>'
    log = 'MATRIX_EXPECTED_RUNTIME: Invalid arguments used to create matrix null 2 3 4 5 6' if constructor else ''
    return (raw, log)

class PureTests(unittest.TestCase):

    def test_four_observation_oracles(self):
        for mode in ('positive','baseline','omit-creator','omit-arguments'):
            raw, log = evidence(mode)
            self.assertTrue(V.classify(raw, log, 0, mode))

    def test_constructor_error_delivery(self):
        raw, log = evidence(constructor=True)
        self.assertTrue(V.classify(raw, log, 0, 'positive', True))
        for bad in ('', log + '\n' + log, log + '\n[ERRO] unrelated'):
            with self.assertRaises(AssertionError):
                V.classify(raw, bad, 0, 'positive', True)

    def test_fail_closed_parser(self):
        raw, log = evidence()
        for bad in (raw.replace('matrices=0 expected=0', 'matrices=1 expected=0', 1), raw.replace('MATRIX_GENERIC datum', 'MATRIX_GENERIC list'), raw.replace('outcome="Passed"', 'outcome="Skipped"'), raw.replace('cases=28', 'cases=27'), raw.replace('failed="0"', 'failed="1"'), raw.replace('testId="x"', 'testId="foreign"')):
            with self.assertRaises(AssertionError):
                V.classify(bad, log, 0, 'positive')
        with self.assertRaises(AssertionError):
            V.classify(raw, '[ERRO] unrelated', 0, 'positive')
        with self.assertRaises(AssertionError):
            V.classify(raw, log, 1, 'positive')
if __name__ == '__main__':
    unittest.main()

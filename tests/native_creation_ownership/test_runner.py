"""Pure runner/archive/ordinary-runtime adversaries. No SDK or native processes."""
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch,MagicMock
import xml.etree.ElementTree as ET
import zipfile
import run

def evidence():
    raw='''<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testName="NoRuntimesTest" testId="a" outcome="Passed"/></Results><TestDefinitions><UnitTest id="a"><TestMethod className="Content.IntegrationTests.GameTests" name="NoRuntimesTest"/></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="1" failed="0"/></ResultSummary></TestRun>'''
    return raw,'Passed NoRuntimesTest [1 s]\nTest Run Successful.'

class RunnerTests(unittest.TestCase):
    def test_pins_and_fixture_parity(self):
        spec=run.authenticate()
        self.assertEqual(spec['base_head'],'e5132430da15bffdf685ce736a6d02773c679012')
        self.assertTrue(all('tests/native_creation_ownership/'+name in spec['sources'] for name in ('run.py','control.py','test_runner.py','resource/verify.py','matrix/verify.py','typesof/verify.py')))
    def test_source_drift(self):
        original=Path.read_bytes
        def read(path):
            raw=original(path)
            return raw+b'changed' if path.name=='NativeMatrixOwnershipTests.cs' else raw
        with patch.object(Path,'read_bytes',read),self.assertRaises(AssertionError):run.authenticate()
    def test_local_guard_before_process(self):
        with patch.dict(os.environ,{},clear=True),patch.object(run.subprocess,'check_output') as proc,self.assertRaises(AssertionError):
            run.run('missing','positive',Path('unused'))
        proc.assert_not_called()
    def test_fresh_ordinary_and_adversaries(self):
        raw,log=evidence();self.assertTrue(run.ordinary(raw,log,0))
        bads=[raw.replace('Passed','Skipped'),raw.replace('GameTests','ForeignTests'),raw.replace('testId="a"','testId="b"'),raw.replace('failed="0"','failed="1"'),raw.replace('failed="0"','failed="0" invented="0"'),raw.replace('failed="0"','failed="0" notExecuted="1"'),raw.replace('<ResultSummary>','<ResultSummary><RunInfo/>'),raw.replace('</Results>','<UnitTestResult/></Results>'),raw.replace('</Results>','<ErrorInfo/></Results>'),raw.replace('</Results>','<StdErr>foreign</StdErr></Results>')]
        for bad in bads:
            with self.assertRaises(AssertionError):run.ordinary(bad,log,0)
        for bad in (log+'\n[ERRO] unrelated',log+'\nruntime error',log.replace('NoRuntimesTest','foreign'),log.replace('Test Run Successful.','Test Run Failed.')):
            with self.assertRaises(AssertionError):run.ordinary(raw,bad,0)
        with self.assertRaises(AssertionError):run.ordinary(raw,log,1)
    def test_raw_archive_and_partial_evidence(self):
        with tempfile.TemporaryDirectory() as d:
            out=Path(d);(out/'resource').mkdir()
            originals={'sdk-info.log':b'sdk\r\n','resource/resource.log':b'Failed exact assertion\r\n','resource/resource.trx':b'<raw-trx/>\r\n'}
            for name,raw in originals.items():(out/name).write_bytes(raw)
            receipt=run.archive(out)
            self.assertEqual(set(receipt['files']),set(originals))
            with zipfile.ZipFile(out/'original-logs-trx.zip') as z:
                for name,raw in originals.items():self.assertEqual(z.read(name),raw)
            self.assertEqual(receipt['zip_sha256'],run.sha((out/'original-logs-trx.zip').read_bytes()))
    def test_archive_bounds_and_symlinks(self):
        with tempfile.TemporaryDirectory() as d:
            out=Path(d);(out/'x.log').write_bytes(b'1234')
            with patch.object(run,'MAX_FILE',3),self.assertRaises(AssertionError):run.archive(out)
            with patch.object(run,'MAX_ARCHIVE',3),self.assertRaises(AssertionError):run.archive(out)
            with patch.object(Path,'is_symlink',return_value=True),self.assertRaises(AssertionError):run.archive(out)
    def test_empty_partial_log_is_retained(self):
        with tempfile.TemporaryDirectory() as d:
            out=Path(d);(out/'sdk-info.log').write_bytes(b'')
            receipt=run.archive(out)
            self.assertEqual(receipt['files']['sdk-info.log']['bytes'],0)
    def test_command_exit_receipt_without_launch(self):
        with tempfile.TemporaryDirectory() as d:
            path=Path(d)/'test.log';process=MagicMock();process.wait.return_value=1
            with patch.object(run.subprocess,'Popen',return_value=process) as launch:
                self.assertEqual(run.command(['fake-sdk','test'],path,{},1),1)
            self.assertTrue(launch.call_args.kwargs['start_new_session'])
            self.assertEqual(json.loads(path.with_suffix('.exit.json').read_text()),{'argv':['fake-sdk','test'],'exit':1,'timeout':False})
    def test_timeout_group_cleanup_and_partial_receipt(self):
        with tempfile.TemporaryDirectory() as d:
            path=Path(d)/'test.log';process=MagicMock(pid=123,returncode=-15)
            process.wait.side_effect=[run.subprocess.TimeoutExpired(['fake-sdk'],1),-15]
            with patch.object(run.subprocess,'Popen',return_value=process),patch.object(run.os,'killpg',create=True) as kill,self.assertRaises(run.subprocess.TimeoutExpired):
                run.command(['fake-sdk'],path,{},1)
            kill.assert_called_once_with(123,run.signal.SIGTERM)
            receipt=json.loads(path.with_suffix('.exit.json').read_text())
            self.assertTrue(receipt['timeout']);self.assertEqual(receipt['exit'],-15)
    def test_explicit_registration(self):
        workflow=(run.ROOT/'.github/workflows/native-creation-ownership.yml').read_text()
        self.assertIn('submodules: recursive',workflow)
        self.assertIn("PYTHONDONTWRITEBYTECODE: '1'",workflow)
        self.assertIn('native_creation_ownership/run.py --dotnet',workflow)
        for mode in run.control.SPEC['modes']:self.assertIn(mode,workflow)
        for suite in ('resource','matrix','typesof'):self.assertIn(suite,workflow)
        self.assertIn('if: always()',workflow)

if __name__=='__main__':unittest.main()

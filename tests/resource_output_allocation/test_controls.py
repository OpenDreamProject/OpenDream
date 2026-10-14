import unittest
from control import mutate

class Controls(unittest.TestCase):
    def test_small_path_control_only_and_missing_duplicate_rejected(self):
        needle=b'if (text.Length <= 4096) {'
        changed=mutate(b'before '+needle+b' after','no-small-path')
        self.assertEqual(changed,b'before if (text.Length < 0) { // negative control: ordinary output pays writer overhead after')
        for raw in (b'missing',needle*2):
            with self.assertRaises(AssertionError):mutate(raw,'no-small-path')
    def test_positive_identical(self):
        raw=b'writer.Write("\\r\\n");'
        self.assertEqual(mutate(raw,'positive'),raw)
    def test_platform_newline_only(self):
        self.assertEqual(mutate(b'prefix writer.Write("\\r\\n"); suffix','platform-newline'),b'prefix writer.WriteLine(); suffix')
    def test_missing_guard_rejected(self):
        with self.assertRaises(AssertionError): mutate(b'no terminator','platform-newline')
    def test_duplicate_guard_rejected(self):
        with self.assertRaises(AssertionError): mutate(b'writer.Write("\\r\\n");'*2,'platform-newline')


class Classifier(unittest.TestCase):
    def positive(self):
        import verify, xml.etree.ElementTree as ET, json
        root=ET.Element('TestRun',xmlns=verify.NS['t'])
        rows=ET.SubElement(root,'Results');defs=ET.SubElement(root,'TestDefinitions')
        for name in sorted(verify.TESTS):
            row=ET.SubElement(rows,'UnitTestResult',testName=name,testId=name,outcome='Passed')
            unit=ET.SubElement(defs,'UnitTest',id=name)
            ET.SubElement(unit,'TestMethod',className=verify.CLASS,name=name)
            if name=='AllocationWorkloads':
                out=ET.SubElement(row,'Output')
                ET.SubElement(out,'StdOut').text='\n'.join('OUTPUT_ALLOCATION_METRIC '+json.dumps({'chars':c,'count':8,'allocated':1024,'seconds':0,'collections':[0,0,0]}) for c in [16,1024,4096,4097,1048576,8388608])
        ET.SubElement(ET.SubElement(root,'ResultSummary'),'Counters',total='3',executed='3',passed='3',failed='0')
        return ET.tostring(root).decode(), 'Test Run Successful.'

    def test_foreign_runtime_identity_counters_and_stderr_rejected(self):
        import verify
        from tempfile import TemporaryDirectory
        from pathlib import Path
        raw,log=self.positive()
        with TemporaryDirectory() as temp:
            verify.verify(raw,log,'positive',0,Path(temp))
            for changed in (raw.replace('testId="SavefileRoundTrip"','testId="foreign"'),raw.replace('passed="3"','passed="2"'),raw.replace('executed="3"','executed="2"'),raw.replace('</Output>','<StdErr>unexpected</StdErr></Output>'),raw.replace('</Output>','<StdOut>[ERRO] unexpected</StdOut></Output>')):
                with self.assertRaises(AssertionError):verify.verify(changed,log,'positive',0,Path(temp))
            with self.assertRaises(AssertionError):verify.verify(raw,log+'\nwarning CS1234 foreign','positive',0,Path(temp))

    def test_positive_cannot_impersonate_counterexample(self):
        import verify
        from tempfile import TemporaryDirectory
        from pathlib import Path
        raw,log=self.positive()
        with TemporaryDirectory() as temp:
            for mode in ('baseline','platform-newline','no-small-path'):
                with self.assertRaises(AssertionError):verify.verify(raw,log,mode,1,Path(temp))


class FixtureSource(unittest.TestCase):
    def test_valid_unicode_line_matches_accepted_fork_utf8_bytes(self):
        import hashlib
        from pathlib import Path
        source=(Path(__file__).resolve().parents[2]/'Content.IntegrationTests/ResourceOutputAllocationTests.cs').read_bytes()
        lines=[line for line in source.replace(b'\r\n',b'\n').splitlines() if b'var values = new[]' in line]
        self.assertEqual(len(lines),1)
        self.assertEqual(hashlib.sha256(lines[0]).hexdigest(), 'bda2449b28e9260287b569735aedf209afd65019b30de65e4d4b988db5c4c737')
        valid=(chr(0xe9)+chr(0x1f600)).encode('utf-8')
        self.assertEqual(lines[0].count(valid),1)
        self.assertEqual(lines[0].count(chr(0x1f600).encode('utf-8')),2)
        corrupted=valid.decode('cp1252').encode('utf-8')
        self.assertNotEqual(hashlib.sha256(lines[0].replace(valid,corrupted)).hexdigest(),'bda2449b28e9260287b569735aedf209afd65019b30de65e4d4b988db5c4c737')

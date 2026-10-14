import hashlib,importlib.util,sys,unittest,xml.etree.ElementTree as ET
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
import control,verify
from unittest.mock import patch
from tempfile import TemporaryDirectory

class Controls(unittest.TestCase):
    def test_actual_source_routes_and_barrier(self):
        raw=(control.ROOT/control.MANAGER).read_bytes().replace(b'\r\n',b'\n')
        self.assertEqual(raw.count(b'level.QueueTileUpdate((turfPos, new Tile((int)turfId)))'),2)
        self.assertNotIn(b'level.QueuedTileUpdates.Add',raw)
        self.assertIn(b'level.FlushTileUpdates',raw)
        mutated=control.mutate(raw,'unsafe-dedup',b'original')
        self.assertNotIn(b'_pendingNonEmptyTiles.Remove(update.Position)',mutated)
        self.assertEqual(control.mutate(raw,'baseline',b'original'),b'original')
        self.assertEqual(control.mutate(raw,'positive',b'original'),raw)
        with self.assertRaises(AssertionError): control.mutate(b'foreign','unsafe-dedup',b'original')

    def test_robust_authentication_rejects_head_dirty_blob_and_disk_drift(self):
        with TemporaryDirectory() as temporary:
            root=Path(temporary);path=root/'RobustToolbox'/'source.cs';path.parent.mkdir();path.write_bytes(b'original\n')
            manifest={'robust_commit':'reviewed','robust_sources':{'source.cs':control.sha(b'original\n')}}
            def git(_, *args):
                if args[0]=='rev-parse':return b'reviewed\n'
                if args[0]=='diff':return b''
                return b'original\n'
            with patch.object(control,'git',side_effect=git):
                self.assertEqual(control.authenticate_robust(root,manifest)['commit'],'reviewed')
                path.write_bytes(b'drift\n')
                with self.assertRaises(AssertionError):control.authenticate_robust(root,manifest)
                path.write_bytes(b'original\n')
            for replies in ((b'foreign\n',),(b'reviewed\n',b'source.cs\n'),(b'reviewed\n',b'',b'foreign\n')):
                with patch.object(control,'git',side_effect=replies):
                    with self.assertRaises(AssertionError):control.authenticate_robust(root,manifest)

    def result(self,mode='positive'):
        ns=verify.NS['t'];root=ET.Element('TestRun',xmlns=ns)
        row=ET.SubElement(ET.SubElement(root,'Results'),'UnitTestResult',testName=verify.TEST,testId='id',outcome='Passed')
        out=ET.SubElement(row,'Output');ET.SubElement(out,'StdOut').text=(
            'TILE_QUEUE_OBSERVATION: repeated=60000 peak=1 retained=0 area_peak=512\n'
            'TILE_QUEUE_PARITY: anchored_empty=True baseline_empty=True barriers=True\n'
            'TILE_QUEUE_COMPLETE: production routes, real grids, transforms, levels, drains and final tiles checked')
        definition=ET.SubElement(ET.SubElement(root,'TestDefinitions'),'UnitTest',id='id')
        ET.SubElement(definition,'TestMethod',className=verify.CLASS,name=verify.TEST)
        ET.SubElement(ET.SubElement(root,'ResultSummary'),'Counters',total='1',executed='1',passed='1',failed='0',error='0')
        return ET.tostring(root),f'Passed {verify.TEST}\nTest Run Successful.'

    def test_original_classifier_rejects_incomplete_or_foreign(self):
        raw,log=self.result()
        self.assertTrue(verify.verify(raw,log,'positive',0))
        for altered in (raw.replace(b'peak=1',b'peak=2'),raw.replace(b'barriers=True',b'barriers=False'),raw.replace(b'Passed',b'Failed'),raw.replace(b'PendingTilesPreserveEmptyTransitions',b'foreign')):
            with self.assertRaises(AssertionError):verify.verify(altered,log,'positive',0)
        for altered in (log+'\n[ERRO] foreign',log+'\nUnhandled exception',log.replace('Test Run Successful.','')):
            with self.assertRaises(AssertionError): verify.verify(raw,altered,'positive',0)
        for mode in ('baseline','unsafe-dedup'):
            with self.assertRaises(AssertionError):verify.verify(raw,log,mode,1)
if __name__=='__main__':unittest.main()

"""Original log preservation controls, never native execution."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('raw_archives',HERE/'archive_logs.py');archive=importlib.util.module_from_spec(spec);spec.loader.exec_module(archive)

class ArchiveTests(unittest.TestCase):
    def populated(self,root):
        for name in archive.NAMES:(root/name).write_bytes(b'original\r\n\x00'+name.encode())
    def test_raw_original_byte_roundtrip(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);self.populated(root);receipt=archive.archive(root)
            self.assertTrue(archive.verify(root));self.assertEqual(set(receipt['files']),set(archive.NAMES))
    def test_original_mutation_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);self.populated(root);archive.archive(root);(root/'raw-cache.log').write_bytes(b'changed')
            with self.assertRaises(AssertionError):archive.verify(root)
    def test_zip_mutation_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);self.populated(root);archive.archive(root)
            with zipfile.ZipFile(root/'original-logs.zip','a') as z:z.writestr('other.log',b'changed')
            with self.assertRaises(AssertionError):archive.verify(root)
    def test_archive_cap_rejected_before_zip(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);self.populated(root);old=archive.LIMIT;archive.LIMIT=1
            try:
                with self.assertRaises(AssertionError):archive.archive(root)
                self.assertFalse((root/'original-logs.zip').exists())
            finally:archive.LIMIT=old

if __name__=='__main__':unittest.main()

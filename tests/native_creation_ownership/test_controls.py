import unittest
from pathlib import Path
import control
HERE=Path(__file__).resolve().parent
class ControlTests(unittest.TestCase):
    def setUp(self):self.source={p:(control.ROOT/p).read_bytes() for p in control.SPEC['candidate_sources']}
    def test_positive(self):self.assertEqual(control.mutate(self.source,'positive'),self.source)
    def test_resource_controls_keep_creation_fixes(self):
        for mode in ['resource-baseline','hash-only']:
            changed=control.mutate(self.source,mode)
            for key in ['creator','arguments','typesof']:
                spec=control.SPEC['methods'][key];self.assertIn(spec['candidate'].encode(),changed[spec['path']])
    def test_creation_controls_keep_resource_fix(self):
        for mode in ['matrix-baseline','omit-creator','omit-arguments','typesof-baseline']:
            changed=control.mutate(self.source,mode)
            self.assertEqual(changed[control.MANAGER],self.source[control.MANAGER])
            self.assertEqual(changed[control.SAVEFILE],self.source[control.SAVEFILE])
    def test_matrix_controls_keep_typesof(self):
        for mode in ['matrix-baseline','omit-creator','omit-arguments']:
            spec=control.SPEC['methods']['typesof'];self.assertIn(spec['candidate'].encode(),control.mutate(self.source,mode)[spec['path']])
    def test_typesof_baseline_keeps_matrix(self):
        changed=control.mutate(self.source,'typesof-baseline')
        for key in ['creator','arguments']:
            spec=control.SPEC['methods'][key];self.assertIn(spec['candidate'].encode(),changed[spec['path']])
    def test_single_omissions(self):
        for omitted,kept in [('creator','arguments'),('arguments','creator')]:
            changed=control.mutate(self.source,'omit-'+omitted)
            for key,name in [(omitted,'original'),(kept,'candidate')]:
                spec=control.SPEC['methods'][key];self.assertIn(spec[name].encode(),changed[spec['path']])
    def test_source_drift_rejected(self):
        self.source[control.MANAGER]+=b'\n'
        with self.assertRaises(AssertionError):control.mutate(self.source,'positive')
if __name__=='__main__':unittest.main()

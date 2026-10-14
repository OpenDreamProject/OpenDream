"""Authenticate exact committed inputs and immutable Git original; no native tools."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASE = 'c3b16108e8c7177a1be5167db8ab61a3398dfd14'
RESOURCE = 'OpenDreamRuntime/Resources/DreamResource.cs'
PATHS = [RESOURCE,
         'OpenDreamRuntime/Objects/Types/DreamObjectSavefile.cs',
         'OpenDreamShared/Dream/StringFormatDecoder.cs',
         'Content.IntegrationTests/ResourceOutputAllocationTests.cs',
         'Content.IntegrationTests/DMProject/Fixtures/resource-output-allocation.dm']

def git(*args): return subprocess.check_output(['git', '-C', str(ROOT), *args])
def sha(raw): return hashlib.sha256(raw).hexdigest()
def mutate(raw, mode):
    if mode == 'positive': return raw
    if mode == 'no-small-path':
        needle = b'if (text.Length <= 4096) {'
        assert raw.count(needle) == 1
        return raw.replace(needle, b'if (text.Length < 0) { // negative control: ordinary output pays writer overhead')
    assert mode == 'platform-newline'
    needle = b'writer.Write("\\r\\n");'
    assert raw.count(needle) == 1
    return raw.replace(needle, b'writer.WriteLine();')

def prepare(mode, head, output, verify=False):
    assert git('rev-parse', 'HEAD').decode().strip() == head
    changed = git('diff', '--name-only', 'HEAD').decode().splitlines()
    assert set(changed) <= ({RESOURCE} if verify else set())
    manifest = json.loads((ROOT / 'tests/resource_output_allocation/source-manifest.json').read_text())
    import importlib.util
    spec = importlib.util.spec_from_file_location('tile_authentication', ROOT / 'tests/turf_tile_queue/control.py')
    auth = importlib.util.module_from_spec(spec); spec.loader.exec_module(auth)
    robust = auth.authenticate_robust(ROOT, manifest)
    manifest_name='tests/resource_output_allocation/source-manifest.json'
    assert (ROOT/manifest_name).read_bytes()==git('cat-file','blob',head+':'+manifest_name)
    records = {}
    for name, pin in manifest['sources'].items():
        raw=git('cat-file','blob',head+':'+name)
        assert sha(raw)==pin, 'unreviewed source: '+name
        if name!=RESOURCE: assert (ROOT/name).read_bytes().replace(b'\r\n',b'\n')==raw, 'source drift: '+name
        if not verify:
            target=output/'prepared-source'/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
    if not verify:
        for name in manifest['robust_sources']:
            target=output/'prepared-source'/'RobustToolbox'/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((ROOT/'RobustToolbox'/name).read_bytes())
    for name in PATHS:
        committed = git('cat-file', 'blob', head + ':' + name)
        assert sha(committed) == manifest['sources'][name], 'unreviewed committed source: ' + name
        wanted = committed
        if name == RESOURCE:
            original = git('cat-file', 'blob', BASE + ':' + name)
            assert sha(original) == manifest['baseline_sha256'], 'unreviewed original'
            assert original.count(b'File.AppendAllText(ResourcePath, text + "\\r\\n");') == 1
            wanted = original if mode == 'baseline' else mutate(committed, mode)
            records['immutable_original_sha256'] = sha(original)
        actual = (ROOT / name).read_bytes().replace(b'\r\n', b'\n')
        assert actual == (wanted if verify else committed), 'source drift: ' + name
        records[name] = {'committed': sha(committed), 'prepared': sha(wanted)}
        if not verify:
            target = output / 'prepared-source' / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(wanted)
            if name == RESOURCE: (ROOT / name).write_bytes(wanted)
    receipt = {'head': head, 'base': BASE, 'mode': mode, 'sources': records, 'robust': robust}
    output.mkdir(parents=True, exist_ok=True)
    if verify: assert json.loads((output / 'source-receipt.json').read_text()) == receipt
    else: (output / 'source-receipt.json').write_text(json.dumps(receipt, indent=2))

if __name__ == '__main__':
    p = argparse.ArgumentParser(); p.add_argument('mode', choices=['positive','baseline','platform-newline','no-small-path'])
    p.add_argument('--head', required=True); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--verify', action='store_true'); a=p.parse_args()
    prepare(a.mode,a.head,a.output,a.verify)

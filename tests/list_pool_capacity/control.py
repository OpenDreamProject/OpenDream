"""Exact production mutations, authenticated against immutable Git source."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASE = 'f7150679d926d6613da32c08d42a0c4d9b922bfc'
LIST = 'OpenDreamRuntime/Objects/Types/DreamList.cs'
PATHS = [LIST, 'OpenDreamRuntime/Objects/DreamObject.cs',
         'OpenDreamRuntime/Objects/DreamObjectTree.cs', 'OpenDreamRuntime/Objects/DreamRefManager.cs',
         'OpenDreamRuntime/DreamValue.cs', 'OpenDreamRuntime/DreamManager.cs',
         'OpenDreamShared/OpenDreamCVars.cs', 'Content.IntegrationTests/ListPoolCapacityTests.cs']

def git(*args): return subprocess.check_output(['git', '-C', str(ROOT), *args])
def sha(raw): return hashlib.sha256(raw).hexdigest()
def mutate(raw, mode):
    if mode == 'positive': return raw
    needle, replacement = {
        'nonclearing': (b'            _values.Clear();', b'            // negative control: retain references in pooled storage'),
        'no-pooling': (b'ListPool.Count < DreamManager.ListPoolSize', b'ListPool.Count < 0 // negative control: disable return\n            ')
    }[mode]
    assert raw.count(needle) == 1
    return raw.replace(needle, replacement)

def prepare(mode, head, output, verify=False):
    assert git('rev-parse', 'HEAD').decode().strip() == head
    changed = git('diff', '--name-only', 'HEAD').decode().splitlines()
    assert set(changed) <= ({LIST} if verify else set())
    original = git('cat-file', 'blob', BASE + ':' + LIST)
    assert b'MaximumPooledCapacity' not in original
    assert original.count(b'ListPool.Count < DreamManager.ListPoolSize') == 1
    manifest_name = 'tests/list_pool_capacity/source-manifest.json'
    manifest_bytes = (ROOT/manifest_name).read_bytes().replace(b'\r\n', b'\n')
    assert manifest_bytes == git('cat-file', 'blob', head + ':' + manifest_name), 'manifest drift'
    manifest=json.loads(manifest_bytes)
    assert sha(original)==manifest['baseline_sha256'], 'unreviewed original'
    records = {}
    for name,pin in manifest['sources'].items():
        committed=git('cat-file','blob',head+':'+name)
        assert sha(committed)==pin,'unreviewed source: '+name
        assert (ROOT/name).read_bytes().replace(b'\r\n',b'\n')==committed or (verify and name==LIST), 'source drift: '+name
        if not verify:
            target=output/'prepared-source'/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(committed)
    for name in PATHS:
        committed = git('cat-file', 'blob', head + ':' + name)
        wanted = original if name == LIST and mode == 'baseline' else mutate(committed, mode) if name == LIST else committed
        assert (ROOT/name).read_bytes().replace(b'\r\n',b'\n') == (wanted if verify else committed), 'source drift: '+name
        records[name] = {'committed':sha(committed), 'prepared':sha(wanted)}
        if not verify:
            target = output/'prepared-source'/name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(wanted)
            if name == LIST: (ROOT/name).write_bytes(wanted)
    receipt = {'head':head, 'base':BASE, 'mode':mode, 'immutable_original_sha256':sha(original), 'sources':records}
    output.mkdir(parents=True, exist_ok=True)
    if verify: assert json.loads((output/'source-receipt.json').read_text()) == receipt
    else: (output/'source-receipt.json').write_text(json.dumps(receipt, indent=2))

if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('mode',choices=['positive','baseline','nonclearing','no-pooling'])
    parser.add_argument('--head',required=True);parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--verify',action='store_true');args=parser.parse_args()
    prepare(args.mode,args.head,args.output,args.verify)

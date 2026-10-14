"""Exact committed-source mutations. No compiler/runtime invocation."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
RESOURCE = 'OpenDreamRuntime/Resources/DreamResource.cs'
SAVEFILE = 'OpenDreamRuntime/Objects/Types/DreamObjectSavefile.cs'
MODES = ('positive', 'baseline', 'omit-release', 'drop-inmemory')
READ = '''            string? data;
            try {
                data = Resource.ReadAsString();
            } finally {
                Resource.ReleaseFileDataCache();
            }'''
GUARD = '''        if (string.IsNullOrEmpty(filePath))
            return;

        ResourceData = null; // The setter also balances TOOLS memory accounting.'''

def sha(raw): return hashlib.sha256(raw).hexdigest()
def git(root, *args): return subprocess.check_output(['git', '-c', 'safe.directory='+root.as_posix(), *args], cwd=root, timeout=20)
def mutate(raw, path, mode):
    text = raw.decode('utf-8')
    if path == SAVEFILE:
        assert text.count(READ) == 1
        if mode == 'baseline': text = text.replace(READ, '            var data = Resource.ReadAsString();')
        if mode == 'omit-release': text = text.replace('                Resource.ReleaseFileDataCache();', '                // Deliberate omitted disk-cache release.')
    if path == RESOURCE:
        assert text.count(GUARD) == 1
        if mode == 'drop-inmemory': text = text.replace(GUARD, '        ResourceData = null; // Deliberate loss of in-memory resource data.')
    return text.encode()

def prepare(root, mode, head, receipt, verify=False):
    assert mode in MODES and git(root, 'rev-parse', 'HEAD').decode().strip() == head
    changed = git(root, 'diff', '--name-only', 'HEAD').decode().splitlines()
    assert set(changed) <= ({RESOURCE, SAVEFILE} if verify else set()), 'unexpected changes'
    manifest_path = 'tests/savefile_raw_cache/source-manifest.json'
    raw_manifest = git(root, 'show', head+':'+manifest_path)
    assert (root/manifest_path).read_bytes() == raw_manifest
    manifest = json.loads(raw_manifest)
    records = {}
    for path, expected in manifest['sources'].items():
        committed = git(root, 'show', head+':'+path)
        assert sha(committed) == expected, 'unreviewed source: '+path
        prepared = mutate(committed, path, mode) if path in (RESOURCE, SAVEFILE) else committed
        assert (root/path).read_bytes().replace(b'\r\n',b'\n') == (prepared if verify else committed), 'source drift: '+path
        records[path] = {'committed_sha256':sha(committed), 'prepared_sha256':sha(prepared)}
    data = {'schema':1, 'head':head, 'mode':mode, 'manifest_sha256':sha(raw_manifest), 'sources':records}
    if verify:
        assert json.loads(receipt.read_text()) == data
    else:
        for path in (RESOURCE, SAVEFILE):
            (root/path).write_bytes(mutate(git(root, 'show', head+':'+path), path, mode))
        receipt.parent.mkdir(parents=True, exist_ok=True)
        receipt.write_text(json.dumps(data,indent=2)+'\n')
    return data

if __name__ == '__main__':
    p=argparse.ArgumentParser();p.add_argument('mode',choices=MODES);p.add_argument('--head',required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--verify',action='store_true');a=p.parse_args()
    prepare(ROOT,a.mode,a.head,a.receipt,a.verify)

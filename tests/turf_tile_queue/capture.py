"""Copy/hash existing source and build outputs; never execute native tools."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

ROOT=Path(__file__).resolve().parents[2]
BINARY_PATHS=('bin/Content.IntegrationTests/Content.IntegrationTests.dll','bin/Content.IntegrationTests/Content.IntegrationTests.pdb',
 'OpenDreamRuntime/bin/Release/net10.0/OpenDreamRuntime.dll','OpenDreamRuntime/bin/Release/net10.0/OpenDreamRuntime.pdb')
def capture(output,binaries):
    manifest=json.loads((ROOT/'tests/turf_tile_queue/source-manifest.json').read_text())
    names=BINARY_PATHS if binaries else list(manifest['sources'])+['RobustToolbox/'+name for name in manifest['robust_sources']]
    target=output/('binaries' if binaries else 'prepared-source');records={}
    for name in names:
        raw=(ROOT/name).read_bytes();path=target/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
        records[name]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    (output/('binary-sha256.json' if binaries else 'prepared-sha256.json')).write_text(json.dumps(records,indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--binaries',action='store_true');a=p.parse_args();capture(a.output,a.binaries)

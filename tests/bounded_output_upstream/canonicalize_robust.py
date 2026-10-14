"""CI-only canonical LF materialization of already authenticated Robust sources.

No compilation or native process. Refuse content drift before writing any source.
"""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def prepare(root):
 assert os.environ.get('GITHUB_ACTIONS')=='true', 'only the owned Actions checkout may be materialized'
 manifest=json.loads((root/'tests/turf_tile_queue/source-manifest.json').read_text())
 robust=root/'RobustToolbox'
 def git(*args):return subprocess.check_output(['git','-C',str(robust),*args])
 assert git('rev-parse','HEAD').decode().strip()==manifest['robust_commit']
 assert not git('diff','--name-only','HEAD').strip(), 'Robust changes'
 writes=[]
 for name,pin in manifest['robust_sources'].items():
  original=git('cat-file','blob',manifest['robust_commit']+':'+name)
  assert hashlib.sha256(original).hexdigest()==pin, 'unreviewed Robust blob'
  target=robust/name
  assert target.read_bytes().replace(b'\r\n',b'\n')==original, 'Robust content drift'
  writes.append((target,original))
 for target,original in writes:target.write_bytes(original)
if __name__=='__main__':prepare(ROOT)

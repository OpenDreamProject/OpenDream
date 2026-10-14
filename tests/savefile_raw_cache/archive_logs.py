"""Retain original bytes; bounded extraction compares hashes before replay."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

NAMES=('sdk-info.log','build.log','raw-cache.log','runtime.log')
LIMIT=32*1024*1024
def sha(raw): return hashlib.sha256(raw).hexdigest()
def archive(root):
    data={name:(root/name).read_bytes() for name in NAMES}
    assert all(len(raw)<=LIMIT for raw in data.values())
    path=root/'original-logs.zip'
    with zipfile.ZipFile(path,'w',compression=zipfile.ZIP_DEFLATED) as z:
        for name,raw in data.items(): z.writestr(name,raw)
    receipt={'schema':1,'zip_sha256':sha(path.read_bytes()),'files':{name:{'bytes':len(raw),'sha256':sha(raw)} for name,raw in data.items()}}
    (root/'original-logs.json').write_text(json.dumps(receipt,indent=2)+'\n')
    return receipt
def verify(root):
    path=root/'original-logs.zip';receipt=json.loads((root/'original-logs.json').read_text())
    assert receipt['schema']==1 and sha(path.read_bytes())==receipt['zip_sha256']
    assert set(receipt['files'])==set(NAMES)
    with zipfile.ZipFile(path) as z:
        assert len(z.infolist())==len(NAMES) and set(z.namelist())==set(NAMES)
        for info in z.infolist():
            assert info.file_size<=LIMIT and not info.flag_bits&1
            raw=z.read(info);record=receipt['files'][info.filename]
            assert len(raw)==record['bytes'] and sha(raw)==record['sha256']
            assert raw==(root/info.filename).read_bytes(), 'original log changed'
    return True
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('--verify',action='store_true');a=p.parse_args();print(verify(a.root) if a.verify else archive(a.root))

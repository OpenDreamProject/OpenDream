"""Detached pure source controls. No native launch or checkout mutation.

Prepare emits four source files into an exclusively new folder; root may stage
those files in a separately owned CI control checkout after verifying the receipt.
"""
import argparse,hashlib,json
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
SPEC=json.loads((HERE/'controls.json').read_text())
MANAGER='OpenDreamRuntime/Resources/DreamResourceManager.cs'
SAVEFILE='OpenDreamRuntime/Objects/Types/DreamObjectSavefile.cs'
def sha(b):return hashlib.sha256(b).hexdigest()
def mutate(source,mode):
    assert mode in SPEC['modes']
    for path,pin in SPEC['candidate_sources'].items():assert sha(source[path])==pin,path
    result=dict(source)
    if mode=='resource-baseline':
        for path in [MANAGER,SAVEFILE]:result[path]=(HERE/'original'/path).read_bytes()
    if mode=='hash-only':
        needle=b'if (data.AsSpan().SequenceEqual(candidate))'
        assert result[MANAGER].count(needle)==1
        result[MANAGER]=result[MANAGER].replace(needle,b'if (true) // Deliberate negative control: ignore byte equality')
    omitted={'matrix-baseline':['creator','arguments'],'omit-creator':['creator'],
             'omit-arguments':['arguments'],'typesof-baseline':['typesof']}.get(mode,[])
    for key in omitted:
        spec=SPEC['methods'][key];path=spec['path'];needle=spec['candidate'].encode()
        assert result[path].count(needle)==1
        result[path]=result[path].replace(needle,spec['original'].encode())
    return result
def prepare(mode,output):
    assert not output.exists(),'output must be a newly owned detached directory'
    source={p:(ROOT/p).read_bytes() for p in SPEC['candidate_sources']}
    prepared=mutate(source,mode);output.mkdir(parents=True)
    for path,b in prepared.items():
        f=output/path;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
    receipt={'mode':mode,'upstream_head':SPEC['upstream_head'],'files':{p:sha(b) for p,b in prepared.items()},'native':'unrun'}
    (output/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');return receipt
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--mode',required=True,choices=SPEC['modes']);p.add_argument('--output',required=True,type=Path);a=p.parse_args();print(json.dumps(prepare(a.mode,a.output)))

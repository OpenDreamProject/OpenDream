"""Reuse the strict ordinary runtime classifier from the existing upstream suite."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'native_creation_ownership'))
from run import ordinary
if __name__=='__main__':
 ordinary(Path(sys.argv[1]).read_text(encoding='utf-8-sig'),Path(sys.argv[2]).read_text(encoding='utf-8'),int(sys.argv[3]))

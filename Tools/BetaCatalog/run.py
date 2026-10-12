import argparse,json,re,subprocess,tempfile
from pathlib import Path
import yaml
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');a=ap.parse_args()
catalog=json.loads((ROOT/'Tools/BetaCatalog/catalog.json').read_text())['entries']
text=(ROOT/'Assets/Block Data.asset').read_text();blocks=yaml.safe_load(text[text.index('MonoBehaviour:\n'):])['MonoBehaviour']['textureDataList']
assert len(blocks)==len(catalog)==133
assert sorted(b['blockType'] for b in blocks)==list(range(133))
lookup={b['blockType']:b for b in blocks}
for e in catalog[21:]:
 b=lookup[e['localId']]
 assert all(ref=={'fileID':0} for ref in b['textureData'].values()),e['name']
 assert b['opacity']==e['opacity'] and b['lightEmission']==e['emission'],e['name']
 assert b['isTransparent']==int(e['transparent']) and b['generateCollider']==int(e['collider']),e['name']
print('133 unique asset entries, 112 unassigned face sets and lighting/collider defaults verified.')
with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'catalog.exe'
 sources=['Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs',"Assets/_Scripts/Entity's/Player/HandMining.cs",'Tools/BetaCatalog/CatalogCheck.cs']
 subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(ROOT/s) for s in sources]],check=True)
 subprocess.run([a.mono,str(binary)],check=True)

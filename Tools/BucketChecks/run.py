import argparse,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as folder:
    binary=Path(folder)/'buckets.exe'
    sources=['Assets/_Scripts/ItemType.cs','Assets/_Scripts/Block/BlockType.cs',"Assets/_Scripts/Entity's/Player/BlockTargeting.cs",'Assets/_Scripts/World/World_Buckets.cs',"Assets/_Scripts/Entity's/Player/Player_Buckets.cs",'Tools/Shared/UnityVectors.cs','Tools/BucketChecks/Check.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(root/s) for s in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)
player=(root/"Assets/_Scripts/Entity's/Player/Player.cs").read_text()
assert 'if (TryUseBucket()) return;' in player
print('Production right-click input is wired to bucket use.')

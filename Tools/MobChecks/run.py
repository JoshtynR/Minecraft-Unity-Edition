import argparse, pathlib, subprocess, tempfile
root=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as folder:
    binary=pathlib.Path(folder)/'mobs.exe'
    sources=['Assets/_Scripts/ItemType.cs','Assets/_Scripts/World/CowState.cs','Assets/_Scripts/World/World_HostileMobs.cs','Tools/MobChecks/Stub.cs','Tools/MobChecks/Check.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(root/s) for s in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)

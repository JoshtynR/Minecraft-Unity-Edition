import argparse, subprocess, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    binary=Path(tmp)/'check.exe'
    clock='Assets/_Scripts/World/SkyClock.cs'
    sources=['Assets/_Scripts/ItemType.cs','Assets/_Scripts/SurvivalActions.cs',"Assets/_Scripts/Entity's/Player/Player_AtmosphereSurvival.cs",'Assets/_Scripts/World/World_Atmosphere.cs','Assets/_Scripts/World/Rendering/OverworldAtmosphere.cs','Tools/SurvivalAtmosphereChecks/RuntimeStub.cs','Tools/SurvivalAtmosphereChecks/Check.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),str(root/clock),*[str(root/f) for f in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)

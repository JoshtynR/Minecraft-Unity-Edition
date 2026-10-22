import argparse, pathlib, subprocess, tempfile
root=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as folder:
    folder=pathlib.Path(folder);fixture=folder/'lightmap.tsv';binary=folder/'check.exe'
    subprocess.run(['java',str(root/'Tools/Java18Lighting/Reference.java'),str(fixture)],check=True)
    subprocess.run([a.mcs,'-out:'+str(binary),str(root/'Assets/_Scripts/World/Java18Lighting.cs'),str(root/'Tools/Java18Lighting/Check.cs')],check=True)
    subprocess.run([a.mono,str(binary),str(fixture)],check=True)
    sources=['Assets/_Scripts/World/Java18Lighting.cs','Assets/_Scripts/World/LightTextureCreator.cs','Assets/_Scripts/World/World_Lighting.cs','Assets/_Scripts/World/SkyClock.cs','Assets/_Scripts/WorldGeneration/Beta/Beta173Lighting.cs','Tools/Java18Lighting/RuntimeCheck.cs']
    subprocess.run([a.mcs,'-out:'+str(binary),*[str(root/s) for s in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)

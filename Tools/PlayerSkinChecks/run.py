import argparse, pathlib, subprocess, tempfile
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory() as folder:
    binary=str(pathlib.Path(folder)/'skin.exe')
    subprocess.run([a.mcs,'-out:'+binary,str(root/"Assets/_Scripts/Entity's/Player/Player_Skin.cs"),str(root/'Tools/PlayerSkinChecks/Check.cs')],check=True)
    subprocess.run([a.mono,binary],check=True)

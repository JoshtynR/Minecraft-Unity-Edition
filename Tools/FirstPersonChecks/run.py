import argparse, subprocess, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    binary=Path(tmp)/'first-person.exe'
    sources=['Tools/FirstPersonChecks/UnityStub.cs','Tools/FirstPersonChecks/FirstPersonCheck.cs','Assets/_Scripts/FirstPersonMesh.cs',"Assets/_Scripts/Entity's/Player/FirstPersonView.cs"]
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(root/f) for f in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)
player=(root/"Assets/_Scripts/Entity's/Player/Player.cs").read_text()
start=player[player.index('public override void Start()'):player.index('public void SetMeshLight(')]
assert start.index('AddComponent<FirstPersonView>')>start.index('if (!isLocalPlayer && networkStarted)')
assert start.index('AddComponent<FirstPersonView>')>start.index('meshFilters = GetComponentsInChildren<MeshFilter>();')
assert '  - LocalPlayerModel\n  - FirstPerson\n' in (root/'ProjectSettings/TagManager.asset').read_text()
shader_guid=(root/'Assets/Shaders/FirstPerson.shader.meta').read_text().split('guid: ')[1].splitlines()[0]
assert shader_guid in (root/'Assets/Resources/FirstPersonMaterial.mat').read_text()
print('Local-player attachment, shader resource linkage, and layer registration verified.')

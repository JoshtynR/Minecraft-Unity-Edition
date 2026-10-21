import argparse
import subprocess
import tempfile
from pathlib import Path

root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    binary=Path(tmp)/'audio.exe'
    sources=['Assets/_Scripts/GameAudio.cs','Assets/_Scripts/BlockSoundRules.cs','Assets/_Scripts/PlayerGameplayAudio.cs',
             'Assets/_Scripts/Block/BlockType.cs','Tools/StartupChecks/UnityStub.cs','Tools/AudioChecks/AudioCheck.cs']
    # The loading overlay is independently tested by StartupChecks.
    loading=Path(tmp)/'Loading.cs';loading.write_text('public static class WorldLoadingScreen{public static bool IsLoading;}')
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(root/s) for s in sources],str(loading)],check=True)
    subprocess.run([a.mono,str(binary)],check=True)

sound_root=root/'Assets/Resources/Sounds'
families=['random/classic_hurt','random/chestopen','random/chestclosed','random/pop','random/click','random/door_open','random/door_close',
          'random/fizz','random/glass','random/levelup','liquid/splash','liquid/swim','liquid/water','liquid/lavapop','fire/fire',
          'mob/cow/say','mob/cow/hurt','mob/cow/step','ambient/cave/cave','UI/click']
families += ['dig/'+material for material in ['cloth','grass','gravel','sand','snow','stone','wood']]
families += ['step/'+material for material in ['cloth','grass','gravel','sand','snow','stone','wood','ladder']]
def matches(name,stem):
    return name==stem or name.startswith(stem) and name[len(stem):].isdigit()
for family in families:
    variants=[family]
    if family.startswith('step/'): variants.append(family.replace('step/','step 1/'))
    assert any(any(matches(file.stem,Path(variant).name) for file in (sound_root/Path(variant).parent).glob('*.ogg')) for variant in variants),family
print(str(len(families))+' sound families verified against committed .ogg files, including step 1 fallbacks.')

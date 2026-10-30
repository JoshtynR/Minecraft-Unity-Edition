import argparse, subprocess, tempfile
from pathlib import Path

root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser()
p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');args=p.parse_args()
sleep=(root/"Assets/_Scripts/Entity's/Player/Player_Sleeping.cs").read_text()
def method(name):
    start=sleep.index('    private ',sleep.index(name)-35)
    brace=sleep.index('{',start);depth=1;end=brace+1
    while depth:
        depth+=(sleep[end]=='{')-(sleep[end]=='}');end+=1
    return sleep[start:end]
player='using System.Collections;using Mirror;using UnityEngine;public partial class Player{\n'
player+='\n'.join(line for line in sleep.splitlines() if 'private double SleepClock=>' in line or 'internal bool FullyAsleep=>' in line)
player+='\n'+method('void BeginSleepCompletion(')+'\n'+method('IEnumerator CompleteOfflineSleep(')+'\n}'
server=sleep[sleep.index('public partial class WorldServer'):]
assert 'BeginSleepCompletion(attempt);' in sleep[sleep.index('private void UseBed('):sleep.index('private void BeginSleepCompletion(')]
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);extracted=folder/'SleepProduction.cs'
    extracted.write_text(player+'\n'+server)
    binary=folder/'sleep.exe'
    sources=[root/'Assets/_Scripts/World/World_Atmosphere.cs',root/'Assets/_Scripts/World/SkyClock.cs',root/'Tools/SleepChecks/Check.cs',extracted]
    subprocess.run([args.mcs,'-langversion:latest','-out:'+str(binary),*[str(s) for s in sources]],check=True)
    subprocess.run([args.mono,str(binary)],check=True)

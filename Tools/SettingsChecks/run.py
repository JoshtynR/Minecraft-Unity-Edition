import argparse,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    tmp=Path(tmp);exe=tmp/'settings.exe'
    source=(root/'Assets/_Scripts/World/World_Generation.cs').read_text()
    method=source[source.index('    public void RequestChunkStreaming('):source.index('    private async void RunChunkStreaming()')]
    adapter=tmp/'Streaming.cs';adapter.write_text('using UnityEngine;public partial class World{'+method+'}')
    files=['Tools/Shared/UnityVectors.cs','Tools/SettingsChecks/Check.cs','Assets/_Scripts/Menus/SettingsManager.cs','Assets/_Scripts/Menus/UISlider.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(exe),*[str(root/f) for f in files],str(adapter)],check=True)
    subprocess.run([a.mono,str(exe)],check=True)

import argparse
import subprocess
import tempfile
from pathlib import Path

root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('--mcs',default='mcs')
parser.add_argument('--mono',default='mono')
args=parser.parse_args()
with tempfile.TemporaryDirectory(prefix='world-startup-') as temporary:
    temporary=Path(temporary)
    source=(root/'Assets/_Scripts/World/World_Serialization.cs').read_text()
    metadata=source[source.index('    public async Task EnsureWorldMetadataAsync'):source.index('    public async void SaveWorld()')]
    save=source[source.index('    public async Task SaveDimensionAsync'):source.index('    public void SavePlayer(')]
    production=temporary/'Save.cs'
    production.write_text('using System;using System.IO;using System.Linq;using System.Threading.Tasks;using System.Diagnostics;using Environment=SaveEnvironment;using Debug=UnityEngine.Debug;using UnityEngine;public partial class World{'+metadata+save+'}')
    binary=temporary/'startup.exe'
    subprocess.run([args.mcs,'-langversion:latest','-out:'+str(binary),str(production),str(root/'Tools/StartupChecks/SaveCheck.cs')],check=True)
    subprocess.run([args.mono,str(binary),str(temporary/'saves')],check=True)

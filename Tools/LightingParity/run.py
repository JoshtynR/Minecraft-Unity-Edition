#!/usr/bin/env python3
import argparse,subprocess,tempfile,gzip
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');a=ap.parse_args()
 with tempfile.TemporaryDirectory() as t:
  t=Path(t);helper=(ROOT/'Assets/_Scripts/World/Rendering/BlockHelper.cs').read_text()
  method=helper[helper.index('    public static void GetFaceLightingAndAO'):helper.index('    public static int GetLightIndex')]
  (t/'BlockHelper.cs').write_text('using System;using UnityEngine;public static class BlockHelper{'+method+'}')
  section=(ROOT/'Assets/_Scripts/World/ChunkSection.cs').read_text();axes=section[section.index('    public static void ChooseFaceAxes'):]
  (t/'ChunkSection.cs').write_text('using UnityEngine;public class ChunkSection{'+axes)
  sources=['Assets/_Scripts/WorldGeneration/Beta/Beta173Lighting.cs','Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/BlockShape.cs','Assets/_Scripts/World/Rendering/MeshData.cs','Assets/_Scripts/World/Lighting.cs','Tools/LightingParity/LightingCheck.cs']
  binary=t/'lighting.exe'
  subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(ROOT/s) for s in sources],str(t/'BlockHelper.cs'),str(t/'ChunkSection.cs')],check=True)
  fixture=t/'fixtures.tsv';fixture.write_bytes(gzip.decompress((ROOT/'Tools/LightingParity/fixtures.tsv.gz').read_bytes()))
  subprocess.run([a.mono,str(binary),str(fixture)],check=True)
if __name__=='__main__':main()

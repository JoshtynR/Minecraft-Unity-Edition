#!/usr/bin/env python3
"""Compile production water/block/save code with Unity/Mirror storage doubles."""
import argparse
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--mcs', default='mcs')
    ap.add_argument('--mono', default='mono')
    args = ap.parse_args()
    with tempfile.TemporaryDirectory(prefix='water-check-') as tmp:
        tmp = Path(tmp)
        # Mono 6 lacks C# 8 switch expressions. Lower just this expression,
        # retaining the exact production mapping branches.
        mapping = (ROOT / 'Assets/_Scripts/Block/BlockMapping.cs').read_text(encoding='utf-8-sig')
        mapping = mapping.replace('return type switch\n        {', 'switch (type)\n        {')
        mapping = mapping.replace('BlockType.Water => new WaterBlock(block),', 'case BlockType.Water: return new WaterBlock(block);')
        mapping = mapping.replace('BlockType.WoodenSlab => new SlabBlock(block),', 'case BlockType.WoodenSlab: return new SlabBlock(block);')
        mapping = mapping.replace('_ => block.Reset(block)', 'default: return block.Reset(block);')
        (tmp / 'BlockMapping.cs').write_text(mapping)
        # Compile the complete production block/RPC region. The transport is
        # mocked, and the send is recorded before the host executes its RPC.
        server = (ROOT / 'Assets/_Scripts/World/WorldServer.cs').read_text(encoding='utf-8-sig')
        server = server[server.index('    #region Block'):server.index('    #endregion')]
        server = server.replace('    #region Block', '')
        server = server.replace('RpcWaterUpdates(positions, levels);', 'RecordSend(positions, levels); RpcWaterUpdates(positions, levels);')
        (tmp / 'WorldServer.cs').write_text('using UnityEngine; using Mirror; public partial class WorldServer {\n' + server + '\n}')
        helper=(ROOT/'Assets/_Scripts/World/Rendering/BlockHelper.cs').read_text(encoding='utf-8-sig')
        height=helper[helper.index('    private static void SetWaterFaceHeight'):helper.index('    public static void GetFaceLightingAndAO')]
        height=height.replace('private static void SetWaterFaceHeight','public static void SetWaterFaceHeight')
        (tmp/'WaterMeshCheck.cs').write_text('using UnityEngine; public static class WaterMeshCheck {\n'+height+'\n}')
        world=(ROOT/'Assets/_Scripts/World/World.cs').read_text()
        method=world[world.index('    public async void SetBlocks(ChunkRenderer'):world.index('    public Vector3Int GetBlockPos(')]
        (tmp/'WorldStairPlacement.cs').write_text('using System.Collections.Generic;using System.Linq;using UnityEngine;public partial class World{'+method+'}')
        files = ['World/WaterSimulation.cs','World/WaterSurface.cs','World/World_Water.cs','World/World_Trees.cs',
                 'Block/BlockType.cs','Block/StairGeometry.cs','Block/BlockShapes.cs','Block/BlockShape.cs','World/Rendering/MeshData.cs','Block/Block.cs','Block/Blocks/WaterBlock.cs',
                 'World/ChunkSection.cs','World/Saving/BlockSaveData.cs','World/Saving/ChunkSectionSaveData.cs']
        sources = [ROOT / 'Assets/_Scripts' / f for f in files]
        sources += [ROOT / 'Tools/WaterChecks/WaterCheck.cs', ROOT / 'Tools/WaterChecks/IntegrationCheck.cs',tmp/'BlockMapping.cs',tmp/'WorldServer.cs',tmp/'WaterMeshCheck.cs',tmp/'WorldStairPlacement.cs']
        binary=tmp/'water.exe'
        subprocess.run([args.mcs,'-langversion:latest','-checked+',f'-out:{binary}',*map(str,sources)],check=True)
        subprocess.run([args.mono,str(binary)],check=True)

if __name__=='__main__': main()

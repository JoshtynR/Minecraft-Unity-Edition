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
        height=height.replace('private static void AddFluidUVs','public static void AddFluidUVs')
        (tmp/'WaterMeshCheck.cs').write_text('using UnityEngine; public static class WaterMeshCheck {\n'+height+'\n}')
        world=(ROOT/'Assets/_Scripts/World/World.cs').read_text()
        method=world[world.index('    public async void SetBlocks(ChunkRenderer'):world.index('    public Vector3Int GetBlockPos(')]
        (tmp/'WorldStairPlacement.cs').write_text('using System.Collections.Generic;using System.Linq;using UnityEngine;public partial class World{'+method+'}')
        helper=(ROOT/'Assets/_Scripts/World/WorldDataHelper.cs').read_text()
        method=helper[helper.index('    public static void SetBlock(World world,'):helper.index('    [CanBeNull]',helper.index('    public static void SetBlock(World world,'))]
        (tmp/'PopulationRecordCleanup.cs').write_text('using UnityEngine;public static partial class WorldDataHelper{'+method+'}')
        files = ['WheatCropRules.cs','World/WaterSimulation.cs','World/WaterSurface.cs','World/World_Water.cs','World/World_Trees.cs','World/FluidTickScheduler.cs','World/BetaFluidPhysics.cs','World/BetaFireSimulation.cs','World/World_FluidRuntime.cs',
                 'WorldGeneration/Beta/JavaRandom.cs','WorldGeneration/Beta/BetaPopulationRecord.cs','Block/BlockType.cs','Block/StairGeometry.cs','Block/SpecialBlockGeometry.cs','Block/BlockShapes.cs','Block/BlockShape.cs','World/Rendering/MeshData.cs','Block/Block.cs','Block/Blocks/WaterBlock.cs',
                 'World/ChunkSection.cs','World/Saving/BlockSaveData.cs','World/Saving/ChunkSectionSaveData.cs']
        sources = [ROOT / 'Assets/_Scripts' / f for f in files]
        sources.extend([ROOT/'Assets/_Scripts/BlockSoundRules.cs',ROOT/'Tools/AudioChecks/SilentGameAudio.cs'])
        sources.append(ROOT/'Tools/WaterChecks/FluidFeatureCheck.cs')
        sources += [ROOT / 'Tools/WaterChecks/WaterCheck.cs',ROOT/'Tools/WaterChecks/LavaCheck.cs', ROOT / 'Tools/WaterChecks/IntegrationCheck.cs',ROOT/'Tools/WaterChecks/LavaIntegrationCheck.cs',tmp/'BlockMapping.cs',tmp/'WorldServer.cs',tmp/'WaterMeshCheck.cs',tmp/'WorldStairPlacement.cs',tmp/'PopulationRecordCleanup.cs']
        binary=tmp/'water.exe'
        subprocess.run([args.mcs,'-langversion:latest','-checked+',f'-out:{binary}',*map(str,sources)],check=True)
        subprocess.run([args.mono,str(binary)],check=True)
        visual=tmp/'visual.exe'
        production=['World/WaterSimulation.cs','World/FluidTickScheduler.cs','World/BetaFluidPhysics.cs','World/FluidWorldQueries.cs','World/World_FluidEffects.cs','World/Rendering/FluidEffects.cs','World/Rendering/FluidCameraEffects.cs',"Entity's/Player/Player_Fluids.cs","Entity's/Player/PlayerHudAssets.cs",'Block/BlockType.cs']
        production.append('Menus/MenuManagers/EscapeMenuManager.cs')
        production.append('BlockSoundRules.cs')
        subprocess.run([args.mcs,'-langversion:latest',f'-out:{visual}',str(ROOT/'Tools/AudioChecks/SilentGameAudio.cs'),str(ROOT/'Tools/WaterChecks/FluidVisualCheck.cs'),*[str(ROOT/'Assets/_Scripts'/p) for p in production]],check=True)
        subprocess.run([args.mono,str(visual)],check=True)

if __name__=='__main__': main()

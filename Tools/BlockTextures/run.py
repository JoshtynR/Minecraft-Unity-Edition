#!/usr/bin/env python3
import argparse,json,re,subprocess,tempfile
from pathlib import Path
import yaml
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');args=ap.parse_args()
    manifest=json.loads((ROOT/'Tools/BlockTextures/migration.json').read_text())
    sources={}
    for entry in manifest:
        atlas=Image.open(ROOT/entry['atlas']).convert('RGBA')
        actual=Image.open(ROOT/entry['path']).convert('RGBA')
        expected=atlas.crop(tuple(entry['box']))
        assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),entry['path']
        meta=(ROOT/entry['path']).with_suffix('.png.meta').read_text()
        assert 'guid: '+entry['guid'] in meta
        sources[entry['guid']]=entry['path']
    text=(ROOT/'Assets/Block Data.asset').read_text();asset=yaml.safe_load(text[text.index('MonoBehaviour:\n'):])['MonoBehaviour']
    assert isinstance(asset,dict),'Unity YAML nesting'
    blocks=asset['textureDataList'];assert len({b['blockType'] for b in blocks})==len(blocks)
    count=0
    for b in blocks:
        assert set(b['textureData'])=={'top','bottom','side'}
        assert 'textureSizeOverride' not in b
        for ref in b['textureData'].values():
            if b['blockType'] in (0,1) or b['blockType']>20:assert ref['fileID']==0
            else:assert ref['fileID']==2800000 and ref['guid'] in sources;count+=1
    assert 'textureSizeX' not in asset and 'textureSizeY' not in asset
    for name in ['Voxel','VoxelVertexLit','VoxelWater']:
        shader=(ROOT/f'Assets/Shaders/{name}.shader').read_text()
        if '#include "VoxelLighting.cginc"' in shader:shader+='\n'+(ROOT/'Assets/Shaders/VoxelLighting.cginc').read_text()
        assert 'UNITY_SAMPLE_TEX2DARRAY(_BlockFaceTextures, i.uv)' in shader
        assert 'float3 uv' in shader and '#pragma require 2darray' in shader
        vertex=shader[re.search(r'\bv2f\s+vert\s*\(',shader).start():re.search(r'\b(?:float4|fixed4)\s+frag\s*\(',shader).start()]
        assert 'AnimatedBlockFaceUV(v.uv)' in vertex
        animation=(ROOT/'Assets/Shaders/BlockFaceAnimation.cginc').read_text()
        assert 'tex2Dlod(_BlockFaceAnimations' in animation and 'floor(_Time.y * timing.z)' in animation
        assert 'uv.z = timing.x + frame;' in animation
        assert 'frame = min(frame, cycle - frame)' in animation
    print(f'{len(manifest)} lossless image crops and {count} face image references verified.')
    helper=(ROOT/'Assets/_Scripts/World/Rendering/BlockHelper.cs').read_text()
    uvs=helper[helper.index('    public static Vector3[] FaceUVs'):]
    with tempfile.TemporaryDirectory() as tmp:
        tmp=Path(tmp);(tmp/'BlockHelper.cs').write_text('using UnityEngine; public static class BlockHelper {\n'+uvs)
        block=(ROOT/'Assets/_Scripts/Block/Block.cs').read_text()
        loaded=block[block.index('    public void Loaded()'):block.index('    public int GetBlockLight()')]
        (tmp/'LoadingProbe.cs').write_text('public partial class LoadingProbe{'+loaded+'}')
        binary=tmp/'textures.exe'
        files=[ROOT/'Tools/BlockTextures/TextureCheck.cs',ROOT/'Assets/_Scripts/BlockDataSO.cs',ROOT/'Assets/_Scripts/SpecialBlockTextures.cs',ROOT/'Assets/_Scripts/BlockDataManager.cs',ROOT/'Assets/_Scripts/BlockDataDefaults.cs',ROOT/'Assets/_Scripts/Block/BlockType.cs',tmp/'BlockHelper.cs',ROOT/'Tools/BlockTextures/LoadingProbe.cs',tmp/'LoadingProbe.cs']
        subprocess.run([args.mcs,'-langversion:latest','-out:'+str(binary),*map(str,files)],check=True)
        subprocess.run([args.mono,str(binary)],check=True)
        importer=tmp/'import.exe'
        subprocess.run([args.mcs,'-langversion:latest','-main:ImportCheck','-out:'+str(importer),*map(str,files),str(ROOT/'Tools/BlockTextures/ImportCheck.cs'),str(ROOT/'Assets/Editor/AnimatedBlockTextureImports.cs')],check=True)
        subprocess.run([args.mono,str(importer)],check=True)
if __name__=='__main__':main()

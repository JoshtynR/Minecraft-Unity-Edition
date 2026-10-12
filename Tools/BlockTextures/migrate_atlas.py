#!/usr/bin/env python3
"""One-time lossless migration of the legacy Block Data atlas references.

Run against an OLD-schema asset, before installing the new BlockDataSO schema.
Only crops pixels; no resampling, tinting, or replacement textures.
Requires Pillow and PyYAML.
"""
import argparse
import json
import re
import uuid
from pathlib import Path
import yaml
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--asset', type=Path, default=ROOT / 'Assets/Block Data.asset')
    ap.add_argument('--atlas', type=Path, default=ROOT / 'Assets/Textures/sprites.png')
    args = ap.parse_args()
    text = args.asset.read_text()
    header, body = text.split('MonoBehaviour:\n', 1)
    data = yaml.safe_load(body)
    if 'textureSizeX' not in data:
        raise SystemExit('Already migrated: no atlas coordinates to convert.')
    atlas = Image.open(args.atlas).convert('RGBA')
    folder = ROOT / 'Assets/Textures/Blocks'
    folder.mkdir(exist_ok=True)
    (folder.with_suffix('.meta')).write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    meta_template = args.atlas.with_suffix('.png.meta').read_text().split('  spriteSheet:', 1)[0]
    meta_template = meta_template.replace('  isReadable: 0', '  isReadable: 1').replace('  spriteMode: 1', '  spriteMode: 0').replace('    textureCompression: 1', '    textureCompression: 0')
    names = ['nothing','air','dirt','grass','stone','oak_log','oak_leaves','sand','water','glass','glowstone','wooden_slab','bedrock','gravel','sandstone','lava','ice','spruce_log','spruce_leaves','birch_log','birch_leaves']
    regions, manifest = {}, []
    sx, sy = data.pop('textureSizeX'), data.pop('textureSizeY')
    for item in data['textureDataList']:
        old = item['textureData']
        override = item.pop('textureSizeOverride', {'x': 0, 'y': 0})
        tx, ty = override.get('x') or sx, override.get('y') or sy
        faces = {}
        for legacy, face in [('up','top'), ('down','bottom'), ('side','side')]:
            if item['blockType'] in (0, 1):
                faces[face] = {'fileID': 0}
                continue
            pos, extent = old[legacy], old[legacy+'Extends']
            x = round(pos['x'] * tx * atlas.width)
            y = round(pos['y'] * ty * atlas.height)
            w = round(extent['x'] * tx * atlas.width)
            h = round(extent['y'] * ty * atlas.height)
            assert 0 <= x < x+w <= atlas.width and 0 <= y < y+h <= atlas.height
            box = (x, atlas.height-y-h, x+w, atlas.height-y)
            if box not in regions:
                name = names[item['blockType']]+'_'+face+'.png'
                path = folder / name
                guid = uuid.uuid4().hex
                atlas.crop(box).save(path)
                meta = re.sub(r'guid: \w+', 'guid: '+guid, meta_template, count=1)
                path.with_suffix('.png.meta').write_text(meta)
                regions[box] = guid
                manifest.append({'path':str(path.relative_to(ROOT)), 'atlas':str(args.atlas.relative_to(ROOT)), 'box':box, 'guid':guid})
            faces[face] = {'fileID':2800000, 'guid':regions[box], 'type':3}
        item['textureData'] = faces
    # Keep Unity's original header and object references; emit references inline.
    class Dumper(yaml.SafeDumper): pass
    def mapping(d, value):
        return d.represent_mapping('tag:yaml.org,2002:map',value,flow_style='fileID' in value or set(value) == {'r','g','b','a'})
    Dumper.add_representer(dict,mapping)
    data['faceTextureResolution'] = 32
    dumped = yaml.dump(data,Dumper=Dumper,sort_keys=False,indent=2)
    args.asset.write_text(header+'MonoBehaviour:\n'+''.join('  '+line+'\n' for line in dumped.splitlines()))
    (ROOT/'Tools/BlockTextures/migration.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(f'Migrated {len(data["textureDataList"])} blocks into {len(regions)} separate face images.')

if __name__ == '__main__': main()

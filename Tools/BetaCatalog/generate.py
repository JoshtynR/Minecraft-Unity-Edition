"""Build texture-ready catalog from the pinned reconstructed Beta registry.
Existing IDs and serialized texture entries are preserved; new entries append.
"""
import json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ref=Path(sys.argv[1])/'1.7.3-LTS/src/minecraft/net/minecraft/src'
source=(ref/'Block.java').read_text()
# Names describe distinct selectable texture variants, not every transient state.
names={1:'Stone',2:'Grass',3:'Dirt',4:'Cobblestone',5:'WoodenPlanks',6:'OakSapling',7:'Bedrock',8:'Water',9:'Water',10:'Lava',11:'Lava',12:'Sand',13:'Gravel',14:'GoldOre',15:'IronOre',16:'CoalOre',17:'Log',18:'Leaves',19:'Sponge',20:'Glass',21:'LapisOre',22:'LapisBlock',23:'Dispenser',24:'Sandstone',25:'NoteBlock',26:'Bed',27:'PoweredRail',28:'DetectorRail',29:'StickyPiston',30:'Cobweb',31:'TallGrass',32:'DeadBush',33:'Piston',34:'PistonHead',35:'WhiteWool',36:'MovingPiston',37:'Dandelion',38:'Rose',39:'BrownMushroom',40:'RedMushroom',41:'GoldBlock',42:'IronBlock',43:'DoubleStoneSlab',44:'StoneSlab',45:'Bricks',46:'TNT',47:'Bookshelf',48:'MossyCobblestone',49:'Obsidian',50:'Torch',51:'Fire',52:'MonsterSpawner',53:'WoodenStairs',54:'Chest',55:'RedstoneWire',56:'DiamondOre',57:'DiamondBlock',58:'CraftingTable',59:'WheatCrop0',60:'Farmland',61:'Furnace',62:'LitFurnace',63:'StandingSign',64:'WoodenDoor',65:'Ladder',66:'Rail',67:'CobblestoneStairs',68:'WallSign',69:'Lever',70:'StonePressurePlate',71:'IronDoor',72:'WoodenPressurePlate',73:'RedstoneOre',74:'LitRedstoneOre',75:'RedstoneTorchOff',76:'RedstoneTorch',77:'StoneButton',78:'SnowLayer',79:'Ice',80:'SnowBlock',81:'Cactus',82:'Clay',83:'SugarCane',84:'Jukebox',85:'Fence',86:'Pumpkin',87:'Netherrack',88:'SoulSand',89:'GlowStone',90:'NetherPortal',91:'JackOLantern',92:'Cake',93:'RedstoneRepeater',94:'PoweredRepeater',95:'LockedChest',96:'Trapdoor'}
nonopaque={0,6,8,9,10,11,18,20,26,27,28,29,30,31,32,33,34,36,37,38,39,40,43,44,50,51,52,53,55,59,60,63,64,65,66,67,68,69,70,71,72,75,76,77,78,79,81,83,85,90,92,93,94,96}
no_collision={0,6,8,9,10,11,27,28,30,31,32,36,37,38,39,40,50,51,55,59,63,66,68,69,70,72,75,76,77,78,83,90}
logic={0,6,27,28,31,32,37,38,39,40,50,51,55,59,65,66,69,75,76,77,78,83,90,93,94}
no_hand={1,4,7,14,15,16,21,22,23,24,30,36,41,42,43,44,45,48,49,52,56,57,61,62,67,70,71,73,74,78,80,87,89,90}
no_block_drop={18,20,30,34,36,39,40,47,51,55,59,63,68,73,74,78,79,80,82,90,92}
records={0:dict(name='Air',sourceId=0,metadata=0,hardness=-1,handHarvestable=False,transparent=True,collider=False,opacity=0,emission=0,cornerPass=True,halfSlab=False,drop='Nothing')}
for line in source.splitlines():
 m=re.search(r'^\s+(\w+) = .*new Block\w*\((\d*)',line)
 if not m:continue
 id=int(m[2]) if m[2] else (35 if m[1]=='cloth' else -1)
 if id not in names:continue
 hard=re.search(r'\.setHardness\((-?[\d.]+)F\)',line)
 h=float(hard[1]) if hard else 0.0
 if '.setBlockUnbreakable()' in line:h=-1
 if id in (29,33,34):h=0.5
 if id==36:h=-1
 if id in (53,67):h=2
 if id in (8,9,10,11):h=-1
 light=re.search(r'\.setLightValue\(([\d.]+)F\)',line)
 emission=int(float(light[1])*15) if light else 0
 op=re.search(r'\.setLightOpacity\((\d+)\)',line)
 opacity=min(15,int(op[1])) if op else (0 if id in nonopaque else 15)
 if id in (43,44,53,60,67):opacity=15
 if id==18:opacity=1
 drop='Nothing' if id in no_hand or id in no_block_drop else names[id]
 if id==2:drop='Dirt'
 if id==60:drop='Dirt'
 if id==62:drop='Nothing'
 if id==75:drop='RedstoneTorch'
 if id==94:drop='RedstoneRepeater'
 records[id]=dict(name=names[id],sourceId=id,metadata=0,hardness=h,handHarvestable=id not in no_hand and id not in (0,8,9,10,11),transparent=id in nonopaque,collider=id not in no_collision,opacity=opacity,emission=emission,cornerPass=id in logic,halfSlab=id==44,drop=drop)
assert set(records)==set(range(97)),set(range(97))-set(records)
# Preserve all preexisting internal IDs, including split tree species.
existing=['Nothing','Air','Dirt','Grass','Stone','Log','Leaves','Sand','Water','Glass','GlowStone','WoodenSlab','Bedrock','Gravel','Sandstone','Lava','Ice','SpruceLog','SpruceLeaves','BirchLog','BirchLeaves']
entries=[dict(name='Nothing',sourceId=-1,metadata=0,hardness=-1,handHarvestable=False,transparent=True,collider=False,opacity=0,emission=0,cornerPass=True,halfSlab=False,drop='Nothing')]
byname={v['name']:dict(v) for k,v in records.items() if k not in (8,10)}
def variant(name,base,metadata,**extra):
 r=dict(records[base]);r.update(name=name,metadata=metadata,**extra);byname[name]=r
variant('WoodenSlab',44,2)
for name,id,meta in [('SpruceLog',17,1),('BirchLog',17,2),('SpruceLeaves',18,1),('BirchLeaves',18,2)]:variant(name,id,meta,drop=name if id==17 else 'Nothing')
for n in existing[1:]:entries.append(byname[n])
for id in range(97):
 n=records[id]['name']
 if n not in existing and n not in [e['name'] for e in entries]:entries.append(byname[n])
variants=[('SpruceSapling',6,1),('BirchSapling',6,2),('DryTallGrass',31,0),('Fern',31,2),('SandstoneSlab',44,1),('CobblestoneSlab',44,3),('DoubleSandstoneSlab',43,1),('DoubleWoodenSlab',43,2),('DoubleCobblestoneSlab',43,3),('MoistFarmland',60,7)]
# TallGrass base variant is metadata 1. White wool metadata 0 remains base.
for e in entries:
 if e['name']=='TallGrass':e['metadata']=1
for i,color in enumerate(['White','Orange','Magenta','LightBlue','Yellow','Lime','Pink','Gray','LightGray','Cyan','Purple','Blue','Brown','Green','Red','Black']):
 if i:variants.append((color+'Wool',35,i))
for i in range(1,8):variants.append(('WheatCrop'+str(i),59,i))
for name,base,meta in variants:
 variant(name,base,meta)
 r=byname[name]
 if base==6:r['drop']=name
 if base==35:r['drop']=name
 entries.append(r)
for i,e in enumerate(entries):e['localId']=i
# Explicit values mean future catalog changes cannot silently shift saves.
p=ROOT/'Assets/_Scripts/Block/BlockType.cs'
p.write_text('using System;\n\n[Serializable]\npublic enum BlockType\n{\n'+''.join('    '+e['name']+' = '+str(e['localId'])+',\n' for e in entries)+'}\n')
(ROOT/'Tools/BetaCatalog/catalog.json').write_text(json.dumps({'referenceCommit':'740c583901e1ff1150e9ef37e37dab5bc0e4f807','sourceAliases':{'8':'Water','10':'Lava'},'entries':entries},indent=2)+'\n')
# Append new serialized entries with unassigned face images, retaining old bytes.
p=ROOT/'Assets/Block Data.asset';asset=p.read_text()
serialized_ids={int(i) for i in re.findall(r'^  - blockType: (\d+)$',asset,re.M)}
for e in entries[21:]:
 if e['localId'] in serialized_ids:continue
 asset+=f'''  - blockType: {e['localId']}
    textureData:
      top: {{fileID: 0}}
      bottom: {{fileID: 0}}
      side: {{fileID: 0}}
    isTransparent: {int(e['transparent'])}
    generateCollider: {int(e['collider'])}
    opacity: {e['opacity']}
    lightEmission: {e['emission']}
    albedoTint: {{r: 1, g: 1, b: 1, a: 1}}
'''
p.write_text(asset)
cs='''using System;

// Texture-ready Beta registry. Source IDs/metadata document vanilla identity;
// local enum IDs are the existing project's save format, not vanilla IDs.
public static class BetaBlockCatalog
{
    public struct Entry
    {
        public readonly BlockType Type, Drop;
        public readonly int SourceId, Metadata;
        public readonly float Hardness;
        public readonly bool HandHarvestable, CornerPass, HalfSlab;
        public Entry(BlockType type, int sourceId, int metadata, float hardness, bool harvestable, bool cornerPass, bool halfSlab, BlockType drop)
        { Type=type; SourceId=sourceId; Metadata=metadata; Hardness=hardness; HandHarvestable=harvestable; CornerPass=cornerPass; HalfSlab=halfSlab; Drop=drop; }
    }
    public static readonly Entry[] Entries =
    {
'''
b=lambda v:str(v).lower()
for e in entries:cs+=f"        new Entry(BlockType.{e['name']}, {e['sourceId']}, {e['metadata']}, {e['hardness']}f, {b(e['handHarvestable'])}, {b(e['cornerPass'])}, {b(e['halfSlab'])}, BlockType.{e['drop']}),\n"
cs+='''    };
    public static bool TryGet(BlockType type, out Entry entry)
    {
        int id = (int)type;
        if (id >= 0 && id < Entries.Length && Entries[id].Type == type) { entry = Entries[id]; return true; }
        entry = default(Entry); return false;
    }
    public static bool TryGetVanilla(int sourceId, int metadata, out BlockType type)
    {
        if (sourceId == 8) sourceId = 9;
        if (sourceId == 10) sourceId = 11;
        if (sourceId == 17 || sourceId == 18 || sourceId == 6) metadata &= 3;
        else if (sourceId == 35) metadata &= 15;
        else if (sourceId == 43 || sourceId == 44) metadata &= 3;
        else if (sourceId == 60) metadata = metadata > 0 ? 7 : 0;
        else if (sourceId != 31 && sourceId != 59) metadata = 0;
        foreach (var entry in Entries)
            if (entry.SourceId == sourceId && entry.Metadata == metadata) { type=entry.Type; return true; }
        type=BlockType.Nothing; return false;
    }
}
'''
(ROOT/'Assets/_Scripts/Block/BetaBlockCatalog.cs').write_text(cs)
print(f'{len(entries)} entries, {len(entries)-21} additions; all 97 vanilla IDs represented (fluid aliases shared).')

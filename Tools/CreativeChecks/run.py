import argparse, csv, re, subprocess, tempfile
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs'); p.add_argument('--mono', default='mono')
p.add_argument('--custom-assets', action='store_true', help='Validate editable item asset ranges instead of requiring stock Beta values.')
a = p.parse_args()
fixture=root/'Tools/CreativeChecks/beta-items.tsv'
expected=list(csv.DictReader(fixture.open(),delimiter='\t'))
enum=dict((n,int(i)) for n,i in re.findall(r'(\w+) = (\d+)',(root/'Assets/_Scripts/ItemType.cs').read_text()))
asset=(root/'Assets/Resources/Item Data.asset').read_text()
parts=re.split(r'^  - id: ',asset,flags=re.M)[1:]
actual={}
for part in parts:
    ident=int(part.splitlines()[0])
    assert ident not in actual, 'Duplicate item ID in asset'
    actual[ident]=dict(re.findall(r'^    (\w+): (.*)$',part,re.M))
assert set(actual)==set(enum.values())-{0,1}, 'Asset must contain every registered item'
for entry in expected:
    definition=actual[enum[entry['item_type']]]
    if not a.custom_assets:
        assert int(definition['maxStack'])==int(entry['stack_limit'])
        assert int(definition['maxDamage'])==int(entry['max_damage'])
    else:
        assert 1 <= int(definition['maxStack']) <= 64 and int(definition['maxDamage']) >= 0
assert len(actual)==251 and len(expected)==122
print('251 asset entries verified ('+('custom item values preserved' if a.custom_assets else '122 Beta item/variant defaults')+').',flush=True)
with tempfile.TemporaryDirectory() as tmp:
    binary = Path(tmp) / 'creative.exe'
    server=(root/'Assets/_Scripts/World/WorldServer.cs').read_text()
    start=server.index('    [Serializable]\n    public struct SavePlayerMessage')
    message=Path(tmp)/'SaveMessage.cs'
    message.write_text('using System;using UnityEngine;using Mirror;public partial class WorldServer{'+server[start:server.index('    #endregion',start)]+'}')
    subprocess.run([a.mcs, '-langversion:latest', '-out:' + str(binary),
        str(root / 'Assets/_Scripts/World/DimensionRules.cs'),
        str(root / 'Assets/_Scripts/World/GrassSpreadRules.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Creative.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Storage.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Preview.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Chest.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Crafting.cs"),
        str(root / 'Assets/_Scripts/BetaCraftingRecipes.cs'),
        str(root / 'Tools/CreativeChecks/CraftingCheck.cs'),
        str(root / 'Assets/_Scripts/DroppedItemRules.cs'),
        str(root / 'Assets/_Scripts/World/BetaFluidPhysics.cs'),
        str(root / 'Assets/_Scripts/World/WaterSimulation.cs'),
        str(root / 'Assets/_Scripts/World/FluidTickScheduler.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/HandMining.cs"),
        str(root / 'Assets/_Scripts/Block/BetaBlockCatalog.cs'),
        str(root / 'Assets/_Scripts/ItemVisualMesh.cs'),
        str(root / 'Assets/_Scripts/ToolMining.cs'),
        str(root / 'Assets/_Scripts/ItemDurability.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Durability.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/Player_Durability.cs"),
        str(root / 'Tools/CreativeChecks/DurabilityCheck.cs'),
        str(root / 'Assets/_Scripts/World/World_DroppedItems.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/DroppedItemVisual.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/Player_DroppedItems.cs"),
        str(root / 'Tools/CreativeChecks/DropCheck.cs'),
        str(root / 'Assets/_Scripts/ChestTransactions.cs'),
        str(root / 'Assets/_Scripts/BetaSmelting.cs'),
        str(root / 'Assets/_Scripts/WheatCropRules.cs'),
        str(root / 'Assets/_Scripts/BlockSoundRules.cs'),str(root / 'Tools/AudioChecks/SilentGameAudio.cs'),
        str(root / 'Assets/_Scripts/MinecraftUIText.cs'),
        str(root / 'Assets/_Scripts/World/MobState.cs'),
        str(root / 'Assets/_Scripts/World/MobDefinition.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/MobModelGeometry.cs'),
        str(root / 'Tools/CreativeChecks/MobCheck.cs'),
        str(root / 'Assets/_Scripts/World/World_Farming.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/Player_Farming.cs"),
        str(root / 'Assets/_Scripts/WorldGeneration/Beta/JavaRandom.cs'),
        str(root / 'Tools/CreativeChecks/FarmingCheck.cs'),
        str(root / 'Assets/_Scripts/World/FurnaceStorage.cs'),
        str(root / 'Assets/_Scripts/World/World_Furnaces.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Furnace.cs"),
        str(root / 'Tools/CreativeChecks/FurnaceCheck.cs'),
        str(root / 'Assets/_Scripts/World/ChestStorage.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/ChestModelGeometry.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/ModernChestVisual.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/ChunkRenderer.cs'),
        str(root / 'Assets/_Scripts/World/World_Chests.cs'),
        str(root / 'Assets/_Scripts/World/VoxelLightingMath.cs'),
        str(root / 'Tools/CreativeChecks/ChestCheck.cs'),
        str(root / "Assets/_Scripts/Entity's/Player/InventoryPlayerGraphic.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/InventoryRules.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/InventorySlot.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerHudAssets.cs"),
        str(root / 'Assets/_Scripts/Block/BlockType.cs'),
        str(root / 'Assets/_Scripts/ItemType.cs'),
        str(root / 'Assets/_Scripts/BetaItemCatalog.cs'),
        str(root / 'Tools/CreativeChecks/BetaItemCheck.cs'),
        str(root / 'Assets/_Scripts/ItemDataSO.cs'),
        str(root / 'Tools/CreativeChecks/ItemCheck.cs'),
        str(root / 'Assets/_Scripts/Block/BlockShape.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/MeshData.cs'),
        str(root / 'Tools/CreativeChecks/InventoryCheck.cs'),
        str(root / 'Tools/CreativeChecks/InventoryRulesCheck.cs'),str(message)], check=True)
    subprocess.run([a.mono, str(binary),str(fixture)], check=True)

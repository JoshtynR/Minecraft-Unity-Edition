import argparse, csv, re, subprocess, tempfile
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs'); p.add_argument('--mono', default='mono')
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
    assert int(definition['maxStack'])==int(entry['stack_limit'])
    assert int(definition['maxDamage'])==int(entry['max_damage'])
assert len(actual)==246 and len(expected)==122
print('246 asset entries verified against 122 Beta item/variant defaults.',flush=True)
with tempfile.TemporaryDirectory() as tmp:
    binary = Path(tmp) / 'creative.exe'
    server=(root/'Assets/_Scripts/World/WorldServer.cs').read_text()
    start=server.index('    [Serializable]\n    public struct SavePlayerMessage')
    message=Path(tmp)/'SaveMessage.cs'
    message.write_text('using System;using UnityEngine;using Mirror;public class WorldServer{'+server[start:server.index('    #endregion',start)]+'}')
    subprocess.run([a.mcs, '-langversion:latest', '-out:' + str(binary),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Creative.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Storage.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Preview.cs"),
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

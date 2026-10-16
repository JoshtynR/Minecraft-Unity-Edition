import argparse, subprocess, tempfile
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs'); p.add_argument('--mono', default='mono')
a = p.parse_args()
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
        str(root / "Assets/_Scripts/Entity's/Player/InventoryRules.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/InventorySlot.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerHudAssets.cs"),
        str(root / 'Assets/_Scripts/Block/BlockType.cs'),
        str(root / 'Assets/_Scripts/ItemType.cs'),
        str(root / 'Assets/_Scripts/ItemDataSO.cs'),
        str(root / 'Tools/CreativeChecks/ItemCheck.cs'),
        str(root / 'Assets/_Scripts/Block/BlockShape.cs'),
        str(root / 'Assets/_Scripts/World/Rendering/MeshData.cs'),
        str(root / 'Tools/CreativeChecks/InventoryCheck.cs'),
        str(root / 'Tools/CreativeChecks/InventoryRulesCheck.cs'),str(message)], check=True)
    subprocess.run([a.mono, str(binary)], check=True)

import argparse,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');a=ap.parse_args()
with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'shapes.exe'
 paths=['Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/StairGeometry.cs','Assets/_Scripts/Block/SpecialBlockGeometry.cs','Assets/_Scripts/World/Rendering/SpecialBlockMesh.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs',"Assets/_Scripts/Entity's/Player/HandMining.cs","Assets/_Scripts/Entity's/Player/BlockTargeting.cs",'Assets/_Scripts/Block/BlockShape.cs','Assets/_Scripts/Block/BlockShapes.cs','Assets/_Scripts/Block/BlockCollision.cs','Assets/_Scripts/World/Rendering/MeshData.cs','Tools/ShapeChecks/ShapeCheck.cs','Tools/ShapeChecks/StairCheck.cs','Tools/ShapeChecks/PlacementCheck.cs']
 controller=(ROOT/"Assets/_Scripts/Entity's/BaseEntity.cs").read_text()
 paths.append('Tools/ShapeChecks/SpecialCheck.cs')
 paths.extend(['Assets/_Scripts/ItemType.cs','Assets/_Scripts/ItemDataSO.cs','Assets/_Scripts/BetaItemCatalog.cs'])
 methods=controller[controller.index('    public virtual void CalculateVelocity()'):controller.rfind('}')]
 extra=Path(tmp)/'Controller.cs';extra.write_text('using UnityEngine;public partial class Controller{'+methods+'}')
 player=(ROOT/"Assets/_Scripts/Entity's/Player/Player.cs").read_text()
 placement=Path(tmp)/'PlayerPlacement.cs'
 placement.write_text('using UnityEngine;using Mirror;public partial class Player{'+player[player.index('    private bool TryUseBlock()'):player.index('    public Block TargetedBlock(')]+'}')
 subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(ROOT/p) for p in paths],str(extra),str(placement)],check=True)
 subprocess.run([a.mono,str(binary)],check=True)

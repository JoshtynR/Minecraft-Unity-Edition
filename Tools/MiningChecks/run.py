import argparse,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');a=ap.parse_args()
base=ROOT/"Assets/_Scripts/Entity's/Player"
with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'mining.exe'
 sources=[base/n for n in ['HandMining.cs','Player_Mining.cs','BlockBreakOverlay.cs','PlayerHudAssets.cs','HungerSimulation.cs','SurvivalHudValues.cs']]+[ROOT/p for p in ['Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/StairGeometry.cs','Assets/_Scripts/Block/SpecialBlockGeometry.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs','Assets/_Scripts/Block/BlockShape.cs','Assets/_Scripts/Block/BlockShapes.cs','Assets/_Scripts/World/Rendering/MeshData.cs','Tools/MiningChecks/MiningCheck.cs','Assets/_Scripts/ToolMining.cs','Assets/_Scripts/ItemDurability.cs','Assets/_Scripts/ItemType.cs','Assets/_Scripts/ItemDataSO.cs','Assets/_Scripts/BetaItemCatalog.cs',"Assets/_Scripts/Entity's/Player/InventoryRules.cs",'Assets/_Scripts/BetaCraftingRecipes.cs','Assets/_Scripts/ChestTransactions.cs']]
 sources.append(ROOT/'Assets/_Scripts/WheatCropRules.cs')
 sources.extend([ROOT/'Assets/_Scripts/BlockSoundRules.cs',ROOT/'Tools/AudioChecks/SilentGameAudio.cs'])
 subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(s) for s in sources]],check=True)
 subprocess.run([a.mono,str(binary)],check=True)

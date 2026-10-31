import argparse, subprocess, tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as folder:
    binary=Path(folder)/'vehicles.exe'
    # Compile the actual interaction, simulation, collision and visual scripts.
    sources=['Tools/Shared/UnityVectors.cs','Tools/VehicleChecks/Check.cs','Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/ItemType.cs','Assets/_Scripts/World/VehiclePhysics.cs','Assets/_Scripts/World/World_Vehicles.cs','Assets/_Scripts/Block/BlockCollision.cs',"Assets/_Scripts/Entity's/Player/BlockTargeting.cs","Assets/_Scripts/Entity's/Player/Player_Vehicles.cs",'Assets/_Scripts/World/Rendering/VehicleVisual.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(root/s) for s in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)
for path,needle in [('Assets/_Scripts/World/World_UpdateLoop.cs','UpdateVehicles();'),('Assets/_Scripts/World/World.cs','InitializeVehicles();'),('Assets/_Scripts/World/World_Serialization.cs','vehicles = CaptureVehicles()'),('Assets/_Scripts/Menus/WorldSettingsManager.cs','RestoreVehicles('),('Assets/_Scripts/World/World_Dimensions.cs','RestoreVehicles(saved.vehicles)'),("Assets/_Scripts/Entity's/Player/Player.cs",'if(UpdateVehicleSeat())')]:
    assert needle in (root/path).read_text(),path
print('Vehicle startup, player, save and dimension hooks verified.')

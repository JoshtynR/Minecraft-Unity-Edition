import argparse, pathlib, subprocess, tempfile
root=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as folder:
    binary=pathlib.Path(folder)/'mobs.exe'
    sources=['Assets/_Scripts/ItemType.cs','Assets/_Scripts/WorldGeneration/Beta/JavaRandom.cs','Assets/_Scripts/World/HostileSpawnRules.cs','Assets/_Scripts/World/MobState.cs','Assets/_Scripts/World/MobDefinition.cs','Assets/_Scripts/World/MobBrain.cs','Assets/_Scripts/World/MobNavigation.cs','Assets/_Scripts/World/MobEntitySystem.cs','Assets/_Scripts/World/World_MobAI.cs','Assets/_Scripts/World/World_HostileMobs.cs','Tools/MobChecks/Stub.cs','Tools/MobChecks/Check.cs']
    sources+=['Assets/_Scripts/World/Rendering/MobProjectileGeometry.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(root/s) for s in sources]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)
    integration=sources+['Assets/_Scripts/World/World_Mobs.cs','Assets/_Scripts/World/World_MobRendering.cs','Assets/_Scripts/World/Rendering/MobModelGeometry.cs','Assets/_Scripts/World/Rendering/MobAnimation.cs','Assets/_Scripts/World/Rendering/MobVisual.cs',"Assets/_Scripts/Entity's/Player/Player_Mobs.cs",'Tools/MobChecks/IntegrationStub.cs']
    subprocess.run([a.mcs,'-langversion:latest','-define:FULL_MOB_SYSTEM','-out:'+str(binary),*[str(root/s) for s in integration]],check=True)
    subprocess.run([a.mono,str(binary)],check=True)
hostiles=(root/'Assets/_Scripts/World/World_HostileMobs.cs').read_text()
assert 'HostileSpawnRules.Allows(block.GetBlockLight(),block.GetSkyLight(),skyLightReduction,mobRandom.NextInt(8))' in hostiles
assert 'anchorY=mobRandom.NextInt(worldHeight)' in hostiles, 'Hostiles must sample the full-height chunk column'
print('Production hostile placement uses the verified Beta light roll.')

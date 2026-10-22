using System;
using UnityEngine;
class Check {
 static int checks;static void Assert(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 static MobState Mob(World w,MobType type)=>w.mobs.Spawn(new Vector3(0,64,0),0,type);
 static Player Target(World w,float distance){var player=new Player();player.transform.position=new Vector3(distance,64,0);w.Add(player);return player;}
 static void Main(){
  foreach(MobType type in Enum.GetValues(typeof(MobType)))Assert(MobDefinitions.Get(type).Type==type&&MobBrains.Get(type)!=null,"every registered species has a definition and brain");
  foreach(int fps in new[]{30,60,144,240})
  {var system=new MobEntitySystem();int ticks=0;for(int frame=0;frame<fps;frame++){system.Accumulate(1f/fps);while(system.TryStep())ticks++;}Assert(ticks==20,"entity system fixed ticks are frame-rate independent");}
  var capped=new MobEntitySystem();capped.Accumulate(100);int caughtUp=0;while(capped.TryStep())caughtUp++;Assert(caughtUp==5,"long frames have bounded entity catch-up");
  var navigator=new MobNavigation();var navigating=new MobState{id=1,species=MobType.Zombie,position=new Vector3(.5f,64,.5f)};bool detour=false;
  Func<Vector3,bool> walkable=p=>p.y==64&&p.x>=0&&p.x<6&&p.z>=-3&&p.z<4&&!(Math.Floor(p.x)==1&&Math.Abs(Math.Floor(p.z))<=1);
  for(int tick=1;tick<=15;tick++){var next=navigator.Steer(navigating,new Vector3(4.5f,64,.5f),tick,walkable,p=>true);Assert(walkable(next),"navigation never enters blocked wall cells");if(Math.Abs(next.z)>.5f)detour=true;navigating.position=next;}
  Assert(detour&&navigating.position.x>=4,"bounded pathfinding routes around a wall");
  var unavailable=new MobNavigation();int probes=0;unavailable.Steer(navigating,new Vector3(10,64,0),1,p=>{probes++;return true;},p=>true,false);Assert(probes==0,"search budget can defer work without querying terrain");
  var world=new World();var player=Target(world,1);var zombie=Mob(world,MobType.Zombie);
  var runtime=world.MobEntities.Runtime(world,zombie);Assert(ReferenceEquals(runtime,world.MobEntities.Runtime(world,zombie)),"live entity reuses its navigation runtime");
  bool staleRejected=false;try{world.MobEntities.Runtime(world,new MobState{id=zombie.id});}catch(InvalidOperationException){staleRejected=true;}Assert(staleRejected,"foreign state cannot reuse a live entity identity");
  world.MobEntities.RemoveRuntime(zombie.id);Assert(world.MobEntities.RuntimeCount==0,"runtime can be released independently of saved entity state");
  var pushed=new MobState{id=900,health=10,position=zombie.position};MobEntitySystem.PushApart(zombie,pushed);Assert(zombie.motion.x<0&&pushed.motion.x>0&&Math.Abs(zombie.motion.x+pushed.motion.x)<.00001f,"overlapping bodies receive equal opposing pushes");zombie.motion=Vector3.zero;
  world.Step(zombie);Assert(player.health==15&&zombie.attackTicks==20,"zombie deals reference melee damage and starts attack cooldown");
  world.Step(zombie);Assert(player.health==15,"attack cooldown prevents per-frame damage");
  world.betaSkylightSubtracted=0;world.Step(zombie);Assert(zombie.fireTicks==160,"exposed zombies ignite in daylight");
  var skeleton=Mob(world,MobType.Skeleton);world.Step(skeleton);Assert(skeleton.fireTicks==160,"exposed skeletons ignite in daylight");
  world=new World{blocked=true};player=Target(world,1);world.Step(Mob(world,MobType.Zombie));Assert(player.health==20,"solid cover blocks melee acquisition");
  world=new World{betaSkylightSubtracted=0};player=Target(world,1);var spider=Mob(world,MobType.Spider);
  world.Step(spider);Assert(player.health==20,"unprovoked spiders are neutral in bright daylight");
  spider.angerTicks=600;world.Step(spider);Assert(player.health==18,"provoked daylight spider attacks");
  world=new World();player=Target(world,1);player.isFlying=true;world.Step(Mob(world,MobType.Zombie));Assert(player.health==20,"flying players are not hostile targets");
  world=new World();player=Target(world,2);var creeper=Mob(world,MobType.Creeper);
  for(int i=0;i<29;i++)Assert(world.Step(creeper),"creeper does not explode before thirty ticks");
  Assert(creeper.fuseTicks==29&&world.Step(creeper)==false&&world.mobs.Get(creeper.id)==null,"thirtieth fuse tick explodes and removes creeper once");
  Assert(player.health<20,"creeper explosion damages exposed player");
  world=new World();player=Target(world,2);creeper=Mob(world,MobType.Creeper);world.Step(creeper);world.blocked=true;world.Step(creeper);Assert(creeper.fuseTicks==0,"creeper defuses after losing sight");
  world=new World();player=Target(world,6);skeleton=Mob(world,MobType.Skeleton);world.Step(skeleton);
  Assert(world.ArrowCount==1&&skeleton.attackTicks==30,"skeleton fires a physical arrow with thirty-tick cooldown");
  for(int i=0;i<30;i++)world.Arrows();Assert(player.health==16,"projectile sweep damages player on intersection");
  Assert(world.ArrowCount==0&&world.VisualCount==0,"impacted arrows release simulation and visual state");
  world=new World{blocked=true};Target(world,6);world.Step(Mob(world,MobType.Skeleton));Assert(world.ArrowCount==0,"skeleton does not shoot through cover");
  var invulnerable=new Player();invulnerable.health=20;Assert(invulnerable.ApplyMobDamage(2,Vector3.up)&&!invulnerable.ApplyMobDamage(2,Vector3.up)&&invulnerable.health==18,"player hurt window protects from simultaneous mob damage");
#if FULL_MOB_SYSTEM
  world=new World();player=Target(world,6);player.inventory.held=new InventoryStack(ItemType.Wheat,1);var cow=Mob(world,MobType.Cow);world.Step(cow);Assert(cow.walking&&cow.targetYaw>0,"farm brain follows a player holding wheat");
  world=new World();player=Target(world,6);player.inventory.held=new InventoryStack(ItemType.Wheat,1);var chicken=Mob(world,MobType.Chicken);world.Step(chicken);Assert(!chicken.walking,"chicken does not follow wheat");player.inventory.held=new InventoryStack(ItemType.Seeds,1);world.Step(chicken);Assert(chicken.walking,"chicken follows seeds");
  world=new World();var a=Mob(world,MobType.Cow);var b=world.mobs.Spawn(new Vector3(1,64,0),0,MobType.Cow);MobRules.Feed(a);MobRules.Feed(b);
  for(int i=0;i<59;i++)world.Step(a);Assert(world.mobs.Count==2,"breeding waits for courtship");world.Step(a);Assert(world.mobs.Count==3&&a.breedCooldown==6000&&b.breedCooldown==6000,"courtship creates one baby and cools both parents");
  MobState baby=null;foreach(var entity in world.mobs.Entities)if(entity.ageTicks>0)baby=entity;Assert(baby!=null&&baby.ageTicks==24000,"baby starts with twenty-minute growth");
  var saved=world.CaptureMobs();Assert(saved.entities.Length==3&&saved.animals==null,"new saves use generic entity arrays");var loaded=new World();loaded.RestoreForCheck(saved);Assert(loaded.mobs.Get(baby.id).ageTicks==24000&&loaded.mobs.Get(a.id).breedCooldown==6000,"breeding and identity survive save restore");
  var legacy=new MobSaveData{animals=new[]{new MobState{id=71,health=10,position=new Vector3(0,64,0)}},nextId=99};loaded.RestoreForCheck(legacy);Assert(loaded.mobs.Get(71).species==MobType.Cow&&loaded.mobs.Spawn(new Vector3(3,64,0),0).id==99,"legacy cow-only payload preserves species and next identity");
  var cold=(World)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(World));cold.DropAuthority=true;
  cold.RestoreForCheck(legacy);Assert(cold.mobs.Get(71)!=null&&cold.CaptureMobs().nextId==99,"save restoration before lifecycle initialization recreates all missing mob runtime stores");
  cold.ClearForCheck();cold.ClearForCheck();Assert(cold.mobs.Count==0&&cold.MobEntities.RuntimeCount==0,"repeated mob reset is safe with cold runtime state");
  world=new World();cow=Mob(world,MobType.Cow);cow.motion.y=.42f;float peak=64;for(int i=0;i<20;i++){world.PhysicsStep(cow);peak=Math.Max(peak,cow.position.y);}Assert(peak>65.1f,"jump ordering reaches above one full block");
  world=new World();chicken=world.mobs.Spawn(new Vector3(0,72,0),0,MobType.Chicken);for(int i=0;i<200;i++)world.PhysicsStep(chicken);Assert(chicken.health==4&&chicken.position.y>=64,"chicken slow falling lands without fall damage");
  world=new World();var tall=Mob(world,MobType.Zombie);var small=world.mobs.Spawn(tall.position,0,MobType.Chicken);world.cells[new Vector3Int(0,65,0)]=new Block{type=BlockType.Stone};Assert(!world.MobBodyClear(tall,tall.position)&&world.MobBodyClear(small,small.position),"shared collision clearance uses each species height");
  world=new World();foreach(MobType type in Enum.GetValues(typeof(MobType)))world.mobs.Spawn(new Vector3((int)type*3,64,5),0,type);int meshStart=Mesh.all.Count;world.RenderForCheck();Assert(world.RendererCount==8,"all eight species construct their actual render hierarchy");world.ClearForCheck();Assert(world.RendererCount==0&&world.mobs.Count==0,"world teardown removes entities and renderers");for(int i=meshStart;i<Mesh.all.Count;i++)Assert(Mesh.all[i].destroyed,"world releases every owned model mesh");
  world=new World();for(int i=0;i<40;i++)world.mobs.Spawn(new Vector3(i,64,0),0,(MobType)(i%8));var connection=new Mirror.NetworkConnectionToClient{identity=new GameObject()};world.SendMobSnapshot(connection);Assert(connection.messages.Count==2&&((MobBatchMessage)connection.messages[0]).reset&&!((MobBatchMessage)connection.messages[1]).reset,"generic mob snapshots retain reset and batching semantics");
  var client=new World{DropAuthority=false};client.RegisterForCheck();foreach(var message in connection.messages)Mirror.NetworkClient.Deliver((MobBatchMessage)message);Assert(client.mobs.Count==40,"client restores all species through generic messages");
#endif
  Console.WriteLine(checks+" production entity/brain/navigation/projectile checks passed (Unity/Mirror/world queries mocked).");
 }
}

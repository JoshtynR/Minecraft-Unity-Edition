using System;
using UnityEngine;
class Check {
 static int checks;static void Assert(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 static CowState Mob(World w,PassiveAnimal type)=>w.cows.Spawn(new Vector3(0,64,0),0,type);
 static Player Target(World w,float distance){var player=new Player();player.transform.position=new Vector3(distance,64,0);w.Add(player);return player;}
 static void Main(){
  var world=new World();var player=Target(world,1);var zombie=Mob(world,PassiveAnimal.Zombie);
  world.Step(zombie);Assert(player.health==15&&zombie.attackTicks==20,"zombie deals reference melee damage and starts attack cooldown");
  world.Step(zombie);Assert(player.health==15,"attack cooldown prevents per-frame damage");
  world.betaSkylightSubtracted=0;world.Step(zombie);Assert(zombie.fireTicks==160,"exposed zombies ignite in daylight");
  var skeleton=Mob(world,PassiveAnimal.Skeleton);world.Step(skeleton);Assert(skeleton.fireTicks==160,"exposed skeletons ignite in daylight");
  world=new World{blocked=true};player=Target(world,1);world.Step(Mob(world,PassiveAnimal.Zombie));Assert(player.health==20,"solid cover blocks melee acquisition");
  world=new World{betaSkylightSubtracted=0};player=Target(world,1);var spider=Mob(world,PassiveAnimal.Spider);
  world.Step(spider);Assert(player.health==20,"unprovoked spiders are neutral in bright daylight");
  spider.angerTicks=600;world.Step(spider);Assert(player.health==18,"provoked daylight spider attacks");
  world=new World();player=Target(world,1);player.isFlying=true;world.Step(Mob(world,PassiveAnimal.Zombie));Assert(player.health==20,"flying players are not hostile targets");
  world=new World();player=Target(world,2);var creeper=Mob(world,PassiveAnimal.Creeper);
  for(int i=0;i<29;i++)Assert(world.Step(creeper),"creeper does not explode before thirty ticks");
  Assert(creeper.fuseTicks==29&&world.Step(creeper)==false&&world.cows.Get(creeper.id)==null,"thirtieth fuse tick explodes and removes creeper once");
  Assert(player.health<20,"creeper explosion damages exposed player");
  world=new World();player=Target(world,2);creeper=Mob(world,PassiveAnimal.Creeper);world.Step(creeper);world.blocked=true;world.Step(creeper);Assert(creeper.fuseTicks==0,"creeper defuses after losing sight");
  world=new World();player=Target(world,6);skeleton=Mob(world,PassiveAnimal.Skeleton);world.Step(skeleton);
  Assert(world.ArrowCount==1&&skeleton.attackTicks==30,"skeleton fires a physical arrow with thirty-tick cooldown");
  for(int i=0;i<30;i++)world.Arrows();Assert(player.health==16,"projectile sweep damages player on intersection");
  Assert(world.ArrowCount==0&&world.VisualCount==0,"impacted arrows release simulation and visual state");
  world=new World{blocked=true};Target(world,6);world.Step(Mob(world,PassiveAnimal.Skeleton));Assert(world.ArrowCount==0,"skeleton does not shoot through cover");
  var invulnerable=new Player();invulnerable.health=20;Assert(invulnerable.ApplyMobDamage(2,Vector3.up)&&!invulnerable.ApplyMobDamage(2,Vector3.up)&&invulnerable.health==18,"player hurt window protects from simultaneous mob damage");
  Console.WriteLine(checks+" production hostile AI/projectile/damage checks passed (Unity/Mirror/world queries mocked).");
 }
}

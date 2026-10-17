using UnityEngine;

public static partial class ShapeCheck
{
 class JumpController:Controller
 {
  public int jumps;
  public void Press(bool down){RequestJump(down);}
  protected override void OnJumpPerformed(){jumps++;}
 }
 static JumpController Jumper(bool ceiling=false)
 {
  var world=new World();
  for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
  {
   Put(world,x,0,z,BlockType.Stone);
   if(ceiling)Put(world,x,3,z,BlockType.Stone);
  }
  return new JumpController{world=world,isGrounded=true,transform=new TestTransform{position=V(.5f,1,.5f)}};
 }
 static void CheckJumpInput()
 {
  foreach(var ceiling in new[]{false,true})foreach(var renderFrames in new[]{1,2,5})
  {
   var player=Jumper(ceiling);player.Press(true);player.Press(false);
   player.FixedUpdate();
   Check(player.jumps==1&&player.transform.position.y>1,"tap takes off during its first physics step even if released before that step");
   for(int tick=0;tick<160;tick++)
   {
    for(int frame=0;frame<renderFrames;frame++)player.Press(false);
    if(tick==0)player.jumpRequest=true; // Reproduce an old request left queued in air.
    player.FixedUpdate();
   }
   Check(player.jumps==1&&player.isGrounded,"one press never repeats in air or on landing, including low ceilings: "+ceiling+" jumps="+player.jumps+" y="+player.transform.position.y+" grounded="+player.isGrounded);
   Check(!player.jumpRequest,"physics consumes pending input rather than retaining it across landings");
   player.Press(true);player.FixedUpdate();Check(player.jumps==2,"a new press after landing still jumps");
   player.Press(true);player.FixedUpdate();Check(player.jumps==2,"airborne input cannot produce a second jump");
  }
  var flying=Jumper();flying.isFlying=true;flying.Press(true);flying.FixedUpdate();
  Check(flying.jumps==0,"flight ascent cannot queue a ground jump");
 }
}

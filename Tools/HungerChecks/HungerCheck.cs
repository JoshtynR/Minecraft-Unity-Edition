using System;
public static class HungerCheck
{
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static void Ticks(HungerSimulation s,int count,SurvivalDifficulty difficulty=SurvivalDifficulty.Normal,bool regen=false){for(int i=0;i<count;i++)s.Tick(difficulty,regen);}
 public static void Main()
 {
  var s=new HungerSimulation();Check(s.CanSprint&&s.Saturation==5&&s.Food==20,"starting food/saturation");
  s.Exhaustion=4;Ticks(s,1);Check(s.Saturation==5,"exactly four is below strict exhaustion threshold");
  s.AddExhaustion(0.001f);Ticks(s,1);Check(s.Saturation==4&&s.Food==20,"saturation spent before food");
  s.Saturation=0;s.Exhaustion=4.01f;Ticks(s,1);Check(s.Food==19,"one food point per exhausted threshold");
  s.Exhaustion=40;Ticks(s,1);Check(s.Food==18&&s.Exhaustion==36,"only one depletion per tick");
  s.AddExhaustion(100);Check(s.Exhaustion==40,"exhaustion capped");
  foreach(float distance in new[]{0f,0.004f,0.005f,0.25f,1f,10f,40f})
  {s=new HungerSimulation();s.AddGroundMovement(distance,true,false);int cm=(int)Math.Floor(distance*100+0.5);Check(Math.Abs(s.Exhaustion-0.099999994f*cm*0.01f)<0.00001f,"sprint centimeter cost");}
  s=new HungerSimulation();s.AddGroundMovement(10,false,false);Check(Math.Abs(s.Exhaustion-0.1f)<0.00001f,"walking cost");
  s.AddGroundMovement(10,true,true);Check(Math.Abs(s.Exhaustion-0.1f)<0.00001f,"flight exemption");
  s=new HungerSimulation();s.AddJump(true,false);Check(s.Exhaustion==0.8f,"sprint jump cost");s.AddJump(false,false);Check(s.Exhaustion==1,"normal jump cost");
  s=new HungerSimulation{Food=6};Check(!s.CanSprint,"sprint blocked at three drumsticks");s.Food=7;Check(s.CanSprint,"sprint allowed above three drumsticks");
  foreach(var difficulty in new[]{SurvivalDifficulty.Easy,SurvivalDifficulty.Normal,SurvivalDifficulty.Hard})
  {
   s=new HungerSimulation{Food=0,Saturation=0};Ticks(s,79,difficulty);Check(s.Health==20,"starvation waits full 80 ticks");Ticks(s,1,difficulty);Check(s.Health==19,"half heart starvation damage");Ticks(s,4000,difficulty);
   Check(s.Health==(difficulty==SurvivalDifficulty.Easy?10:difficulty==SurvivalDifficulty.Normal?1:0),"difficulty starvation floor");
  }
  s=new HungerSimulation{Health=18,Food=18};Ticks(s,79,SurvivalDifficulty.Normal,true);Check(s.Health==18,"regen interval");Ticks(s,1,SurvivalDifficulty.Normal,true);Check(s.Health==19&&s.Exhaustion==3,"regen half heart and exhaustion cost");
  s=new HungerSimulation{Health=18,Food=17};Ticks(s,160,SurvivalDifficulty.Normal,true);Check(s.Health==18,"regen requires eighteen food");
  s=new HungerSimulation{Food=0};Ticks(s,79);s.Food=1;Ticks(s,1);s.Food=0;Ticks(s,79);Check(s.Health==20,"food restores/reset starvation timer");
  s=new HungerSimulation{Health=0};s.AddJump(true,false);s.AddGroundMovement(1,true,false);Ticks(s,100,SurvivalDifficulty.Hard,true);Check(s.Health==0&&s.Exhaustion==0,"dead state neither heals nor drains");
  Console.WriteLine(checks+" classic hunger threshold/movement/jump/regen/starvation checks passed.");
 }
}

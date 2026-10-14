using System;
using UnityEngine;
public static partial class ShapeCheck
{
 static void CheckStairs()
 {
  foreach(var type in new[]{BlockType.WoodenStairs,BlockType.CobblestoneStairs})
  for(byte facing=0;facing<4;facing++)
  {
   var world=new World();var block=new Block(type){blockShape=StairGeometry.Shape(facing)};world.blocks[new Vector3Int()]=block;
   var low=StairGeometry.CollisionPart(facing,0);var high=StairGeometry.CollisionPart(facing,1);
   var lowFeet=V((low.xMin+low.xMax)/32f,3,(low.zMin+low.zMax)/32f);
   var highFeet=V((high.xMin+high.xMax)/32f,3,(high.zMin+high.zMax)/32f);
   Check(Near(BlockCollision.Clip(world,lowFeet,.1f,1.8f,-3,1),-2.5f),"stair low half landing at .5");
   Check(Near(BlockCollision.Clip(world,highFeet,.1f,1.8f,-3,1),-2),"stair high half landing at 1");
   float volume=(low.xMax-low.xMin)*(low.yMax-low.yMin)*(low.zMax-low.zMin)+(high.xMax-high.xMin)*(high.yMax-high.yMin)*(high.zMax-high.zMin);
   Check(Near(volume,3072),"stair has Beta three-quarter-block solid volume");
   Vector3 feet,move;
   if(facing==0){feet=V(-.4f,0,.5f);move=V(.55f,0,0);}
   else if(facing==1){feet=V(1.4f,0,.5f);move=V(-.55f,0,0);}
   else if(facing==2){feet=V(.5f,0,-.4f);move=V(0,0,.55f);}
   else {feet=V(.5f,0,1.4f);move=V(0,0,-.55f);}
   var stepped=BlockCollision.Resolve(world,feet,.1f,1.8f,move,true);
   Check(Near(stepped.y,.5f)&&Near(stepped.x,move.x)&&Near(stepped.z,move.z),"walk onto first stair tread without jumping");
   var next=feet+stepped;next.y=.5f;
   var second=BlockCollision.Resolve(world,next,.1f,1.8f,move,true);
   Check(Near(second.y,.5f),"walk onto second stair tread without jumping");
   Check(StairGeometry.Facing(StairGeometry.Shape(facing))==facing,"orientation round trip");
   var direction=facing==0?V(1,0,0):facing==1?V(-1,0,0):facing==2?V(0,0,1):V(0,0,-1);
   Check(StairGeometry.FacingForLook(direction)==facing,"placement faces forward in each cardinal direction");
  }
  foreach(var slab in new[]{BlockType.StoneSlab,BlockType.SandstoneSlab,BlockType.WoodenSlab,BlockType.CobblestoneSlab})
  {
   BlockType doubled;Check(StairGeometry.TryDoubleSlab(slab,slab,out doubled)&&BlockShapesExtensions.ForBlock(doubled)==BlockShapes.FullBlock,"matching slabs combine into full block");
   Check(!StairGeometry.TryDoubleSlab(slab,BlockType.Dirt,out doubled),"slabs do not combine with other materials");
   var world=new World();Put(world,0,0,0,slab);
   Check(Near(BlockCollision.Clip(world,V(.5f,2,.5f),.3f,1.8f,-2,1),-1.5f),"all four slab materials have half-height collision");
  }
 }
}

using System;
using UnityEngine;
public static partial class ShapeCheck
{
 static void CheckSpecial()
 {
  int start=checks;
  foreach(BlockType t in Enum.GetValues(typeof(BlockType))) if(SpecialBlockGeometry.Handles(t))
  for(byte state=0;state<16;state++)
  {
   var shape=SpecialBlockGeometry.Shape(t,state);
   Check(SpecialBlockGeometry.State(t,shape)==state,"all sixteen native states round trip for "+t);
   Check(ReferenceEquals(shape.GetShape(),shape.GetShape()),"selection bounds are cached, not allocated per collision query");
   var m=new MeshData(false);
   SpecialBlockMesh.Append(t,state,new Vector3Int(),null,d=>3,new Color(1,1,1,1),m);
   int n=m.vertices.Count;
   Check(n%4==0&&m.triangles.Count==n/4*6&&m.uv.Count==n&&m.colors.Count==n&&m.faceCoordinates.Count==n,"complete mesh channels for "+t);
   foreach(var v in m.vertices)Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z),"finite special vertices");
   foreach(int i in m.triangles)Check(i>=0&&i<n,"special model triangle indices in range");
  }
  for(int mask=0;mask<16;mask++)
  {
   int neighbors=mask;
   Func<Vector3Int,BlockType> get=p=>p.x==-1&&(neighbors&1)!=0||p.x==1&&(neighbors&2)!=0||p.z==-1&&(neighbors&4)!=0||p.z==1&&(neighbors&8)!=0?BlockType.Fence:BlockType.Air;
   var m=new MeshData(false);SpecialBlockMesh.Append(BlockType.Fence,0,new Vector3Int(),get,d=>0,new Color(),m);
   bool x=(mask&3)!=0||(mask&12)==0,z=(mask&12)!=0;
   Check(m.vertices.Count==24*(1+(x?2:0)+(z?2:0)),"Beta fence axis rail count for neighbor mask "+mask);
   m=new MeshData(false);SpecialBlockMesh.Append(BlockType.Fence,0,new Vector3Int(),p=>BlockType.Stone,d=>0,new Color(),m);
   Check(m.vertices.Count==72,"fences do not connect to solid blocks in Beta");
  }
  var world=new World();Put(world,0,0,0,BlockType.Fence);
  Check(Near(BlockCollision.Clip(world,V(-1,1.2f,.5f),.3f,1.8f,2,0),.7f),"fence collider reaches above own voxel and spans full width");
  Check(Near(BlockCollision.Clip(world,V(-1,1.6f,.5f),.3f,1.8f,2,0),2),"fence collision stops at 1.5 blocks");
  foreach(var t in new[]{BlockType.Torch,BlockType.RedstoneTorch,BlockType.RedstoneWire,BlockType.Rail,BlockType.Lever,BlockType.Cobweb})
  {
   world=new World();Put(world,0,0,0,t);Check(Near(BlockCollision.Clip(world,V(-1,0,.5f),.3f,1.8f,2,0),2),"canonical non-solid collision for "+t);
  }
  var normals=new[]{new Vector3Int(1,0,0),new Vector3Int(-1,0,0),new Vector3Int(0,0,1),new Vector3Int(0,0,-1),new Vector3Int(0,1,0)};
  for(byte s=0;s<6;s++)
  {
   var head=SpecialBlockGeometry.Shape(BlockType.PistonHead,s);
   Check(SpecialBlockGeometry.CollisionCount(BlockType.PistonHead)==2,"piston heads have cap and rod collision");
   Check(ReferenceEquals(head.GetShape(),SpecialBlockGeometry.CollisionPart(BlockType.PistonHead,head,0)),"piston head selection is cap only");
   var extended=SpecialBlockGeometry.Shape(BlockType.Piston,(byte)(s|8)).GetShape();
   float volume=(extended.xMax-extended.xMin)*(extended.yMax-extended.yMin)*(extended.zMax-extended.zMin);
   Check(Near(volume,3072),"extended piston base is three-quarter cube in every direction");
  }
  byte next;
  Check(SpecialBlockGeometry.TryUse(BlockType.WoodenDoor,8,out next)&&next==12,"upper door interaction retains half and toggles open");
  Check(!SpecialBlockGeometry.TryUse(BlockType.IronDoor,0,out next),"iron doors are not hand-toggleable");
  var doorOrigin=new Vector3Int(8,8,8);
  Func<Vector3Int,BlockType> emptyDoorWorld=p=>BlockType.Air;
  Check(SpecialBlockGeometry.DoorPlacement(BlockType.WoodenDoor,1,doorOrigin,emptyDoorWorld)==1,"single door preserves the Beta yaw orientation");
  Func<Vector3Int,BlockType> leftDoorWorld=p=>p.Equals(doorOrigin+Vector3Int.right)||p.Equals(doorOrigin+Vector3Int.right+Vector3Int.up)?BlockType.WoodenDoor:BlockType.Air;
  Check(SpecialBlockGeometry.DoorPlacement(BlockType.WoodenDoor,1,doorOrigin,leftDoorWorld)==4,"adjacent door reverses the hinge for a centred double door");
  Func<Vector3Int,BlockType> solidRightWorld=p=>p.Equals(doorOrigin+Vector3Int.left)||p.Equals(doorOrigin+Vector3Int.left+Vector3Int.up)?BlockType.Stone:BlockType.Air;
  Check(SpecialBlockGeometry.DoorPlacement(BlockType.WoodenDoor,1,doorOrigin,solidRightWorld)==4,"solid-side count selects the unobstructed door hinge");
  var closedTrap=SpecialBlockGeometry.Selection(BlockType.Trapdoor,2);var openTrap=SpecialBlockGeometry.Selection(BlockType.Trapdoor,6);
  Check(closedTrap.yMax==3&&openTrap.xMin==13&&openTrap.yMax==16,"trapdoor switches from a three-pixel floor plate to its attached wall");
  Check(SpecialBlockGeometry.TryUse(BlockType.Trapdoor,2,out next)&&next==6,"trapdoor interaction toggles its open metadata bit");
  var doorNormal=new MeshData(false);var doorMirrored=new MeshData(false);
  SpecialBlockMesh.Append(BlockType.WoodenDoor,1,new Vector3Int(),null,d=>0,new Color(),doorNormal);
  SpecialBlockMesh.Append(BlockType.WoodenDoor,4,new Vector3Int(),null,d=>0,new Color(),doorMirrored);
  Check(doorNormal.uv[0].x!=doorMirrored.uv[0].x,"opposite door hinges mirror the broad-face handle texture");
  Check(SpecialBlockGeometry.Selection(BlockType.Trapdoor,10).yMin==13,"upper trapdoor closes flush with the cell ceiling");
  Check(SpecialBlockGeometry.TryUse(BlockType.Trapdoor,10,out next)&&next==14&&SpecialBlockGeometry.Selection(BlockType.Trapdoor,next).yMin==0,"upper trapdoor opens downward and preserves its mounting bit");
  for(byte orientation=0;orientation<4;orientation++)
  {
   byte initial=orientation;
   var offset=orientation==0?Vector3Int.forward:orientation==1?Vector3Int.left:orientation==2?Vector3Int.back:Vector3Int.right;
   foreach(int sign in new[]{-1,1})
   {
    var neighbor=doorOrigin+new Vector3Int(offset.x*sign,0,offset.z*sign);
    Func<Vector3Int,BlockType> adjacent=p=>p.Equals(neighbor)?BlockType.WoodenDoor:BlockType.Air;
    byte paired=SpecialBlockGeometry.DoorPlacement(BlockType.WoodenDoor,initial,doorOrigin,adjacent,p=>initial);
    Check(paired==(sign<0?(byte)(((initial-1)&3)|4):initial),"double door placement chooses the outside hinge for its side");
   }
  }
  Check(GrassSpreadRules.ShouldDecay(3,15)&&!GrassSpreadRules.ShouldDecay(4,15)&&!GrassSpreadRules.ShouldDecay(3,1),"grass decay requires both darkness and an opaque cover");
  Check(GrassSpreadRules.CanSpreadFrom(9)&&!GrassSpreadRules.CanSpreadFrom(8),"grass spread uses the Beta source-light threshold");
  Check(GrassSpreadRules.CanBecomeGrass(BlockType.Dirt,4,1)&&GrassSpreadRules.CanBecomeGrass(BlockType.Dirt,15,0),"dirt under leaves or air can become grass");
  Check(!GrassSpreadRules.CanBecomeGrass(BlockType.Dirt,15,3)&&!GrassSpreadRules.CanBecomeGrass(BlockType.Stone,15,0),"opaque cover and non-dirt candidates reject grass spread");
  Check(SpecialBlockGeometry.TryUse(BlockType.RedstoneRepeater,15,out next)&&next==3,"repeater delay cycles without rotating");
  Func<Vector3Int,BlockType> curve=p=>(p.x==1&&p.z==0&&p.y==0)||(p.x==0&&p.z==1&&p.y==0)?BlockType.Rail:BlockType.Air;
  Check(SpecialBlockGeometry.RailState(BlockType.Rail,0,new Vector3Int(),curve)==6,"rail turns south-east corner");
  Func<Vector3Int,BlockType> slope=p=>p.x==1&&p.z==0&&p.y==1?BlockType.Rail:BlockType.Air;
  Check(SpecialBlockGeometry.RailState(BlockType.Rail,1,new Vector3Int(),slope)==2,"rail rises to east neighbor");
  for(byte i=0;i<5;i++)Check(SpecialBlockGeometry.Placement(BlockType.Torch,V(0,0,1),normals[i])==i+1,"native torch attachment direction");
  for(byte q=0;q<4;q++)
  {
   var look=q==0?V(0,0,1):q==1?V(-1,0,0):q==2?V(0,0,-1):V(1,0,0);
   Check(SpecialBlockGeometry.Placement(BlockType.RedstoneWire,look,new Vector3Int())==0,"placement never confuses direction with redstone power");
   byte s=SpecialBlockGeometry.Placement(BlockType.Furnace,look,new Vector3Int());
   Direction front=s==2?Direction.backwards:s==3?Direction.forwards:s==4?Direction.left:Direction.right;
   Check(SpecialBlockGeometry.TextureDirection(BlockType.Furnace,s,front)==Direction.backwards,"furnace front rotates independently of side art");
   var foot=SpecialBlockGeometry.PartnerOffset(BlockType.Bed,q);var head=SpecialBlockGeometry.PartnerOffset(BlockType.Bed,(byte)(q|8));
   Check(foot.x+head.x==0&&foot.z+head.z==0,"bed halves use inverse offsets");
  }
  for(int mask=0;mask<16;mask++)
  {
   int n=mask;Func<Vector3Int,BlockType> get=p=>p.y==0&&((p.x==-1&&(n&1)!=0)||(p.x==1&&(n&2)!=0)||(p.z==-1&&(n&4)!=0)||(p.z==1&&(n&8)!=0))?BlockType.RedstoneWire:BlockType.Air;
   var m=new MeshData(false);SpecialBlockMesh.Append(BlockType.RedstoneWire,0,new Vector3Int(),get,d=>d==Direction.up?1:2,new Color(),m);
   Check(m.vertices.Count==8&&Near(m.vertices[0].y,1/64f),"dust lies one sixty-fourth above support");
   bool straight=((mask&3)!=0&&(mask&12)==0)||((mask&12)!=0&&(mask&3)==0);
   Check(m.uv[0].z==(straight?2:1),"dust uses cross versus straight image for mask "+mask);
   Check(Near(m.colors[0].r,.3f)&&m.colors[0].g==0&&m.colors[0].b==0,"unpowered dust Beta tint");
  }
  var climb=new MeshData(false);SpecialBlockMesh.Append(BlockType.RedstoneWire,15,new Vector3Int(),p=>p.x==1&&p.y==0?BlockType.Stone:p.x==1&&p.y==1?BlockType.RedstoneWire:BlockType.Air,d=>0,new Color(),climb);
  Check(climb.vertices.Count==16&&Near(climb.colors[0].r,1),"dust climbs solid side to upper wire and powered tint is retained");
  for(byte state=0;state<2;state++)
  {
   var portal=new MeshData(false);SpecialBlockMesh.Append(BlockType.NetherPortal,state,new Vector3Int(),null,d=>3,new Color(1,1,1,1),portal);
   Check(portal.vertices.Count==8&&portal.triangles.Count==12,"portal emits only its front and back faces");
   foreach(var v in portal.vertices)
    Check(state==0?Math.Abs(v.z-.375f)<.001f||Math.Abs(v.z-.625f)<.001f:Math.Abs(v.x-.375f)<.001f||Math.Abs(v.x-.625f)<.001f,"portal vertices stay on the two oriented broad planes");
  }
  Console.WriteLine((checks-start)+" special model/state/connection/attachment checks passed.");
 }
}

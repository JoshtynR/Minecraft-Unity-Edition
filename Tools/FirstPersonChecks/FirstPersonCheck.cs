using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
class FirstPersonCheck {
 static int assertions;
 static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
 static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 static void Step(FirstPersonView view,int frames=1){for(int i=0;i<frames;i++)Call(view,"LateUpdate");}
 static void MeshCheck(Mesh mesh,int vertices) {
  Check(mesh.vertexCount==vertices,"Vertex count");Check(mesh.uv.Count==vertices,"UV count");
  Check(mesh.triangles.Count==vertices/4*6,"Triangle count");
  foreach(int i in mesh.triangles)Check(i>=0&&i<vertices,"Valid triangle index");
  foreach(var uv in mesh.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"UV in texture");
  for(int q=0;q<vertices;q+=4){var a=mesh.vertices[q+1]-mesh.vertices[q];var b=mesh.vertices[q+2]-mesh.vertices[q];float x=a.y*b.z-a.z*b.y,y=a.z*b.x-a.x*b.z,z=a.x*b.y-a.y*b.x;Check(x*x+y*y+z*z>0,"Nondegenerate quad");}
 }
 static void Main(){
  MeshCheck(FirstPersonMesh.Sprite(16,16),264);MeshCheck(FirstPersonMesh.Sprite(32,16),392);
  MeshCheck(FirstPersonMesh.Sprite(0,0),24);MeshCheck(FirstPersonMesh.Sprite(10000,10000),4104);
  var arm=FirstPersonMesh.Arm(64,32);MeshCheck(arm,24);MeshCheck(FirstPersonMesh.Arm(64,64),24);
  Check(Math.Abs(arm.uv[0].x-44f/64)<.0001&&Math.Abs(arm.uv[0].y)<.0001,"Classic right arm UV");
  var spriteTemplate=new Material();var blockTemplate=new Material();
  Resources.assets["FirstPersonMaterial"]=spriteTemplate;Resources.assets["DroppedBlockMaterial"]=blockTemplate;
  Resources.assets["PlayerHudAssets"]=new PlayerHudAssets{inventoryPlayerSkin=new Texture2D()};
  var ui=new GameObject("UI camera").AddComponent<Camera>();ui.depth=0;
  new GameObject("PlayerCanvas").AddComponent<Canvas>().worldCamera=ui;
  var player=new GameObject("Player").AddComponent<Player>();player.cam=new GameObject("Player camera").transform;
  var source=player.cam.gameObject.AddComponent<Camera>();source.cullingMask=255;
  var view=player.gameObject.AddComponent<FirstPersonView>();view.Initialize(player);Step(view);
  var hand=(Camera)Field(view,"handCamera");var filter=(MeshFilter)Field(view,"filter");
  Check(hand.enabled,"Ready player sees hand");Check(source.depth<hand.depth&&hand.depth<ui.depth,"HUD draws after hand");
  Check(hand.cullingMask==128&&(source.cullingMask&128)==0,"Camera layer isolation");
  Check(hand.clearFlags==CameraClearFlags.Depth&&hand.nearClipPlane==.01f,"Hand cannot clip into world depth");
  Check(filter.sharedMesh.name=="First person right arm","Empty hand skin mesh");
  var ownedArm=filter.sharedMesh;
  player.inventory.stack.type=ItemType.Stone;Step(view,30);
  Check((ItemType)Field(view,"shown")==ItemType.Stone&&(bool)Field(view,"isBlock"),"Block stays 3D despite assigned icon");
  var stoneMesh=filter.sharedMesh;Step(view,100);Check(filter.sharedMesh==stoneMesh,"No mesh rebuild per frame");
  player.inventory.stack.type=ItemType.Pickaxe;Step(view,30);
  Check(!(bool)Field(view,"isBlock")&&filter.sharedMesh.vertexCount==264,"Extruded sprite tool");
  var toolMesh=filter.sharedMesh;
  player.inventory.stack.type=ItemType.Stone;Step(view,30);Check(filter.sharedMesh==stoneMesh,"Hotbar reuses cached mesh");
  Input.buttons[0]=true;Step(view);Check((float)Field(view,"swing")<1,"Mining starts swing");Input.buttons[0]=false;
  player.inventory.isOpen=true;Step(view);Check(!hand.enabled,"Inventory hides hand");player.inventory.isOpen=false;
  player.escapeMenuOpen=true;Step(view);Check(!hand.enabled,"Escape hides hand");player.escapeMenuOpen=false;
  WorldLoadingScreen.IsLoading=true;Step(view);Check(!hand.enabled,"Loading hides hand");WorldLoadingScreen.IsLoading=false;
  player.health=0;Step(view);Check(!hand.enabled,"Death hides hand");player.health=20;
  player.IsReadyForPlay=false;Step(view);Check(!hand.enabled,"Saved-player initialization hides hand");player.IsReadyForPlay=true;
  Step(view);Check(hand.enabled,"Hand returns to play");
  player.inventory.stack.type=ItemType.Missing;Step(view,30);Check(filter.sharedMesh==ownedArm,"Missing definition falls back to arm");
  World.Instance=new World();Step(view);
  var ownedSprite=(Material)Field(view,"spriteMaterial");var ownedBlock=(Material)Field(view,"blockMaterial");
  Check(ownedSprite.values["_Brightness"]==.5f,"Arm follows world lighting");
  Call(view,"OnDisable");Check(!hand.enabled,"Disabling component disables camera");
  Call(view,"OnDestroy");
  Check(ownedArm.destroyed&&stoneMesh.destroyed&&toolMesh.destroyed,"Owned meshes released");
  Check(ownedSprite.destroyed&&ownedBlock.destroyed&&!spriteTemplate.destroyed&&!blockTemplate.destroyed,"Only cloned materials released");
  Check(source.cullingMask==255&&ui.depth==0,"Camera settings restored");
  Check(((GameObject)Field(view,"cameraObject")).destroyed,"Owned hierarchy released");
  Console.WriteLine("First-person checks passed: "+assertions);
 }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
class FirstPersonCheck {
 static int assertions;
 static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
 static int VisibleArmVertices(Mesh mesh,Transform pivot,Vector3 inheritedScale,float aspect)
 {
  int visible=0;
  foreach(var vertex in mesh.vertices)
  {
   var point=pivot.localRotation*new Vector3(vertex.x*pivot.localScale.x,vertex.y*pivot.localScale.y,vertex.z*pivot.localScale.z);
   double x=(point.x+pivot.localPosition.x)*inheritedScale.x,y=(point.y+pivot.localPosition.y)*inheritedScale.y,z=(point.z+pivot.localPosition.z)*inheritedScale.z;
   double halfHeight=z*Math.Tan(35*Math.PI/180);
   if(z>.01&&z<4&&Math.Abs(y)<halfHeight&&Math.Abs(x)<halfHeight*aspect)visible++;
  }
  return visible;
 }
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
  var body=new GameObject("Scaled body").transform;body.localScale=new Vector3(.058f,.058f,.058f);
  player.cam.SetParent(body,false);player.cam.localScale=new Vector3(18.018015f,18.018015f,.99369353f);
  player.cam.position=new Vector3(15,72,-31);player.cam.rotation=Quaternion.Euler(new Vector3(12,85,0));
  var source=player.cam.gameObject.AddComponent<Camera>();source.cullingMask=255;
  var view=player.gameObject.AddComponent<FirstPersonView>();view.Initialize(player);Step(view);
  var hand=(Camera)Field(view,"handCamera");var filter=(MeshFilter)Field(view,"filter");
  Check(hand.enabled,"Ready player sees hand");Check(source.depth<hand.depth&&hand.depth<ui.depth,"HUD draws after hand");
  Check(hand.cullingMask==128&&(source.cullingMask&128)==0,"Camera layer isolation");
  Check((source.cullingMask&64)==0,"local third-person arm cannot overlay the first-person hand");
  Check(hand.clearFlags==CameraClearFlags.Depth&&hand.nearClipPlane==.01f,"Hand cannot clip into world depth");
  Check(filter.sharedMesh.name=="First person right arm","Empty hand skin mesh");
  var root=(GameObject)Field(view,"cameraObject");var pose=(Transform)Field(view,"pivot");
  Check(root.transform.parent==null&&root.transform.lossyScale.z==1,"hand root does not inherit compressed prefab scale");
  Check(root.transform.position.x==15&&root.transform.position.y==72&&root.transform.rotation.euler.y==85,"hand camera copies world camera pose");
  Check(VisibleArmVertices(filter.sharedMesh,pose,player.cam.lossyScale,16f/9)==0,"original scaled hierarchy moves the actual arm outside the view");
  Check(VisibleArmVertices(filter.sharedMesh,pose,root.transform.lossyScale,16f/9)>=4,"unscaled arm intersects widescreen frustum");
  Check(VisibleArmVertices(filter.sharedMesh,pose,root.transform.lossyScale,4f/3)>=4,"unscaled arm intersects standard aspect frustum");
  var wrist=pose.localRotation*new Vector3(0,-.75f,0);
  Check(wrist.z>0,"empty wrist extends into the scene, beyond the shoulder");
  double shoulderHalfHeight=pose.localPosition.z*Math.Tan(35*Math.PI/180);
  Check(wrist.y>0&&pose.localPosition.y < -shoulderHalfHeight,"shoulder exits below view while hand extends upward");
  Check(Math.Abs(pose.localPosition.x-.47867263)<.0001&&Math.Abs(pose.localPosition.y+.84348354)<.0001&&Math.Abs(pose.localPosition.z-.52812314)<.0001,"reference shoulder translation");
  var fist=pose.localPosition+wrist;
  Check(Math.Abs(fist.x-.75517044)<.0001&&Math.Abs(fist.y+.43795376)<.0001&&Math.Abs(fist.z-1.09521622)<.0001,"reference fist position and arm rotation");
  player.cam.position=new Vector3(-20,90,50);player.cam.rotation=Quaternion.Euler(new Vector3(-30,180,0));source.targetDisplay=1;
  Step(view);Check(root.transform.position.x==-20&&root.transform.rotation.euler.x==-30&&hand.targetDisplay==1,"camera follows movement/looking and display changes");
  player.cam.localScale=new Vector3(10,20,.01f);Step(view);Check(root.transform.lossyScale.x==1&&root.transform.lossyScale.z==1,"later source scale changes do not distort hand");
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

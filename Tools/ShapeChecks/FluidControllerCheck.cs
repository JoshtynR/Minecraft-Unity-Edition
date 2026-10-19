using UnityEngine;

public static partial class ShapeCheck
{
    private class FluidController:Controller
    {
        public int jumps;
        public void Input(bool held,float forward){swimJumpHeld=held;vertical=forward;}
        protected override void OnJumpPerformed(){jumps++;}
    }
    private static FluidController Swimmer(BlockType type)
    {
        var world=new World();
        for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++)for(int y=0;y<=9;y++)Put(world,x,y,z,y==0?BlockType.Stone:type);
        return new FluidController{world=world,transform=new TestTransform{position=V(.5f,4,.5f)}};
    }
    private static void CheckFluidController()
    {
        float step=Time.fixedDeltaTime;
        Time.fixedDeltaTime=Time.deltaTime=.05f;
        var water=Swimmer(BlockType.Water);water.Input(true,1);water.isGrounded=true;water.jumpRequest=true;
        for(int i=0;i<20;i++)water.FixedUpdate();
        Check(water.InWater&&!water.InLava&&water.jumps==0,"actual controller uses held-jump buoyancy rather than ground jumping underwater");
        Check(water.transform.position.y>4.1f&&water.transform.position.z>1,"held swim input rises and advances");
        var lava=Swimmer(BlockType.Lava);lava.Input(true,1);
        for(int i=0;i<20;i++)lava.FixedUpdate();
        Check(lava.InLava&&lava.transform.position.z<water.transform.position.z,"lava has stronger drag than water in actual controller");
        var sink=Swimmer(BlockType.Water);sink.Input(false,0);
        for(int i=0;i<20;i++)sink.FixedUpdate();
        Check(sink.transform.position.y<3.5f,"releasing jump sinks without land gravity");
        var reference=Swimmer(BlockType.Water);reference.Input(true,1);
        for(int i=0;i<20;i++)reference.FixedUpdate();
        Time.fixedDeltaTime=Time.deltaTime=.02f;
        var other=Swimmer(BlockType.Water);other.Input(true,1);
        for(int i=0;i<50;i++)other.FixedUpdate();
        Check((other.transform.position-reference.transform.position).sqrMagnitude<.000001f,"20Hz fluid integration is independent of Unity fixed step");
        Time.fixedDeltaTime=Time.deltaTime=.05f;
        var wall=Swimmer(BlockType.Water);wall.Input(false,1);
        for(int x=-1;x<=1;x++)for(int y=1;y<10;y++)Put(wall.world,x,y,1,BlockType.Stone);
        for(int i=0;i<30;i++)wall.FixedUpdate();
        Check(wall.transform.position.z+wall.entityWidth<=1.0001f,"swimming cannot tunnel through a solid wall");
        Check(FluidWorldQueries.AtEye(wall.world,V(-.5f,4.5f,-.5f))==BlockType.Water,"negative-coordinate eye lookup");
        Time.fixedDeltaTime=Time.deltaTime=.02f;
        var shore=new World();
        for(int x=-2;x<=2;x++)for(int z=-2;z<=8;z++)
        {Put(shore,x,0,z,BlockType.Stone);if(z>=2)for(int y=1;y<=2;y++)Put(shore,x,y,z,BlockType.Lava);}
        var entering=new FluidController{world=shore,isGrounded=true,transform=new TestTransform{position=V(.5f,1,.5f)}};
        entering.Input(false,1);for(int i=0;i<100;i++)entering.FixedUpdate();
        float enteredZ=entering.transform.position.z;
        Check(entering.InLava&&enteredZ>2.2f,"walking from shore into lava keeps accepting movement at 50Hz");
        entering.Input(false,-1);for(int i=0;i<150;i++)entering.FixedUpdate();
        Check(!entering.InLava&&entering.transform.position.z<2,"reversing out of lava returns to ordinary land movement");
        Time.fixedDeltaTime=Time.deltaTime=step;
    }
}

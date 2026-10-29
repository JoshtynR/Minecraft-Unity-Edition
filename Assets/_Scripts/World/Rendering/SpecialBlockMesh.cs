using System;
using UnityEngine;

// Code-defined models; never changes the user's block catalog or images.
public static class SpecialBlockMesh
{
    private static readonly Direction[] Faces={Direction.backwards,Direction.forwards,Direction.left,Direction.right,Direction.down,Direction.up};
    private static readonly Vector3Int[] Neighbors={new Vector3Int(-1,0,0),new Vector3Int(1,0,0),new Vector3Int(0,0,-1),new Vector3Int(0,0,1)};
    public static void Append(BlockType t,byte s,Vector3Int pos,Func<Vector3Int,BlockType> get,
        Func<Direction,int> layer,Color tint,MeshData mesh,Func<BlockTexturePart,int> parts=null)
    {
        if(SpecialBlockGeometry.IsTorch(t)){Torch(s,pos,layer(Direction.backwards),tint,mesh);return;}
        if(t==BlockType.Fence)
        {
            Cube(B(6,0,6,10,16,10),pos,layer,tint,mesh);
            bool left=get!=null&&get(pos+Neighbors[0])==t,right=get!=null&&get(pos+Neighbors[1])==t;
            bool back=get!=null&&get(pos+Neighbors[2])==t,front=get!=null&&get(pos+Neighbors[3])==t;
            bool x=left||right||(!back&&!front),z=back||front;
            for(int y=6;y<=12;y+=6)
            {
                if(x)Cube(B(left?0:7,y,7,right?16:9,y+3,9),pos,layer,tint,mesh);
                if(z)Cube(B(7,y,back?0:7,9,y+3,front?16:9),pos,layer,tint,mesh);
            }
            return;
        }
        if(t==BlockType.RedstoneWire){Wire(s,pos,get,layer,tint,mesh,parts);return;}
        if(SpecialBlockGeometry.IsRail(t))
        {
            int r=t==BlockType.Rail?s:s&7;
            var a=V(0,.0625f,0);var b=V(0,.0625f,1);var c=V(1,.0625f,1);var d=V(1,.0625f,0);
            if(r==2){c.y+=1;d.y+=1;} if(r==3){a.y+=1;b.y+=1;}
            if(r==4){a.y+=1;d.y+=1;} if(r==5){b.y+=1;c.y+=1;}
            Quad(pos,a,b,c,d,Texture(parts,t!=BlockType.Rail&&(s&8)!=0?BlockTexturePart.RailPowered:r>=6?BlockTexturePart.RailCorner:BlockTexturePart.RailStraight,layer(r>=6?Direction.backwards:Direction.up)),tint,mesh,true);
            RotateUV(mesh,r==1||r==2||r==3?1:r>=6?r-6:0);return;
        }
        if(t==BlockType.Ladder)
        {
            if(s==2||s==3){float z=s==2?.95f:.05f;Quad(pos,V(0,0,z),V(0,1,z),V(1,1,z),V(1,0,z),layer(Direction.backwards),tint,mesh,true);}
            else{float x=s==4?.95f:.05f;Quad(pos,V(x,0,1),V(x,1,1),V(x,1,0),V(x,0,0),layer(Direction.backwards),tint,mesh,true);}
            return;
        }
        if(t==BlockType.Fire)
        {
            // Beta's floor flame fills the voxel footprint with eight leaning
            // sheets; the old narrow crossed plant only covered its centre.
            int a=Texture(parts,BlockTexturePart.FireA,layer(Direction.backwards));
            int b=Texture(parts,BlockTexturePart.FireB,layer(Direction.backwards));
            FireX(.7f,.2f,pos,a,tint,mesh);FireX(.3f,.8f,pos,a,tint,mesh);
            FireZ(.3f,.8f,pos,b,tint,mesh);FireZ(.7f,.2f,pos,b,tint,mesh);
            FireX(0,.1f,pos,b,tint,mesh);FireX(1,.9f,pos,b,tint,mesh);
            FireZ(1,.9f,pos,a,tint,mesh);FireZ(0,.1f,pos,a,tint,mesh);
            return;
        }
        if(t==BlockType.Cobweb)
        {
            float a=t==BlockType.Cobweb?0:.2f,b=1-a;
            Quad(pos,V(a,0,a),V(a,1.4f,a),V(b,1.4f,b),V(b,0,b),Texture(parts,t==BlockType.Fire?BlockTexturePart.FireA:BlockTexturePart.Surface,layer(Direction.backwards)),tint,mesh,true);
            Quad(pos,V(a,0,b),V(a,1.4f,b),V(b,1.4f,a),V(b,0,a),Texture(parts,t==BlockType.Fire?BlockTexturePart.FireB:BlockTexturePart.Surface,layer(Direction.backwards)),tint,mesh,true);
            if(t==BlockType.Cobweb) for(int i=mesh.vertices.Count-16;i<mesh.vertices.Count;i++){var v=mesh.vertices[i];v.y=pos.y+(v.y-pos.y)/1.4f;mesh.vertices[i]=v;}
            return;
        }
        if(t==BlockType.StandingSign)
        {
            int start=mesh.vertices.Count;
            Cube(B(7,0,7,9,8,9),pos,d=>Texture(parts,BlockTexturePart.SignPost,layer(d)),tint,mesh);
            Cube(B(0,8,7,16,16,9),pos,d=>Texture(parts,d==Direction.backwards?BlockTexturePart.SignFront:d==Direction.forwards?BlockTexturePart.SignBack:BlockTexturePart.SignEdge,layer(d)),tint,mesh,true);
            Rotate(mesh,start,pos,s*Math.PI/8);return;
        }
        if(t==BlockType.WallSign)
        {
            Direction front=s==2?Direction.backwards:s==3?Direction.forwards:s==4?Direction.left:Direction.right;
            Direction back=front==Direction.backwards?Direction.forwards:front==Direction.forwards?Direction.backwards:front==Direction.left?Direction.right:Direction.left;
            Cube(SpecialBlockGeometry.Selection(t,s),pos,d=>Texture(parts,d==front?BlockTexturePart.SignFront:d==back?BlockTexturePart.SignBack:BlockTexturePart.SignEdge,layer(d)),tint,mesh,true);return;
        }
        if(t==BlockType.Lever)
        {
            var baseShape=SpecialBlockGeometry.Selection(t,s);
            if((s&7)>=5)baseShape=B(5,0,4,11,3,12);
            Cube(baseShape,pos,d=>Texture(parts,BlockTexturePart.LeverBase,layer(d)),tint,mesh);
            // Handle in local floor coordinates, then mount on the selected wall.
            int start=mesh.vertices.Count;Cube(B(7,1,7,9,11,9),pos,d=>Texture(parts,BlockTexturePart.LeverHandle,layer(d)),tint,mesh);
            for(int i=start;i<mesh.vertices.Count;i++)
            {
                var v=mesh.vertices[i];float x=v.x-pos.x,y=v.y-pos.y,z=v.z-pos.z;
                z+=(y-.0625f)*((s&8)==0?-.4f:.4f);
                int mount=s&7;
                mesh.vertices[i]= mount==1?V(pos.x+y,pos.y+x,pos.z+z):mount==2?V(pos.x+1-y,pos.y+x,pos.z+z):
                    mount==3?V(pos.x+z,pos.y+x,pos.z+y):mount==4?V(pos.x+z,pos.y+x,pos.z+1-y):V(pos.x+x,pos.y+y,pos.z+z);
            }
            return;
        }
        if(t==BlockType.RedstoneRepeater||t==BlockType.PoweredRepeater)
        {
            int start=mesh.vertices.Count;Cube(BlockShapesList.Repeater,pos,d=>Texture(parts,d==Direction.up?BlockTexturePart.RepeaterTop:d==Direction.down?BlockTexturePart.RepeaterBottom:BlockTexturePart.RepeaterSide,layer(d)),tint,mesh);
            Torch(5,new Vector3Int(),Texture(parts,BlockTexturePart.RepeaterTorch,layer(Direction.backwards)),tint,mesh);
            int torchStart=mesh.vertices.Count-20;
            for(int i=torchStart;i<mesh.vertices.Count;i++){var v=mesh.vertices[i];mesh.vertices[i]=V(pos.x+v.x,pos.y+v.y-.1875f,pos.z+v.z-.3125f);}
            Torch(5,new Vector3Int(),Texture(parts,BlockTexturePart.RepeaterTorch,layer(Direction.backwards)),tint,mesh);
            torchStart=mesh.vertices.Count-20;float offset=-.0625f+((s>>2)&3)*.125f;
            for(int i=torchStart;i<mesh.vertices.Count;i++){var v=mesh.vertices[i];mesh.vertices[i]=V(pos.x+v.x,pos.y+v.y-.1875f,pos.z+v.z+offset);}
            Rotate(mesh,start,pos,(s&3)*Math.PI/2);return;
        }
        if(t==BlockType.Bed)
        {
            int start=mesh.vertices.Count;
            foreach(var d in Faces)
            {
                if(d==((s&8)==0?Direction.forwards:Direction.backwards))continue;
                var b=d==Direction.down?B(0,3,0,16,9,16):BlockShapesList.Bed;
                b.SetFaceVertices(d,pos,mesh);mesh.AddQuadTriangles();
                bool head=(s&8)!=0;
                var part=d==Direction.down?BlockTexturePart.BedBottom:d==Direction.up?
                    (head?BlockTexturePart.BedHeadTop:BlockTexturePart.BedFootTop):
                    d==(head?Direction.forwards:Direction.backwards)?(head?BlockTexturePart.BedHeadEnd:BlockTexturePart.BedFootEnd):
                    (head?BlockTexturePart.BedHeadSide:BlockTexturePart.BedFootSide);
                mesh.AddFaceUVs(Texture(parts,part,layer(d)),d==Direction.up||d==Direction.down?new Vector4(1,1,0,0):b.FaceUVTransform(d));
                // Beta's separate bed-side tiles are directional: both long
                // faces must run from foot to head. BlockShape emits the left
                // face in the opposite Z order, so mirror that face to keep
                // the pillow and outer leg away from the centre seam.
                if(d==Direction.left)MirrorLastFaceU(mesh);
                mesh.AddQuadColor(tint);
            }
            Rotate(mesh,start,pos,(s&3)*Math.PI/2);return;
        }
        if(t==BlockType.NetherPortal)
        {
            // A portal is a two-sided animated plane with selection thickness,
            // not a textured cuboid. Rendering its four thin edge faces wraps
            // the animation into stretched strips along the frame.
            var portal=SpecialBlockGeometry.Selection(t,s);
            var first=(s&1)==0?Direction.backwards:Direction.left;
            var second=(s&1)==0?Direction.forwards:Direction.right;
            PortalFace(portal,first,pos,layer(first),tint,mesh);
            PortalFace(portal,second,pos,layer(second),tint,mesh);
            return;
        }
        if(t==BlockType.PistonHead)
        {
            int start=mesh.vertices.Count;Cube(B(0,12,0,16,16,16),pos,d=>Texture(parts,d==Direction.up?((s&8)!=0?BlockTexturePart.PistonStickyFace:BlockTexturePart.PistonFace):d==Direction.down?BlockTexturePart.PistonInner:BlockTexturePart.PistonSide,layer(d)),tint,mesh);
            Cube(B(6,0,6,10,12,10),pos,d=>Texture(parts,BlockTexturePart.PistonRod,layer(d)),tint,mesh);
            OrientPiston(mesh,start,pos,s&7);return;
        }
        if(t==BlockType.MovingPiston)return; // Its visual belongs to a moving tile entity, not a static cube.
        if(t==BlockType.Piston||t==BlockType.StickyPiston)
        {
            int start=mesh.vertices.Count;Cube((s&8)==0?BlockShapesList.FullBlock:B(0,0,0,16,12,16),pos,d=>Texture(parts,d==Direction.up?((s&8)!=0?BlockTexturePart.PistonInner:t==BlockType.StickyPiston?BlockTexturePart.PistonStickyFace:BlockTexturePart.PistonFace):d==Direction.down?BlockTexturePart.PistonBottom:BlockTexturePart.PistonSide,layer(d)),tint,mesh);
            OrientPiston(mesh,start,pos,s&7);return;
        }
        if(t==BlockType.Chest||t==BlockType.LockedChest)
        {
            int half=0;
            if(get!=null)foreach(var n in Neighbors)
            {
                if(get(pos+n)!=t)continue;
                var local=SpecialBlockGeometry.TextureDirection(t,s,n.x<0?Direction.left:n.x>0?Direction.right:n.z<0?Direction.backwards:Direction.forwards);
                if(local==Direction.left)half=2;
                if(local==Direction.right)half=1;
            }
            Cube(t==BlockType.Chest?SpecialBlockGeometry.Selection(t,s):BlockShapesList.FullBlock,pos,d=>
            {
                var local=SpecialBlockGeometry.TextureDirection(t,s,d);
                if(half==0)return layer(d);
                BlockTexturePart role=local==Direction.up?(half==1?BlockTexturePart.ChestTopLeft:BlockTexturePart.ChestTopRight):
                    local==Direction.backwards?(half==1?BlockTexturePart.ChestFrontLeft:BlockTexturePart.ChestFrontRight):
                    local==Direction.forwards?(half==1?BlockTexturePart.ChestBackLeft:BlockTexturePart.ChestBackRight):BlockTexturePart.Surface;
                return Texture(parts,role,layer(d));
            },tint,mesh);return;
        }
        var bounds=SpecialBlockGeometry.Selection(t,s);
        if(SpecialBlockGeometry.IsDoor(t))
        {
            int rotation=(s&4)==0?(s-1)&3:s&3;
            foreach(var d in Faces)
            {
                bool broad=(rotation&1)==0?(d==Direction.backwards||d==Direction.forwards):(d==Direction.left||d==Direction.right);
                bounds.SetFaceVertices(d,pos,mesh);mesh.AddQuadTriangles();
                mesh.AddFaceUVs(Texture(parts,(s&8)!=0?BlockTexturePart.DoorUpper:BlockTexturePart.DoorLower,layer((s&8)!=0?Direction.up:Direction.backwards)),broad?new Vector4(1,1,0,0):bounds.FaceUVTransform(d));
                int side=d==Direction.backwards?2:d==Direction.forwards?3:d==Direction.left?4:d==Direction.right?5:0;
                if(broad&&((rotation/2+((side&1)^rotation)+((s&4)/4))&1)!=0)MirrorLastFaceU(mesh);
                mesh.AddQuadColor(tint);
            }
        }
        else Cube(bounds,pos,d=>t==BlockType.Cake&&(s&7)>0&&d==Direction.left?Texture(parts,BlockTexturePart.CakeInside,layer(d)):layer(d),tint,mesh,t==BlockType.Trapdoor);
    }
    private static int Texture(Func<BlockTexturePart,int> parts,BlockTexturePart role,int fallback)
    {
        int index=parts==null?0:parts(role);return index==0?fallback:index;
    }
    private static void FireX(float bottom,float top,Vector3Int p,int layer,Color tint,MeshData m)
    { Quad(p,V(bottom,0,0),V(top,1.4f,0),V(top,1.4f,1),V(bottom,0,1),layer,tint,m,true); }
    private static void FireZ(float bottom,float top,Vector3Int p,int layer,Color tint,MeshData m)
    { Quad(p,V(0,0,bottom),V(0,1.4f,top),V(1,1.4f,top),V(1,0,bottom),layer,tint,m,true); }
    private static BlockShape B(float a,float b,float c,float d,float e,float f)=>SpecialBlockGeometry.Box(a,b,c,d,e,f);
    private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    private static void PortalFace(BlockShape bounds,Direction direction,Vector3Int pos,int layer,Color tint,MeshData mesh)
    {
        bounds.SetFaceVertices(direction,pos,mesh);mesh.AddQuadTriangles();
        mesh.AddFaceUVs(layer,bounds.FaceUVTransform(direction));mesh.AddQuadColor(tint);
    }
    private static void Cube(BlockShape b,Vector3Int pos,Func<Direction,int> layer,Color tint,MeshData m,bool fullUV=false)
    {
        float x=b.xMax-b.xMin,y=b.yMax-b.yMin,z=b.zMax-b.zMin;
        foreach(var d in Faces)
        {
            bool broad=x<y&&x<z?(d==Direction.left||d==Direction.right):y<x&&y<z?(d==Direction.up||d==Direction.down):(d==Direction.forwards||d==Direction.backwards);
            b.SetFaceVertices(d,pos,m);m.AddQuadTriangles();m.AddFaceUVs(layer(d),fullUV&&broad?new Vector4(1,1,0,0):b.FaceUVTransform(d));m.AddQuadColor(tint);
        }
    }
    private static void Quad(Vector3Int pos,Vector3 a,Vector3 b,Vector3 c,Vector3 d,int layer,Color tint,MeshData m,bool both=false)
    {
        m.AddVertex((Vector3)pos+a);m.AddVertex((Vector3)pos+b);m.AddVertex((Vector3)pos+c);m.AddVertex((Vector3)pos+d);
        m.AddQuadTriangles();m.AddFaceUVs(layer);m.AddQuadColor(tint);
        if(both)Quad(pos,d,c,b,a,layer,tint,m);
    }
    private static void Rotate(MeshData m,int first,Vector3Int p,double angle)
    {
        float c=(float)Math.Cos(angle),s=(float)Math.Sin(angle);
        for(int i=first;i<m.vertices.Count;i++){var v=m.vertices[i];float x=v.x-p.x-.5f,z=v.z-p.z-.5f;m.vertices[i]=V(p.x+.5f+x*c-z*s,v.y,p.z+.5f+x*s+z*c);}
    }
    private static void RotateUV(MeshData m,int turns)
    {
        for(int i=m.uv.Count-8;i<m.uv.Count;i++){var uv=m.uv[i];for(int j=0;j<(turns&3);j++){float x=uv.x;uv.x=uv.y;uv.y=1-x;}m.uv[i]=uv;}
    }
    private static void MirrorLastFaceU(MeshData m)
    {
        int first=m.uv.Count-4;
        float min=m.uv[first].x,max=min;
        for(int i=first+1;i<m.uv.Count;i++){min=Math.Min(min,m.uv[i].x);max=Math.Max(max,m.uv[i].x);}
        for(int i=first;i<m.uv.Count;i++){var uv=m.uv[i];uv.x=min+max-uv.x;m.uv[i]=uv;}
    }
    private static void OrientPiston(MeshData m,int first,Vector3Int p,int facing)
    {
        for(int i=first;i<m.vertices.Count;i++)
        {
            var v=m.vertices[i];float x=v.x-p.x,y=v.y-p.y,z=v.z-p.z;
            m.vertices[i]=facing==0?V(p.x+x,p.y+1-y,p.z+1-z):facing==2?V(p.x+x,p.y+z,p.z+1-y):
                facing==3?V(p.x+x,p.y+1-z,p.z+y):facing==4?V(p.x+1-y,p.y+x,p.z+z):facing==5?V(p.x+y,p.y+1-x,p.z+z):v;
        }
    }
    private static void Torch(byte state,Vector3Int pos,int layer,Color tint,MeshData m)
    {
        int start=m.vertices.Count;
        float dx=state==1?-.4f:state==2?.4f:0,dz=state==3?-.4f:state==4?.4f:0;
        float x=.5f+dx*.25f,z=.5f+dz*.25f,y=state>=1&&state<=4?.2f:0;
        // Original renderer: four one-block image sheets and the cropped 2x2 cap.
        Quad(pos,V(x-.0625f+dx,y,z-.5f+dz),V(x-.0625f,y+1,z-.5f),V(x-.0625f,y+1,z+.5f),V(x-.0625f+dx,y,z+.5f+dz),layer,tint,m);
        Quad(pos,V(x+.0625f+dx,y,z+.5f+dz),V(x+.0625f,y+1,z+.5f),V(x+.0625f,y+1,z-.5f),V(x+.0625f+dx,y,z-.5f+dz),layer,tint,m);
        Quad(pos,V(x-.5f+dx,y,z+.0625f+dz),V(x-.5f,y+1,z+.0625f),V(x+.5f,y+1,z+.0625f),V(x+.5f+dx,y,z+.0625f+dz),layer,tint,m);
        Quad(pos,V(x+.5f+dx,y,z-.0625f+dz),V(x+.5f,y+1,z-.0625f),V(x-.5f,y+1,z-.0625f),V(x-.5f+dx,y,z-.0625f+dz),layer,tint,m);
        for(int i=start;i<start+16;i+=4)
        {
            var a=m.vertices[i];m.vertices[i]=m.vertices[i+3];m.vertices[i+3]=a;
            a=m.vertices[i+1];m.vertices[i+1]=m.vertices[i+2];m.vertices[i+2]=a;
        }
        x+=dx*.375f;z+=dz*.375f;
        Quad(pos,V(x-.0625f,y+.625f,z-.0625f),V(x-.0625f,y+.625f,z+.0625f),V(x+.0625f,y+.625f,z+.0625f),V(x+.0625f,y+.625f,z-.0625f),layer,tint,m);
        int first=m.uv.Count-4;for(int i=first;i<m.uv.Count;i++){var uv=m.uv[i];uv.x=.4375f+uv.x*.125f;uv.y=.5f+uv.y*.125f;m.uv[i]=uv;}
    }
    private static void Overlay(MeshData m,int first,int count,int layer)
    {
        if(layer==0)return;
        for(int q=first;q<first+count;q+=4)
        {
            // Offset along the quad normal, so climbing wire also avoids z-fighting.
            var a=m.vertices[q];var b=m.vertices[q+1];var c=m.vertices[q+2];
            float ax=b.x-a.x,ay=b.y-a.y,az=b.z-a.z,bx=c.x-a.x,by=c.y-a.y,bz=c.z-a.z;
            float nx=ay*bz-az*by,ny=az*bx-ax*bz,nz=ax*by-ay*bx;
            float length=(float)Math.Sqrt(nx*nx+ny*ny+nz*nz);
            float k=length>0?.0001f/length:0;
            for(int i=0;i<4;i++){var v=m.vertices[q+i];m.AddVertex(V(v.x+nx*k,v.y+ny*k,v.z+nz*k));}
            m.AddQuadTriangles();m.AddFaceUVs(layer);m.AddQuadColor(new Color(1,1,1,1));
            for(int i=0;i<4;i++){var uv=m.uv[q+i];uv.z=layer;m.uv[m.uv.Count-4+i]=uv;}
        }
    }
    private static bool Solid(BlockType t)=>SpecialBlockGeometry.NormalCube(t);
    private static bool Connect(BlockType t)=>t==BlockType.RedstoneWire||t==BlockType.RedstoneTorch||t==BlockType.Lever||t==BlockType.StoneButton||t==BlockType.StonePressurePlate||t==BlockType.WoodenPressurePlate||t==BlockType.DetectorRail;
    private static void Wire(byte power,Vector3Int p,Func<Vector3Int,BlockType> get,Func<Direction,int> layer,Color tint,MeshData m,Func<BlockTexturePart,int> parts)
    {
        int mask=0,up=0;
        for(int i=0;i<4;i++)if(get!=null)
        {
            var n=p+Neighbors[i];var t=get(n);
            bool climb=!Solid(get(p+new Vector3Int(0,1,0)))&&Solid(t)&&get(n+new Vector3Int(0,1,0))==BlockType.RedstoneWire;
            if(Connect(t)||(!Solid(t)&&Connect(get(n+new Vector3Int(0,-1,0))))||climb)mask|=1<<i;
            if(climb)up|=1<<i;
        }
        float f=power/15f;
        Color red=new Color(power==0?.3f:f*.6f+.4f,Math.Max(0,f*f*.7f-.5f),Math.Max(0,f*f*.6f-.7f),1);
        float x0=0,x1=1,z0=0,z1=1;
        bool lineX=(mask&3)!=0&&(mask&12)==0,lineZ=(mask&12)!=0&&(mask&3)==0;
        if(mask!=0&&!lineX&&!lineZ){if((mask&1)==0)x0=.3125f;if((mask&2)==0)x1=.6875f;if((mask&4)==0)z0=.3125f;if((mask&8)==0)z1=.6875f;}
        Quad(p,V(x0,.015625f,z0),V(x0,.015625f,z1),V(x1,.015625f,z1),V(x1,.015625f,z0),Texture(parts,BlockTexturePart.WireCross,layer(Direction.up)),red,m,true);
        // Top = cross artwork, Side = straight artwork. No catalog migration.
        if(lineX||lineZ)
        {
            for(int i=m.uv.Count-8;i<m.uv.Count;i++){var uv=m.uv[i];uv.z=Texture(parts,BlockTexturePart.WireLine,layer(Direction.backwards));m.uv[i]=uv;}
            if(lineX)RotateUV(m,1);
        }
        else for(int i=m.uv.Count-8;i<m.uv.Count;i++){var uv=m.uv[i];uv.x=x0+uv.x*(x1-x0);uv.y=z0+uv.y*(z1-z0);m.uv[i]=uv;}
        Overlay(m,m.vertices.Count-8,8,Texture(parts,lineX||lineZ?BlockTexturePart.WireLineOverlay:BlockTexturePart.WireCrossOverlay,0));
        for(int i=0;i<4;i++)if((up&(1<<i))!=0)
        {
            if(i<2){float x=i==0?.015625f:.984375f;Quad(p,V(x,0,0),V(x,1.021875f,0),V(x,1.021875f,1),V(x,0,1),Texture(parts,BlockTexturePart.WireLine,layer(Direction.backwards)),red,m,true);}
            else{float z=i==2?.015625f:.984375f;Quad(p,V(0,0,z),V(0,1.021875f,z),V(1,1.021875f,z),V(1,0,z),layer(Direction.backwards),red,m,true);}
        }
    }
}

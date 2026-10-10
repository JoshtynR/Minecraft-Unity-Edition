using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

public static class TreeParityCheck
{
    private struct Pos : IComparable<Pos>
    {
        public int x,y,z;
        public Pos(int x,int y,int z) { this.x=x;this.y=y;this.z=z; }
        public int CompareTo(Pos p) { int c=x.CompareTo(p.x);if(c==0)c=y.CompareTo(p.y);if(c==0)c=z.CompareTo(p.z);return c; }
    }
    private sealed class TestWorld : IBetaTreeWorld
    {
        private int env,x,y,z;
        public SortedDictionary<Pos,BlockType> writes=new SortedDictionary<Pos,BlockType>();
        public TestWorld(int env,int x,int y,int z) { this.env=env;this.x=x;this.y=y;this.z=z; }
        public BlockType Get(int a,int b,int c)
        {
            BlockType block;
            if(writes.TryGetValue(new Pos(a,b,c),out block))return block;
            if(b<0||b>=128)return BlockType.Air;
            if(env==1 && a==x && c==z && b==y+4)return BlockType.Stone;
            if(env==2 && a==x && c==z && b==y+8)return BlockType.Stone;
            if(env==3 && a==x+2 && c==z+2 && b==y+4)return BlockType.Stone;
            if(env==5 && b==y-1)return BlockType.Sand;
            if(env==6 && a==x && c==z && b==y)return BlockType.Water;
            if(env==7 && b>=y && b<y+2 && Math.Abs(a-x)<=2 && Math.Abs(c-z)<=2)return BlockType.Leaves;
            if(env==8 && a==x && c==z && b==y+1)return BlockType.Log;
            if(env==9 && a==x+2 && c==z && b==y+5)return BlockType.Glass;
            if(env==10 && a==x-2 && c==z-2 && b==y+4)return BlockType.Water;
            if(b==y-1)return BlockType.Grass;
            return b<y-1?BlockType.Stone:BlockType.Air;
        }
        public void Set(int a,int b,int c,BlockType block) { if(b>=0&&b<128)writes[new Pos(a,b,c)]=block; }
        public bool IsOpaque(BlockType block) { return block!=BlockType.Air && block!=BlockType.Water && block!=BlockType.Glass && !Beta173Trees.IsLeaves(block); }
        public string Hash()
        {
            var text=new StringBuilder();
            foreach(var e in writes)text.Append(e.Key.x+","+e.Key.y+","+e.Key.z+","+(int)e.Value+"\n");
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "").ToLowerInvariant();
        }
    }
    public static int Main(string[] args)
    {
        int cases=0,failures=0,checks=0;
        foreach(var line in File.ReadLines(args[0]))
        {
            if(line.StartsWith("#")||line.Length==0)continue;
            var p=line.Split('\t');string error=null;
            if(p[0]=="tree")
            {
                var kind=(BetaTreeKind)int.Parse(p[1]);var world=new TestWorld(int.Parse(p[2]),int.Parse(p[4]),int.Parse(p[5]),int.Parse(p[6]));
                var random=new JavaRandom(long.Parse(p[3]));
                bool success=Beta173Trees.Generate(world,random,kind,int.Parse(p[4]),int.Parse(p[5]),int.Parse(p[6]));
                long next=random.NextLong();checks+=4;
                if(success!=bool.Parse(p[7]))error="success";
                else if(world.writes.Count!=int.Parse(p[8]))error="block count "+world.writes.Count+" expected "+p[8];
                else if(world.Hash()!=p[9])error="coordinates/materials";
                else if(next!=long.Parse(p[10]))error="population RNG "+next+" expected "+p[10];
            }
            else if(p[0]=="select")
            {
                var random=new JavaRandom(long.Parse(p[2]));var kind=Beta173Trees.Select((BetaBiomeType)int.Parse(p[1]),random);checks+=2;
                if((int)kind!=int.Parse(p[3]))error="biome selection";
                else if(random.NextLong()!=long.Parse(p[4]))error="selection RNG";
            }
            else if(p[0]=="count")
            {
                var random=new JavaRandom(long.Parse(p[2]));int count=Beta173Trees.AttemptCount((BetaBiomeType)int.Parse(p[1]),double.Parse(p[3],CultureInfo.InvariantCulture),random);checks+=2;
                if(count!=int.Parse(p[4]))error="attempt count";
                else if(random.NextLong()!=long.Parse(p[5]))error="count RNG";
            }
            cases++;
            if(error!=null){if(failures++<30)Console.WriteLine("FAIL "+line+": "+error);}
        }
        Console.WriteLine(cases+" reference cases, "+checks+" checks, "+failures+" failures.");
        return failures==0?0:1;
    }
}

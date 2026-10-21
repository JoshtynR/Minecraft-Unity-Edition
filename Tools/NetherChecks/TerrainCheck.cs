using System;using System.IO;using System.Security.Cryptography;
public static class TerrainCheck {
 public static void Main(string[] args){int checks=0;foreach(var line in File.ReadAllLines(args[0])){
 var row=line.Split('\t');long seed=long.Parse(row[0]);int x=int.Parse(row[1]),z=int.Parse(row[2]);var generator=new Beta173NetherTerrain(seed);var blocks=generator.GenerateRaw(x,z);
 if(row[3]!="raw")generator.Surface(blocks,x,z);if(row[3]=="caves")new Beta173NetherCaves(seed).Generate(blocks,x,z);
 var bytes=new byte[32768];for(int xx=0;xx<16;xx++)for(int zz=0;zz<16;zz++)for(int y=0;y<128;y++){BetaBlockCatalog.Entry e;var type=blocks[xx,y,zz];int id=type==BlockType.Air?0:BetaBlockCatalog.TryGet(type,out e)?e.SourceId:-1;if(id<0)throw new Exception("Unknown block "+type);bytes[(xx*16+zz)*128+y]=(byte)id;}
 string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
 if(hash!=row[4])throw new Exception("Nether mismatch: "+string.Join(" ",row)+" actual="+hash);checks++;}
 Console.WriteLine(checks+" Java-reference Nether raw/surface/cave chunk hashes matched ("+(checks*32768)+" block comparisons).");}
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Generated crops keep the source PNGs and original GUIDs untouched.
public sealed class MinecraftTextureImageTools
{
    private readonly Dictionary<Texture2D,Color32[]> pixels=new Dictionary<Texture2D,Color32[]>();
    public Color32[] Read(Texture2D source)
    {
        Color32[] result;if(pixels.TryGetValue(source,out result))return result;
        if(source.isReadable)result=source.GetPixels32();
        else
        {
            var previous=RenderTexture.active;bool srgb=GL.sRGBWrite;var filter=source.filterMode;
            var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,false);
            try
            {
                source.filterMode=FilterMode.Point;GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Graphics.Blit(source,target);RenderTexture.active=target;
                copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);
                result=copy.GetPixels32();
            }
            finally {source.filterMode=filter;GL.sRGBWrite=srgb;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(copy);}
        }
        pixels.Add(source,result);return result;
    }
    public Texture2D Icon(Texture2D source)
    {
        if(source.height<=source.width||source.height%source.width!=0)return source;
        return Crop(source,0,0,source.width,source.width,"icon");
    }
    public Texture2D Skin(Texture2D source)
    {
        if(source.width==source.height*2)return source;
        return source.width==source.height?Crop(source,0,0,source.width,source.height/2,"classic_skin"):null;
    }
    public Texture2D Crop(Texture2D source,int x,int y,int width,int height,string name)
    {
        var raw=Read(source);var result=new Color32[width*height];
        for(int row=0;row<height;row++)Array.Copy(raw,(source.height-y-height+row)*source.width+x,result,row*width,width);
        return Save(source,name,width,height,result);
    }
    public Texture2D Breaking(Texture2D[] frames)
    {
        int size=frames[0].width;
        foreach(var frame in frames)if(frame.width!=size||frame.height!=size)return null;
        var result=new Color32[size*size*10];
        for(int frame=0;frame<10;frame++)
        {
            var raw=Read(frames[frame]);
            for(int row=0;row<size;row++)Array.Copy(raw,row*size,result,row*size*10+frame*size,size);
        }
        return Save(frames[0],"breaking",size*10,size,result);
    }
    private static Texture2D Save(Texture2D source,string name,int width,int height,Color32[] pixels)
    {
        const string directory="Assets/Textures/Minecraft_Generated";
        string absolute=Path.Combine(Path.GetDirectoryName(Application.dataPath),directory);
        if(!AssetDatabase.IsValidFolder(directory)){Directory.CreateDirectory(absolute);AssetDatabase.Refresh();}
        string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
        string path=directory+"/"+guid+"_"+name+".png";
        string disk=Path.Combine(Path.GetDirectoryName(Application.dataPath),path);
        var image=new Texture2D(width,height,TextureFormat.RGBA32,false,false);
        byte[] bytes;
        try{image.SetPixels32(pixels);image.Apply(false,false);bytes=image.EncodeToPNG();}
        finally{UnityEngine.Object.DestroyImmediate(image);}
        bool same=File.Exists(disk)&&Equal(File.ReadAllBytes(disk),bytes);
        if(!same){File.WriteAllBytes(disk,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer!=null&&(importer.npotScale!=TextureImporterNPOTScale.None||importer.mipmapEnabled||
            importer.filterMode!=FilterMode.Point||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize<Math.Max(width,height)))
        {
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            int size=32;while(size<width||size<height)size*=2;importer.maxTextureSize=Math.Max(importer.maxTextureSize,Math.Min(16384,size));
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static bool Equal(byte[] a,byte[] b)
    {if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
}

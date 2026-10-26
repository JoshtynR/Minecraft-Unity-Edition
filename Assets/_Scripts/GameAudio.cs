using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

// A fixed pool keeps overlapping sounds at their original positions without
// creating/destroying an AudioSource object for every footstep or mined block.
public sealed class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private static readonly Dictionary<string,AudioClip[]> families=new Dictionary<string,AudioClip[]>();
    private static readonly Dictionary<string,AudioClip[]> directories=new Dictionary<string,AudioClip[]>();
    private static readonly System.Random cosmetic=new System.Random();
    private readonly AudioSource[] voices=new AudioSource[24];
    private int nextVoice;
    public static float Variation(float min,float max) => min+(float)cosmetic.NextDouble()*(max-min);

    // A family matches only its unnumbered clip and numbered variants, never
    // unrelated prefixes such as cow/say and cow/say_special.
    public static bool Matches(string name,string stem)
    {
        if(!name.StartsWith(stem,StringComparison.OrdinalIgnoreCase))return false;
        for(int i=stem.Length;i<name.Length;i++)if(!char.IsDigit(name[i]))return false;
        return true;
    }
    private static AudioClip[] LoadFamily(string family)
    {
        int slash=family.LastIndexOf('/');
        string directory="Sounds/"+(slash<0?"":family.Substring(0,slash)),stem=family.Substring(slash+1);
        AudioClip[] clips;
        if(!directories.TryGetValue(directory,out clips))directories[directory]=clips=Resources.LoadAll<AudioClip>(directory);
        var found=new List<AudioClip>();
        foreach(var clip in clips)if(clip!=null&&Matches(clip.name,stem))found.Add(clip);
        found.Sort((a,b)=>StringComparer.Ordinal.Compare(a.name,b.name));
        return found.ToArray();
    }
    public static AudioClip Find(string family)
    {
        if(string.IsNullOrEmpty(family))return null;
        AudioClip[] clips;
        if(!families.TryGetValue(family,out clips))
        {
            clips=LoadFamily(family);
            if(clips.Length==0&&family=="random/classic_hurt")clips=LoadFamily("damage/hit");
            if(clips.Length==0&&family.StartsWith("step/",StringComparison.Ordinal))clips=LoadFamily("step 1/"+family.Substring(5));
            if(clips.Length==0&&family.StartsWith("step/",StringComparison.Ordinal))clips=LoadFamily("dig/"+family.Substring(5));
            families[family]=clips;
        }
        return clips.Length==0?null:clips[cosmetic.Next(clips.Length)];
    }
    public static void Play(string family,Vector3 position,float volume=1,float pitch=1)
    {if(!Application.isBatchMode&&!WorldLoadingScreen.IsLoading)PlayClip(Find(family),position,volume,true,pitch);}
    public static void PlayClip(AudioClip clip,Vector3 position,float volume,bool spatial=true,float pitch=1)
    {
        if(clip==null||Application.isBatchMode||spatial&&WorldLoadingScreen.IsLoading)return;
        var camera=Camera.main;
        if(spatial&&camera!=null&&(position-camera.transform.position).sqrMagnitude>32*32)return;
        if(instance==null)instance=new GameObject("GameplayAudio").AddComponent<GameAudio>();
        AudioSource voice=null;
        for(int i=0;i<instance.voices.Length;i++)
        {
            int index=(instance.nextVoice+i)%instance.voices.Length;
            if(!instance.voices[index].isPlaying){voice=instance.voices[index];instance.nextVoice=(index+1)%instance.voices.Length;break;}
        }
        if(voice==null){voice=instance.voices[instance.nextVoice];instance.nextVoice=(instance.nextVoice+1)%instance.voices.Length;}
        voice.Stop();voice.transform.position=position;voice.clip=clip;voice.spatialBlend=spatial?1:0;
        voice.volume=Mathf.Clamp01(volume);voice.pitch=Mathf.Clamp(pitch,.25f,2);voice.Play();
    }
    public static void Broadcast(string family,Vector3 position,float volume=1,float pitch=1,World world=null)
    {
        if(string.IsNullOrEmpty(family))return;
        if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.RpcGameplaySound((world??World.Instance).dimension,family,position,volume,pitch);
        else Play(family,position,volume,pitch);
    }
    public static void BroadcastBlock(BlockType type,Vector3Int position,bool broken,World world=null)
    {
        var realm=world??World.Instance;
        if(broken&&realm!=null)
        {
            if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.RpcBlockBreakParticles(realm.dimension,position,type);
            else realm.PlayBlockBreakParticles(position,type);
        }
        Broadcast(broken?BlockSoundRules.Break(type):BlockSoundRules.Place(type),(Vector3)position+Vector3.one*.5f,1,broken?1:.8f,realm);
    }
    private void Awake()
    {
        for(int i=0;i<voices.Length;i++)
        {
            var child=new GameObject("Voice "+i);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
            source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=2;source.maxDistance=24;source.dopplerLevel=0;
            voices[i]=source;
        }
    }
    private void OnDestroy()
    {if(instance==this){instance=null;families.Clear();directories.Clear();}}
}

public partial class WorldServer
{
    [ClientRpc] public void RpcGameplaySound(WorldDimension dimension,string family,Vector3 position,float volume,float pitch)
    {if(World.Instance!=null&&World.Instance.dimension==dimension)GameAudio.Play(family,position,volume,pitch);}
    [ClientRpc] public void RpcBlockBreakParticles(WorldDimension dimension,Vector3Int position,BlockType type)
    {var world=World.FindDimension(dimension);if(world!=null)world.PlayBlockBreakParticles(position,type);}
}

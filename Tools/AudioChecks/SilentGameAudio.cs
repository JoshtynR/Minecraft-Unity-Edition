using UnityEngine;
// Integration fixtures verify gameplay state independently of the audio device.
// AudioCheck compiles and exercises the real pooled GameAudio implementation.
public static class GameAudio
{
 public static int plays,blockBroadcasts;
 public static AudioClip Find(string family)=>null;
 public static float Variation(float min,float max)=>min;
 public static void Play(string family,Vector3 position,float volume=1,float pitch=1){if(family!=null)plays++;}
 public static void PlayClip(AudioClip clip,Vector3 position,float volume,bool spatial=true,float pitch=1){}
 public static void Broadcast(string family,Vector3 position,float volume=1,float pitch=1){}
 public static void BroadcastBlock(BlockType type,Vector3Int position,bool broken){blockBroadcasts++;}
}

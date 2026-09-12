using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 public enum SoundKind{Laser,Dash,Charge,Impact,BossShot,Hurt,Pistol}
 public sealed class SynthAudio:MonoBehaviour {
  static SynthAudio instance;Dictionary<SoundKind,AudioClip> clips=new Dictionary<SoundKind,AudioClip>();AudioSource source;
  void Awake(){instance=this;source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;foreach(SoundKind k in System.Enum.GetValues(typeof(SoundKind)))clips[k]=Generate(k);}
  public static void Play(SoundKind kind,Vector3 at,float volume){if(instance)instance.source.PlayOneShot(instance.clips[kind],volume);}
  AudioClip Generate(SoundKind kind){
   int rate=22050;float duration=kind==SoundKind.Impact?.7f:kind==SoundKind.Charge?.65f:.2f;int count=(int)(rate*duration);float[] data=new float[count];var random=new System.Random(37+(int)kind);double phase=0;
   for(int i=0;i<count;i++){float t=(float)i/count;double freq=kind==SoundKind.Pistol?Mathf.Lerp(1100,95,t):kind==SoundKind.Laser?Mathf.Lerp(1500,180,t):kind==SoundKind.Charge?Mathf.Lerp(180,1200,t):kind==SoundKind.BossShot?Mathf.Lerp(450,75,t):Mathf.Lerp(110,35,t);phase+=freq/rate*System.Math.PI*2;float noise=(float)(random.NextDouble()*2-1);float mix=kind==SoundKind.Pistol?.3f:kind==SoundKind.Dash?.85f:kind==SoundKind.Impact?.7f:kind==SoundKind.Hurt?.5f:.12f;data[i]=((float)System.Math.Sin(phase)*(1-mix)+noise*mix)*Mathf.Pow(1-t,2)*Mathf.Min(1,t*60)*.7f;if(kind==SoundKind.Pistol)data[i]=data[i]*.7f+(float)System.Math.Sin(i/(float)rate*145*Mathf.PI*2)*Mathf.Exp(-t*16)*Mathf.Min(1,t*120)*.25f;}
   var clip=AudioClip.Create(kind.ToString(),count,1,rate,false);clip.SetData(data,0);return clip;
  }
  void OnDestroy(){if(instance==this)instance=null;foreach(var c in clips.Values)Destroy(c);}
 }
}

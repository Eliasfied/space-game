using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 public sealed class GroundHazard:MonoBehaviour {
  public Transform Owner;AbilityCaster source;float energyGain,push;Vector3 pushDirection;
  float dotDuration,dotDamage,slow;
  float radius,delay,damage,born;Team victim;Color color;bool orbital;LineRenderer outline,progress;GameObject fill;
  public void Setup(Vector3 at,float radius,float delay,float damage,Team victim,Color color,bool orbital=false,AbilityCaster source=null,float energyGain=0,float push=0,Vector3 pushDirection=default,float dotDuration=0,float dotDamage=0,float slow=0){
   this.dotDuration=dotDuration;this.dotDamage=dotDamage;this.slow=slow;
   this.source=source;this.energyGain=energyGain;this.push=push;this.pushDirection=pushDirection;Owner=source?source.transform:null;
   transform.position=at;this.radius=radius;this.delay=delay;this.damage=damage;this.victim=victim;this.color=color;this.orbital=orbital;born=Time.time;
   outline=CombatFx.Ring("Impact boundary",at+Vector3.up*.08f,radius,color,.06f);outline.transform.SetParent(transform);
   progress=CombatFx.Ring("Impact countdown",at+Vector3.up*.09f,.1f,color,.12f);progress.transform.SetParent(transform);
   fill=CombatFx.Disc(at+Vector3.up*.04f,radius,new Color(color.r,color.g,color.b,.18f));fill.transform.SetParent(transform);
  }
  void Update(){float t=(Time.time-born)/delay;CombatFx.SetRing(progress,transform.position+Vector3.up*.09f,Mathf.Lerp(.1f,radius,t));if(t>=1)Detonate();}
  void Detonate(){
   var hit=new HashSet<Health>();foreach(var c in Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Ignore)){
    var h=c.GetComponentInParent<Health>();if(h&&h.team==victim&&hit.Add(h)){
     bool damaged=source?CombatShots.Damage(h,damage,source,energyGain):h.ApplyDamage(damage);
     if(damaged&&dotDuration>0)IonDebuff.Apply(h,source,dotDuration,dotDamage,slow);
     if(damaged&&push>0)CrowdControl.For(h).Knockback(pushDirection.sqrMagnitude>.01f?pushDirection:h.transform.position-transform.position,push);
    }
   }
   CombatFx.Shockwave(transform.position,color,radius);CombatFx.Burst(transform.position+Vector3.up*.2f,color,orbital?32:16,radius);
   if(orbital){CombatFx.Beam(transform.position+Vector3.up*18,transform.position,color,.65f,.32f);Camera.main?.GetComponent<FollowCamera>()?.Shake(.22f);}
   SynthAudio.Play(SoundKind.Impact,transform.position,orbital?.5f:.3f);Destroy(gameObject);
  }
 }
}

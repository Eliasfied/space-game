using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Forward Kick")]
 public sealed class KickAbility:AbilityDefinition {
  public float stunDuration=2,dashDistance=2.1f;
  public override AbilityAimShape AimShape=>AbilityAimShape.Dash;
  public override float AimRadius=>1.15f;
  public override float AimRange=>dashDistance+1.8f;
  public override void Execute(AbilityContext c){c.caster.GetComponent<PlayerMotor>().DashTowards(c.direction,dashDistance);c.caster.GetComponent<MechAnimator>()?.PlayKick();var kick=c.caster.gameObject.AddComponent<ForwardKick>();kick.Setup(c.direction,damage,stunDuration,energyOnHit,c.caster);}
 }
 public sealed class ForwardKick:MonoBehaviour {
  Vector3 direction;float damage,stun,gain,end;AbilityCaster source;readonly HashSet<Health> hit=new HashSet<Health>();
  public void Setup(Vector3 direction,float damage,float stun,float gain,AbilityCaster source){this.direction=direction;this.damage=damage;this.stun=stun;this.gain=gain;this.source=source;end=Time.time+.24f;CombatFx.Beam(transform.position+Vector3.up*.65f,transform.position+Vector3.up*.65f+direction*2,new Color(1,.7f,.2f),.2f,.2f);}
  void Update(){
   if(!source||!source.GetComponent<Health>().Alive||Time.time>end){Destroy(this);return;}
   Vector3 center=transform.position+Vector3.up*.9f;
   foreach(var col in Physics.OverlapSphere(center+direction*.65f,1.15f,~0,QueryTriggerInteraction.Ignore)){
    var h=col.GetComponentInParent<Health>();if(!h||h.team!=Team.Hostile||hit.Contains(h))continue;
    Vector3 delta=col.bounds.center-center;delta.y=0;if(Vector3.Dot(direction,delta.normalized)<.25f)continue;
    Vector3 closest=col.ClosestPoint(center);
    if(Physics.Linecast(center,closest,out RaycastHit obstruction,~(1<<8),QueryTriggerInteraction.Ignore)&&obstruction.collider.GetComponentInParent<Health>()!=h)continue;
    if(CombatShots.Damage(h,damage,source,gain)){hit.Add(h);CrowdControl.For(h).Stun(stun);CombatFx.Burst(closest,new Color(1,.8f,.25f),10,.7f);}
   }
  }
 }
}

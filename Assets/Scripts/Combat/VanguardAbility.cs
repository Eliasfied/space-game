using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 public enum VanguardSkill {PulseBurst,OverchargeRailgun,GravitonDash,AegisSupplyDrone,ConcussiveBlast,TractorBeam,TargetingMatrix,OrbitalKineticStrike}
 [CreateAssetMenu(menuName="Asterion/Abilities/Vanguard")]
 public sealed class VanguardAbility:AbilityDefinition {
  public VanguardSkill skill;
  public override bool RequiresTarget=>skill==VanguardSkill.PulseBurst||skill==VanguardSkill.OverchargeRailgun||skill==VanguardSkill.ConcussiveBlast||skill==VanguardSkill.TargetingMatrix||skill==VanguardSkill.OrbitalKineticStrike;
  public bool IsMovement=>skill==VanguardSkill.GravitonDash;
  public bool IsWeapon=>skill==VanguardSkill.PulseBurst||skill==VanguardSkill.OverchargeRailgun||skill==VanguardSkill.ConcussiveBlast;
  public static Health Partner(AbilityCaster source,float range,bool nearest){
   var own=source.GetComponent<Health>();var camera=Camera.main;var input=source.GetComponent<PlayerInputReader>();
   if(camera&&input){foreach(var hit in Physics.RaycastAll(camera.ScreenPointToRay(input.Pointer),100,1<<8,QueryTriggerInteraction.Ignore)){
    var h=hit.collider.GetComponentInParent<Health>();if(h&&h!=own&&h.Alive&&h.team==Team.Player&&Vector3.Distance(source.transform.position,h.transform.position)<=range)return h;
   }}
   if(!nearest)return null;
   Health partner=null;float distance=range;
   foreach(var h in Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude))if(h!=own&&h.Alive&&h.team==Team.Player){float d=Vector3.Distance(source.transform.position,h.transform.position);if(d<=distance){partner=h;distance=d;}}
   return partner;
  }
  public override void Execute(AbilityContext c){
   var caster=c.caster;Vector3 origin=caster.ShotOrigin;var cyan=CombatFx.Cyan;
   switch(skill){
    case VanguardSkill.PulseBurst:caster.FireBurst(c,damage,range,energyOnHit,cyan,3,.10f,true);break;
    case VanguardSkill.OverchargeRailgun:
     Rail(c);break;
    case VanguardSkill.GravitonDash:
     VanguardZone.Create(caster.transform.position,3,4,false,caster);
     caster.GetComponent<PlayerMotor>().DashTowards(caster.transform.forward,5.5f,true);CombatFx.Shockwave(caster.transform.position,cyan,2);SynthAudio.Play(SoundKind.Dash,origin,.35f);break;
    case VanguardSkill.AegisSupplyDrone:
     var recipient=Partner(caster,range,false);if(!recipient)recipient=caster.GetComponent<Health>();
     recipient.AddShield(recipient.maximum*.3f,6);VanguardStatus.For(recipient).Immune(4);SupplyDroneVisual.Create(recipient);break;
    case VanguardSkill.ConcussiveBlast:
     if(c.target&&CombatShots.Damage(c.target,damage,caster,0,true)){
      var control=CrowdControl.For(c.target);control.Interrupt();control.Knockback(c.direction,5,2);
      CombatFx.MuzzleBeam(caster.ShotMuzzle,origin,PlayerTargeting.Center(c.target),new Color(1,.65f,.2f),.38f,.2f);CombatFx.Shockwave(c.target.transform.position,CombatFx.Amber,1.5f);
     }break;
    case VanguardSkill.TractorBeam:
     var ally=Partner(caster,range,true);if(!ally)break;
     var old=ally.transform.position;var motor=ally.GetComponent<PlayerMotor>();
     if(motor){motor.PullTo(caster.transform.position);CombatFx.Beam(old+Vector3.up,ally.transform.position+Vector3.up,cyan,.18f,.3f);CombatFx.Burst(ally.transform.position,cyan,18,1);SynthAudio.Play(SoundKind.Dash,old,.35f);}break;
    case VanguardSkill.TargetingMatrix:
     if(c.target){VanguardStatus.For(c.target).Mark(8);CombatFx.Beam(origin,PlayerTargeting.Center(c.target),cyan,.1f,.25f);CombatFx.Shockwave(c.target.transform.position,cyan,2);}break;
    case VanguardSkill.OrbitalKineticStrike:VanguardOrbital.Create(c.point,caster,damage);break;
   }
  }
  void Rail(AbilityContext c){
   var origin=c.caster.ShotOrigin;var direction=(PlayerTargeting.Center(c.target)-origin).normalized;float length=range;
   if(Physics.Raycast(origin,direction,out var wall,range,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))length=wall.distance;
   var hits=new HashSet<Health>();
   foreach(var collider in Physics.OverlapCapsule(origin,origin+direction*length,.45f,1<<9,QueryTriggerInteraction.Ignore)){
    var h=collider.GetComponentInParent<Health>();if(!h||!hits.Add(h))continue;
    if(Physics.Linecast(origin,PlayerTargeting.Center(h),~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))continue;
    if(CombatShots.Damage(h,damage,c.caster,0,true))VanguardStatus.For(h).Expose(6);
   }
   CombatFx.PistolFlash(c.caster.ShotMuzzle,origin,direction,CombatFx.Cyan);
   CombatFx.MuzzleBeam(c.caster.ShotMuzzle,origin,origin+direction*length,CombatFx.Cyan,.5f,.24f);
   CombatFx.Beam(origin,origin+direction*length,Color.white,.12f,.16f);CombatFx.Burst(origin+direction*length,CombatFx.Cyan,18,1.2f);SynthAudio.Play(SoundKind.Laser,origin,.45f);
  }
 }
}

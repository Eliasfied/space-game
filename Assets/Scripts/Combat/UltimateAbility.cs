using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Ultimate")]
 public sealed class UltimateAbility:AbilityDefinition {
  public float coneAngle=80,buffDuration=15;
  public override AbilityAimShape AimShape=>AbilityAimShape.Cone;
  public override float AimAngle=>coneAngle;
  public override void OnStarted(AbilityContext c){
   var buff=c.caster.GetComponent<HunterOverdrive>();if(!buff)buff=c.caster.gameObject.AddComponent<HunterOverdrive>();buff.Activate(buffDuration);
  }
  // One cone hit per enemy and tick, independent of the bullets from both weapons.
  public override void Execute(AbilityContext c){
   var origin=c.caster.transform.position;var hit=new HashSet<Health>();
   foreach(var col in Physics.OverlapSphere(origin,range,~0,QueryTriggerInteraction.Ignore)){
    var target=col.GetComponentInParent<Health>();if(!target||!target.Alive||target.team!=Team.Hostile||!hit.Add(target))continue;
    if(SkillshotGeometry.InCone(origin,c.direction,target.transform.position,range,coneAngle)&&
     !Physics.Linecast(origin+Vector3.up*1.3f,PlayerTargeting.Center(target),~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))CombatShots.Damage(target,damage,c.caster,0);
   }
   Color color=new Color(1,.65f,.19f);
   for(int gun=0;gun<2;gun++){
    Transform socket=c.caster.ShotMuzzle;Vector3 start=c.caster.ShotOrigin;c.caster.AdvanceMuzzle();CombatFx.PistolFlash(socket,start,c.direction,color);
    for(int ray=0;ray<5;ray++){
     float angle=Mathf.Lerp(-coneAngle*.5f,coneAngle*.5f,(ray+Random.value)/5f);
     Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*c.direction,end=start+direction*range;
     if(Physics.Linecast(start,end,out RaycastHit wall,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))end=wall.point;
     CombatFx.Beam(start,end,color,.045f,.12f);
    }
   }
   SynthAudio.Play(SoundKind.Pistol,origin,.3f);Camera.main?.GetComponent<FollowCamera>()?.Shake(.035f);
  }
 }
}

using UnityEngine;
namespace AsterionGame {
 public static class CombatShots {
  public static bool Damage(Health target,float amount,AbilityCaster source,float energyGain,bool canCrit=false){
   if(!target||target.team!=Team.Hostile)return false;
   var mark=target.GetComponent<VanguardStatus>();var buff=source?source.GetComponent<VanguardStatus>():null;
   if(buff&&buff.BoostRemaining>0)amount*=1.3f;
   bool critical=(canCrit||(mark&&mark.MatrixRemaining>0))&&Random.value<.2f;
   if(critical)amount*=1.5f;
   float before=target.Current;if(!target.ApplyDamage(amount))return false;
   if(critical&&mark)mark.CriticalHit();
   if(source)source.Statistics.Record(Mathf.Max(0,before-target.Current));
   if(source){var energy=source.GetComponent<Energy>();if(energy)energy.Gain(energyGain);}return true;
  }
  public static void Fire(AbilityContext c,float damage,float range,Color color,float width,float gain){
   if(c.target&&c.target.Alive){
    Vector3 start=c.caster.ShotOrigin,aimEnd=PlayerTargeting.Center(c.target);Transform socket=c.caster.ShotMuzzle;c.caster.AdvanceMuzzle();
    if(Physics.Linecast(c.caster.transform.position+Vector3.up*1.3f,aimEnd,out RaycastHit wall,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))aimEnd=wall.point;
    else Damage(c.target,damage,c.caster,gain);
    CombatFx.MuzzleBeam(socket,start,aimEnd,color,width,.1f);CombatFx.Burst(aimEnd,color,5,.3f);SynthAudio.Play(SoundKind.Laser,c.caster.transform.position,.18f);return;
   }
   Transform muzzle=c.caster.ShotMuzzle;Vector3 origin=c.caster.ShotOrigin;c.caster.AdvanceMuzzle();Vector3 direction=c.direction.normalized;
   Vector3 chest=c.caster.transform.position+Vector3.up*1.45f,toMuzzle=origin-chest,end=origin+direction*range;
   if(Physics.Raycast(chest,toMuzzle.normalized,out RaycastHit blocked,toMuzzle.magnitude,~(1<<8),QueryTriggerInteraction.Ignore)){
    Damage(blocked.collider.GetComponentInParent<Health>(),damage,c.caster,gain);CombatFx.Beam(chest,blocked.point,color,width,.12f);CombatFx.Burst(blocked.point,color,5,.35f);return;
   }
   if(Physics.Raycast(origin,direction,out RaycastHit hit,range,~(1<<8),QueryTriggerInteraction.Ignore)){end=hit.point;Damage(hit.collider.GetComponentInParent<Health>(),damage,c.caster,gain);CombatFx.Burst(end,color,6,.55f);}
   CombatFx.MuzzleBeam(muzzle,origin,end,color,width,.10f);SynthAudio.Play(SoundKind.Laser,c.caster.transform.position,.18f);
  }
 }
}

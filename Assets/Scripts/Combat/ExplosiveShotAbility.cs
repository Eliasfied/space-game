using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Explosive Shot")]
 public sealed class ExplosiveShotAbility:AbilityDefinition {
  public float radius=2.8f,projectileSpeed=26;
  public override bool RequiresTarget=>true;
  public override void Execute(AbilityContext c){
   if(!c.target||!c.target.Alive)return;
   Vector3 origin=c.caster.ShotOrigin,end=PlayerTargeting.Center(c.target);
   var shot=new GameObject("Explosive round").AddComponent<ExplosiveRound>();shot.Setup(c.caster,this,origin,c.target);
   CombatFx.PistolFlash(c.caster.ShotMuzzle,origin,(end-origin).normalized,new Color(1,.55f,.14f));c.caster.AdvanceMuzzle();
   SynthAudio.Play(SoundKind.Pistol,origin,.32f);
  }
 }
 public sealed class ExplosiveRound:MonoBehaviour {
  AbilityCaster source;ExplosiveShotAbility ability;Health target;float remaining;LineRenderer trail;bool exploded;static readonly Color Tint=new Color(1,.55f,.14f);
  public void Setup(AbilityCaster caster,ExplosiveShotAbility definition,Vector3 start,Health target){
   source=caster;ability=definition;transform.position=start;this.target=target;remaining=definition.range;
   trail=CombatFx.Line("Explosive tracer",Tint*2,.16f);trail.positionCount=2;trail.transform.SetParent(transform);trail.SetPosition(0,start);trail.SetPosition(1,start);
   Vector3 chest=caster.transform.position+Vector3.up*1.3f;
   if(Physics.Linecast(chest,start,out RaycastHit wall,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)){transform.position=wall.point+wall.normal*.03f;Explode();}
  }
  void Update(){
   if(Time.deltaTime<=0||exploded)return;
   if(!source||!source.GetComponent<Health>().Alive||!target||!target.Alive||!target.gameObject.activeInHierarchy){Destroy(gameObject);return;}
   // Keep following the original cast target, even when the player selects another enemy.
   Vector3 end=PlayerTargeting.Center(target),before=transform.position;
   Vector3 next=Vector3.MoveTowards(before,end,Mathf.Min(remaining,Mathf.Max(1,ability.projectileSpeed)*Time.deltaTime));
   bool blocked=Physics.Linecast(before,next,out RaycastHit hit,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);
   transform.position=blocked?hit.point+hit.normal*.03f:next;trail.SetPosition(0,transform.position);trail.SetPosition(1,Vector3.MoveTowards(transform.position,before,.8f));
   remaining-=Vector3.Distance(before,next);
   if(blocked)Explode();
   else if((next-end).sqrMagnitude<.0001f)Explode(target);
   else if(remaining<=0)Destroy(gameObject);
  }
  void Explode(Health directTarget=null){
   if(exploded)return;exploded=true;Vector3 at=transform.position;var seen=new HashSet<Health>();
   // The selected enemy is hit exactly once, even with several colliders or none.
   if(directTarget){seen.Add(directTarget);CombatShots.Damage(directTarget,ability.damage,source,0);}
   foreach(var col in Physics.OverlapSphere(at,ability.radius,~0,QueryTriggerInteraction.Ignore)){
    var target=col.GetComponentInParent<Health>();if(!target||!target.Alive||target.team!=Team.Hostile||!seen.Add(target))continue;
    if(!Physics.Linecast(at,PlayerTargeting.Center(target),~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))CombatShots.Damage(target,ability.damage,source,0);
   }
   Vector3 floor=at;floor.y=.08f;CombatFx.Shockwave(floor,Tint,ability.radius);CombatFx.Burst(at,Tint,18,ability.radius*.65f);SynthAudio.Play(SoundKind.Impact,at,.35f);Destroy(gameObject);
  }
 }
}

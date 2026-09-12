using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class AddDrone:MonoBehaviour {
  public enum Kind { Rusher,Gunner,Mortar,Repair }
  BossBrain boss;Health health,target;CrowdControl control;Kind kind;Transform visual;
  EnemyVisualAnimator rig;Health repairTarget;LineRenderer repairBeam;float nextRepair;
  float nextAttack,fireAt,age;Vector3 lockedAim;bool winding;LineRenderer warning;GroundHazard mortar;
  public void Initialize(BossBrain owner,Kind role,Transform chassis){
   boss=owner;kind=role;visual=chassis;health=GetComponent<Health>();target=boss.player.GetComponent<Health>();control=CrowdControl.For(health);
   rig=visual?visual.GetComponentInChildren<EnemyVisualAnimator>():null;repairTarget=boss.GetComponent<Health>();
   health.Died+=Die;control.Stunned+=CancelAttack;nextAttack=Time.time+Random.Range(1f,2f);
  }
  void Update(){
   if(!boss||!target||!target.Alive||!boss.GetComponent<Health>().Alive){Destroy(gameObject);return;}
   if(!health.Alive||Time.timeScale==0)return;
   age+=Time.deltaTime;if(visual&&!rig)visual.localPosition=Vector3.up*Mathf.Sin(age*3)*.06f;
   if(control.IsStunned||control.IsKnockedBack){CancelAttack();return;}
   if(kind==Kind.Repair){Repair();return;}
   Vector3 toward=target.transform.position-transform.position;toward.y=0;float distance=toward.magnitude;
   if(winding){if(Time.time>=fireAt)Fire();return;}
   if(toward.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(toward),Time.deltaTime*7);
   float range=kind==Kind.Rusher?1.6f:kind==Kind.Gunner?6.5f:9;
   bool sight=!Physics.Linecast(transform.position+Vector3.up,target.transform.position+Vector3.up,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);
   if(distance>range||!sight)Move(toward.normalized*(kind==Kind.Rusher?3.6f:2.4f));
   else if(kind!=Kind.Rusher&&distance<3)Move(-toward.normalized*2);
   if(distance<=range+.2f&&sight&&Time.time>=nextAttack)Windup();
  }
  void Repair(){
   Vector3 toward=repairTarget.transform.position-transform.position;toward.y=0;float distance=toward.magnitude;
   if(toward.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(toward),Time.deltaTime*5);
   bool sight=!Physics.Linecast(transform.position+Vector3.up*1.3f,PlayerTargeting.Center(repairTarget),~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);
   if(distance>5.5f||!sight)Move(toward.normalized*2.5f);else if(distance<3)Move(-toward.normalized*2);
   bool repairing=distance<=6&&sight&&repairTarget.Current<repairTarget.maximum&&Time.time>=nextAttack;
   if(!repairing){StopRepair();return;}
   if(!repairBeam){repairBeam=CombatFx.Line("Repair tether",new Color(.2f,1,.55f),.065f);repairBeam.positionCount=2;}
   repairBeam.SetPosition(0,transform.position+Vector3.up*1.25f+transform.forward*.35f);
   repairBeam.SetPosition(1,PlayerTargeting.Center(repairTarget));repairBeam.widthMultiplier=.06f+Mathf.Sin(Time.time*12)*.015f;
   if(Time.time>=nextRepair){nextRepair=Time.time+.5f;repairTarget.Heal(6);}
  }
  void StopRepair(){if(repairBeam)Destroy(repairBeam.gameObject);repairBeam=null;}
  void Move(Vector3 velocity){
   velocity*=control.MovementMultiplier;float step=velocity.magnitude*Time.deltaTime;if(step<=0)return;Vector3 direction=velocity.normalized;
   if(!Free(direction,step,out RaycastHit blocker)){
    Vector3 tangent=Vector3.ProjectOnPlane(direction,blocker.normal);tangent.y=0;
    if(tangent.sqrMagnitude<.05f)tangent=Vector3.Cross(Vector3.up,direction);
    direction=tangent.normalized;if(!Free(direction,step,out _))return;
   }
   Vector3 next=transform.position+direction*step;Vector2 flat=Vector2.ClampMagnitude(new Vector2(next.x,next.z),12.3f);next.x=flat.x;next.z=flat.y;transform.position=next;
  }
  bool Free(Vector3 direction,float step,out RaycastHit blocker){
   blocker=default;float closest=float.PositiveInfinity;
   foreach(var hit in Physics.CapsuleCastAll(transform.position+Vector3.up*.48f,transform.position+Vector3.up*1.4f,.4f,direction,step+.06f,~0,QueryTriggerInteraction.Ignore)){
    if(hit.collider.transform.IsChildOf(transform))continue;if(hit.distance<closest){closest=hit.distance;blocker=hit;}
   }return float.IsPositiveInfinity(closest);
  }
  void Windup(){
   if(rig)rig.Action("Strike",.85f);
   lockedAim=target.transform.position;lockedAim.y=.06f;winding=true;fireAt=Time.time+(kind==Kind.Mortar?1.1f:.65f);
   if(kind==Kind.Mortar){mortar=new GameObject("Drone mortar warning").AddComponent<GroundHazard>();mortar.Setup(lockedAim,1.35f,1.1f,12,Team.Player,new Color(.8f,.25f,1));mortar.Owner=transform;}
   else if(kind==Kind.Rusher){warning=CombatFx.Ring("Drone melee windup",transform.position+transform.forward*.8f+Vector3.up*.06f,1.05f,CombatFx.Amber,.055f);}
   else {warning=CombatFx.Line("Drone shot warning",new Color(1,.55f,.1f),.025f);warning.positionCount=2;warning.SetPosition(0,transform.position+Vector3.up);warning.SetPosition(1,lockedAim+Vector3.up);}
  }
  void Fire(){
   winding=false;nextAttack=Time.time+(kind==Kind.Rusher?1.6f:kind==Kind.Gunner?2.8f:4.5f);if(warning)Destroy(warning.gameObject);
   if(kind==Kind.Rusher){
    Vector3 delta=target.transform.position-transform.position;delta.y=0;
    if(delta.magnitude<2&&Vector3.Dot(transform.forward,delta.normalized)>.35f)target.ApplyDamage(8);
    CombatFx.Shockwave(transform.position+transform.forward*.8f,CombatFx.Amber,1.05f);
   }else if(kind==Kind.Gunner){
    Vector3 direction=lockedAim-transform.position;direction.y=0;direction.Normalize();
    for(int i=-1;i<=1;i+=2){var d=Quaternion.Euler(0,i*7,0)*direction;CombatFx.Projectile(transform.position+Vector3.up+transform.right*(i*.35f)+d*.9f,d,8,7);}
   }
  }
  void CancelAttack(){StopRepair();if(rig)rig.CancelAction();winding=false;nextAttack=Mathf.Max(nextAttack,Time.time+.6f);if(warning)Destroy(warning.gameObject);if(mortar)Destroy(mortar.gameObject);}
  void Die(){CancelAttack();CombatFx.Burst(transform.position+Vector3.up,CombatFx.Amber,10,.8f);Destroy(gameObject);}
  void OnDestroy(){CancelAttack();if(health)health.Died-=Die;if(control)control.Stunned-=CancelAttack;}
 }
}

using System.Collections;
using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class BossBrain:MonoBehaviour {
  public Transform player,visual;public bool Engaged {get;private set;}public string AttackName {get;private set;}="STANDBY";
  public float AttackProgress {get;private set;}public int Phase=>health&&health.Current<=health.maximum*.5f?2:1;
  public int AttackCount{get;private set;}
  [Header("Approach and melee")]
  public float aggroRange=8.5f,moveSpeed=3.2f,meleeRange=3.1f,meleeDamage=14;
  public bool IsCasting=>casting&&isActiveAndEnabled&&health&&health.Alive&&playerHealth&&playerHealth.Alive&&(!control||!control.IsStunned&&!control.IsKnockedBack);
  public string CastName{get;private set;}="";
  public float CastDuration{get;private set;}
  public float CastRemaining=>IsCasting?Mathf.Max(0,castEndsAt-Time.time):0;
  public float CastProgress=>IsCasting&&CastDuration>0?Mathf.Clamp01(1-CastRemaining/CastDuration):0;
  bool casting,pursuing,meleeWinding;float castEndsAt,nextMelee;
  const float MeleeHalfAngle=65;
  LineRenderer meleeWarning;BossNavigation navigation;
  CrowdControl control;bool resumeAfterStun;
  EnemyVisualAnimator rig;
  Health health;Health playerHealth;float phase;
  void Awake(){if(!GetComponent<BossReinforcements>())gameObject.AddComponent<BossReinforcements>();health=GetComponent<Health>();health.Damaged+=OnHit;health.Died+=OnDeath;control=CrowdControl.For(health);control.Stunned+=InterruptAttack;}
  void Start(){EnemyVisuals.ReplaceBoss(this);rig=visual?visual.GetComponentInChildren<EnemyVisualAnimator>():null;playerHealth=player.GetComponent<Health>();navigation=new BossNavigation(transform,GetComponent<CapsuleCollider>());StartCoroutine(Encounter());}
  bool CanEngage=>!AegisLevel.Instance||AegisLevel.Instance.BossUnlocked;
  void OnHit(float amount){if(amount>0&&CanEngage)Engaged=true;}
  bool CanAct=>health&&health.Alive&&playerHealth&&playerHealth.Alive&&!control.IsStunned&&!control.IsKnockedBack;
  bool HasSight()=>!Physics.Linecast(transform.position+Vector3.up*1.2f,player.position+Vector3.up,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);
  void Update(){if(!health.Alive||!playerHealth||!playerHealth.Alive){StopAllCoroutines();ClearActions();return;}
   if(control.IsStunned||control.IsKnockedBack){if(!resumeAfterStun)InterruptAttack();AttackName="STUNNED";AttackProgress=0;return;}
   if(resumeAfterStun){resumeAfterStun=false;StartCoroutine(Encounter());}
   if(!Engaged&&CanEngage&&Vector3.Distance(transform.position,player.position)<aggroRange&&HasSight())Engaged=true;
   if(Engaged&&!pursuing&&!meleeWinding){Vector3 d=player.position-transform.position;d.y=0;if(d.sqrMagnitude>.1f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*1.8f);}
   if(visual&&!rig){phase+=Time.deltaTime;visual.localPosition=Vector3.up*(Mathf.Sin(phase*2)*.065f);}
  }
  void ClearActions(){EndCast();pursuing=false;meleeWinding=false;if(meleeWarning){meleeWarning.gameObject.SetActive(false);Destroy(meleeWarning.gameObject);}meleeWarning=null;navigation?.ResetPath();}
  void InterruptAttack(){StopAllCoroutines();ClearActions();if(rig)rig.CancelAction();resumeAfterStun=true;AttackName="STUNNED";AttackProgress=0;foreach(var hazard in FindObjectsByType<GroundHazard>(FindObjectsSortMode.None))if(hazard.Owner==transform)Destroy(hazard.gameObject);}
  GroundHazard Hazard(string name){var h=new GameObject(name).AddComponent<GroundHazard>();return h;}
  IEnumerator Encounter(){
   yield return new WaitUntil(()=>Engaged);yield return new WaitForSeconds(1.2f);
   int pattern=0;while(health.Alive&&playerHealth.Alive){
    if(pattern%3==0)yield return Bombardment();else if(pattern%3==1)yield return PlasmaFan();else yield return Nova();
    pattern++;AttackCount++;yield return PursueAndMelee(Phase==2?3.8f:4.8f);
   }ClearActions();AttackName="STANDBY";
  }
  IEnumerator PursueAndMelee(float seconds){
   pursuing=true;AttackName="PURSUING";AttackProgress=0;navigation?.ResetPath();
   float until=Time.time+seconds;
   try{
    while(Time.time<until&&CanAct){
     Vector3 toward=player.position-transform.position;toward.y=0;bool sight=HasSight();
     if(sight&&toward.magnitude<=meleeRange-.35f&&Time.time>=nextMelee){
      yield return MeleeStrike(toward);
      AttackName="PURSUING";
     }else{
      navigation?.MoveTowards(player.position,moveSpeed*(Phase==2?1.12f:1)*control.MovementMultiplier,sight?meleeRange-.85f:.6f);
      if(sight&&toward.sqrMagnitude>.01f&&toward.magnitude<=meleeRange-.35f)
       transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(toward),1-Mathf.Exp(-8*Time.deltaTime));
      yield return null;
     }
    }
   }finally{pursuing=false;}
  }
  IEnumerator MeleeStrike(Vector3 direction){
   if(direction.sqrMagnitude<.001f)direction=transform.forward;
   direction.Normalize();transform.rotation=Quaternion.LookRotation(direction);
   Vector3 origin=transform.position;
   meleeWinding=true;
   DrawMeleeWarning(origin,direction);
   if(rig)rig.Action("Strike",1.15f);
   try{
    yield return Windup("HAMMER STRIKE",.8f,"Hammerschlag");
    if(!CanAct)yield break;
    Vector3 delta=player.position-origin;delta.y=0;
    if(delta.magnitude<=meleeRange&&(delta.sqrMagnitude<.001f||Vector3.Dot(direction,delta.normalized)>=Mathf.Cos(MeleeHalfAngle*Mathf.Deg2Rad))&&HasSight())
     playerHealth.ApplyDamage(meleeDamage+(Phase==2?4:0));
    Vector3 impact=origin+direction*1.7f;
    CombatFx.Shockwave(impact,CombatFx.Amber,1.15f);CombatFx.Burst(impact+Vector3.up*.15f,CombatFx.Amber,10,.7f);
    SynthAudio.Play(SoundKind.Impact,impact,.4f);nextMelee=Time.time+1.5f;
   }finally{
    meleeWinding=false;EndCast();
    if(meleeWarning){meleeWarning.gameObject.SetActive(false);Destroy(meleeWarning.gameObject);}meleeWarning=null;
   }
   yield return new WaitForSeconds(.35f);
  }
  void DrawMeleeWarning(Vector3 origin,Vector3 direction){
   const int segments=24;
   meleeWarning=CombatFx.Line("Warden / hammer cleave",new Color(1,.5f,.12f),.085f);
   meleeWarning.loop=true;meleeWarning.positionCount=segments+2;
   origin.y+=.065f;meleeWarning.SetPosition(0,origin);
   for(int i=0;i<=segments;i++)meleeWarning.SetPosition(i+1,origin+(Quaternion.Euler(0,Mathf.Lerp(-MeleeHalfAngle,MeleeHalfAngle,i/(float)segments),0)*direction)*meleeRange);
  }
  void BeginCast(string name,float seconds){CastName=name;CastDuration=Mathf.Max(0,seconds);castEndsAt=Time.time+CastDuration;casting=CastDuration>0;}
  void EndCast(){casting=false;}
  IEnumerator Windup(string name,float seconds,string castName){
   AttackName=name;BeginCast(castName,seconds);
   try{while(Time.time<castEndsAt){AttackProgress=CastProgress;yield return null;}AttackProgress=1;}
   finally{EndCast();}
  }
  IEnumerator Bombardment(){
   AttackName="ORBITAL BOMBARDMENT";int count=Phase==2?5:3;if(rig)rig.Action("Channel",count*.52f+1.1f);
   BeginCast("Orbitales Bombardement",count*.52f);
   try{
    for(int i=0;i<count;i++){
     if(!health.Alive||!playerHealth.Alive)yield break;
     Vector3 at=player.position;at.y=.05f;var impact=Hazard("Boss impact");impact.Setup(at,Phase==2?2.5f:2.1f,1.3f,24,Team.Player,CombatFx.Amber);impact.Owner=transform;
     yield return new WaitForSeconds(.52f);
    }
   }finally{EndCast();}
   // Recovery has no cast bar: all bombardment markers have already been placed.
   yield return new WaitForSeconds(1.1f);
  }
  IEnumerator PlasmaFan(){
   if(rig)rig.Action("Strike",1.35f);yield return Windup("PLASMA ARRAY / MOVE",1.1f,"Plasmafächer");int waves=Phase==2?4:3;
   for(int j=0;j<waves;j++){
    Vector3 d=player.position-transform.position;d.y=0;d.Normalize();
    for(int i=-2;i<=2;i++){Vector3 dir=Quaternion.Euler(0,i*14+j%2*5,0)*d;CombatFx.Projectile(transform.position+Vector3.up*.85f+dir*2.55f,dir,Phase==2?9:7.5f,15);}
    if(rig)rig.Action("Strike",.6f);SynthAudio.Play(SoundKind.BossShot,transform.position,.24f);yield return new WaitForSeconds(.6f);
   }
  }
  IEnumerator Nova(){
   if(rig)rig.Action("Slam",2.15f);var nova=Hazard("Reactor discharge");nova.Setup(transform.position+Vector3.up*.04f,6,1.85f,34,Team.Player,CombatFx.Amber);nova.Owner=transform;
   yield return Windup("REACTOR NOVA / GET OUT",1.85f,"Reaktornova");
   int bolts=Phase==2?18:12;for(int i=0;i<bolts;i++){Vector3 d=Quaternion.Euler(0,i*360f/bolts,0)*Vector3.forward;CombatFx.Projectile(transform.position+Vector3.up*.8f+d*2.6f,d,6,12);}yield return new WaitForSeconds(.5f);
  }
  void OnDeath(){StopAllCoroutines();ClearActions();AttackName="CORE OFFLINE";AttackProgress=0;foreach(var h in FindObjectsByType<GroundHazard>(FindObjectsSortMode.None))Destroy(h.gameObject);foreach(var p in FindObjectsByType<HostileProjectile>(FindObjectsSortMode.None))Destroy(p.gameObject);CombatFx.Burst(transform.position+Vector3.up,CombatFx.Amber,60,4);CombatFx.Shockwave(transform.position,CombatFx.Cyan,8);SynthAudio.Play(SoundKind.Impact,transform.position,.7f);Camera.main?.GetComponent<FollowCamera>()?.Shake(.6f);if(visual&&!rig)visual.localRotation=Quaternion.Euler(8,0,12);}
  void OnDisable(){StopAllCoroutines();ClearActions();resumeAfterStun=true;}
  void OnDestroy(){navigation?.Dispose();if(control)control.Stunned-=InterruptAttack;if(health){health.Damaged-=OnHit;health.Died-=OnDeath;}}
 }
}

using System.Collections;
using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class BossBrain:MonoBehaviour {
  public Transform player,visual;public bool Engaged {get;private set;}public string AttackName {get;private set;}="STANDBY";
  public float AttackProgress {get;private set;}public int Phase=>health&&health.Current<=health.maximum*.5f?2:1;
  public int AttackCount{get;private set;}
  CrowdControl control;bool resumeAfterStun;
  EnemyVisualAnimator rig;
  Health health;Health playerHealth;Vector3 home;float phase;
  void Awake(){if(!GetComponent<BossReinforcements>())gameObject.AddComponent<BossReinforcements>();health=GetComponent<Health>();health.Damaged+=OnHit;health.Died+=OnDeath;home=transform.position;control=CrowdControl.For(health);control.Stunned+=InterruptAttack;}
  void Start(){EnemyVisuals.ReplaceBoss(this);rig=visual?visual.GetComponentInChildren<EnemyVisualAnimator>():null;playerHealth=player.GetComponent<Health>();StartCoroutine(Encounter());}
  void OnHit(float amount){Engaged=true;}
  void Update(){if(!health.Alive||!playerHealth||!playerHealth.Alive)return;
   if(control.IsStunned){AttackName="STUNNED";AttackProgress=0;return;}
   if(resumeAfterStun){resumeAfterStun=false;StartCoroutine(Encounter());}
   if(Vector3.Distance(transform.position,player.position)<8.5f)Engaged=true;
   if(Engaged){Vector3 d=player.position-transform.position;d.y=0;if(d.sqrMagnitude>.1f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*1.8f);}
   if(visual&&!rig){phase+=Time.deltaTime;visual.localPosition=Vector3.up*(Mathf.Sin(phase*2)*.065f);}
  }
  void InterruptAttack(){if(rig)rig.CancelAction();StopAllCoroutines();resumeAfterStun=true;AttackName="STUNNED";AttackProgress=0;foreach(var hazard in FindObjectsByType<GroundHazard>(FindObjectsSortMode.None))if(hazard.Owner==transform)Destroy(hazard.gameObject);}
  GroundHazard Hazard(string name){var h=new GameObject(name).AddComponent<GroundHazard>();return h;}
  IEnumerator Encounter(){
   yield return new WaitUntil(()=>Engaged);yield return new WaitForSeconds(1.2f);
   int pattern=0;while(health.Alive&&playerHealth.Alive){
    if(pattern%3==0)yield return Bombardment();else if(pattern%3==1)yield return PlasmaFan();else yield return Nova();
    pattern++;AttackCount++;AttackName="REPOSITIONING";AttackProgress=0;yield return new WaitForSeconds(Phase==2?1.25f:2.1f);
   }AttackName="STANDBY";
  }
  IEnumerator Windup(string name,float seconds){AttackName=name;float end=Time.time+seconds;while(Time.time<end){AttackProgress=1-(end-Time.time)/seconds;yield return null;}AttackProgress=1;}
  IEnumerator Bombardment(){
   AttackName="ORBITAL BOMBARDMENT";int count=Phase==2?5:3;if(rig)rig.Action("Channel",count*.52f+1.1f);
   for(int i=0;i<count;i++){
    if(!health.Alive||!playerHealth.Alive)yield break;
    Vector3 at=player.position;at.y=.05f;var impact=Hazard("Boss impact");impact.Setup(at,Phase==2?2.5f:2.1f,1.3f,24,Team.Player,CombatFx.Amber);impact.Owner=transform;
    yield return new WaitForSeconds(.52f);
   }yield return Windup("BOMBARDMENT / EVADE",1.1f);
  }
  IEnumerator PlasmaFan(){
   if(rig)rig.Action("Strike",1.35f);yield return Windup("PLASMA ARRAY / MOVE",1.1f);int waves=Phase==2?4:3;
   for(int j=0;j<waves;j++){
    Vector3 d=player.position-transform.position;d.y=0;d.Normalize();
    for(int i=-2;i<=2;i++){Vector3 dir=Quaternion.Euler(0,i*14+j%2*5,0)*d;CombatFx.Projectile(transform.position+Vector3.up*.85f+dir*2.55f,dir,Phase==2?9:7.5f,15);}
    if(rig)rig.Action("Strike",.6f);SynthAudio.Play(SoundKind.BossShot,transform.position,.24f);yield return new WaitForSeconds(.6f);
   }
  }
  IEnumerator Nova(){
   if(rig)rig.Action("Slam",2.15f);var nova=Hazard("Reactor discharge");nova.Setup(transform.position+Vector3.up*.04f,6,1.85f,34,Team.Player,CombatFx.Amber);nova.Owner=transform;
   yield return Windup("REACTOR NOVA / GET OUT",1.85f);
   int bolts=Phase==2?18:12;for(int i=0;i<bolts;i++){Vector3 d=Quaternion.Euler(0,i*360f/bolts,0)*Vector3.forward;CombatFx.Projectile(transform.position+Vector3.up*.8f+d*2.6f,d,6,12);}yield return new WaitForSeconds(.5f);
  }
  void OnDeath(){StopAllCoroutines();AttackName="CORE OFFLINE";AttackProgress=0;foreach(var h in FindObjectsByType<GroundHazard>(FindObjectsSortMode.None))Destroy(h.gameObject);foreach(var p in FindObjectsByType<HostileProjectile>(FindObjectsSortMode.None))Destroy(p.gameObject);CombatFx.Burst(transform.position+Vector3.up,CombatFx.Amber,60,4);CombatFx.Shockwave(transform.position,CombatFx.Cyan,8);SynthAudio.Play(SoundKind.Impact,transform.position,.7f);Camera.main?.GetComponent<FollowCamera>()?.Shake(.6f);if(visual&&!rig)visual.localRotation=Quaternion.Euler(8,0,12);}
  void OnDestroy(){if(control)control.Stunned-=InterruptAttack;health.Damaged-=OnHit;health.Died-=OnDeath;}
 }
}

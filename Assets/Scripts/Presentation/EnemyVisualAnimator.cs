using UnityEngine;
namespace AsterionGame {
 // Full-body clips drive only the model; gameplay movement owns the enemy root.
 [DefaultExecutionOrder(90)]
 public sealed class EnemyVisualAnimator:MonoBehaviour {
  public Animator animator;public float slamLength=2,strikeLength=1,channelLength=3;
  Health health;CrowdControl control;Transform bodyAnchor;Vector3 localPosition,localScale,previousPosition;Quaternion localRotation;
  float actionUntil;string state="";bool dead;
  void Awake(){
   if(!animator)animator=GetComponent<Animator>();health=GetComponentInParent<Health>();
   if(health){control=CrowdControl.For(health);health.Died+=Die;control.Stunned+=CancelAction;previousPosition=health.transform.position;}
   localPosition=transform.localPosition;localRotation=transform.localRotation;localScale=transform.localScale;
   if(animator){
    animator.applyRootMotion=false;animator.SetFloat("ActionSpeed",1);
    if(health&&health.GetComponent<BossBrain>()&&animator.isHuman)bodyAnchor=animator.GetBoneTransform(HumanBodyBones.Hips);
   }
  }
  public void Action(string name,float duration){
   if(dead||!animator)return;
   float length=name=="Slam"?slamLength:name=="Channel"?channelLength:strikeLength;
   duration=Mathf.Max(.1f,duration);animator.SetFloat("ActionSpeed",Mathf.Max(.01f,length/duration));
   animator.CrossFadeInFixedTime(name,.08f,0,0);state=name;actionUntil=Time.time+duration;
  }
  public void CancelAction(){actionUntil=0;if(!dead)Play("Idle");}
  void Play(string name){if(!animator||state==name)return;state=name;animator.CrossFadeInFixedTime(name,.12f,0,0);}
  void Update(){
   if(!health||!animator||dead||Time.deltaTime<=0)return;
   float speed=(health.transform.position-previousPosition).magnitude/Mathf.Max(.0001f,Time.deltaTime);previousPosition=health.transform.position;
   if(control&&(control.IsStunned||control.IsKnockedBack)){CancelAction();return;}
   if(Time.time<actionUntil)return;
   Play(speed>.2f?(speed>2.8f?"Run":"Walk"):"Idle");
  }
  void LateUpdate(){
   transform.localPosition=localPosition;transform.localRotation=localRotation;transform.localScale=localScale;
   if(bodyAnchor&&health&&!dead){
    // Meshy clips use different body origins. Keep the evaluated torso over the
    // gameplay capsule, rather than carrying the idle mesh's offset into a run.
    Vector3 offset=health.transform.position-bodyAnchor.position;offset.y=0;
    transform.position+=offset;
   }
  }
  void Die(){if(bodyAnchor)localPosition=transform.localPosition;dead=true;if(animator&&animator.HasState(0,Animator.StringToHash("Base.Death"))){animator.speed=1;animator.CrossFadeInFixedTime("Death",.06f,0,0);}}
  void OnDestroy(){if(health)health.Died-=Die;if(control)control.Stunned-=CancelAction;}
 }
}

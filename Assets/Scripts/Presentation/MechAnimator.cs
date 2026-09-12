using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 public sealed class MechAnimator:MonoBehaviour {
  public PlayerMotor motor;public Transform leftLeg,rightLeg,body,halo;float time,trailAt,lift,kickUntil;
  MeshyCharacterAnimator rig;
  Quaternion leftRest=Quaternion.identity,rightRest=Quaternion.identity;Color accent=CombatFx.Cyan;
  readonly List<Transform> jets=new List<Transform>();readonly List<LineRenderer> flames=new List<LineRenderer>();
  public void PlayKick(){kickUntil=Time.time+.24f;if(rig)rig.PlayKick();}
  void Start(){if(motor)BindVisual(motor.visual?motor.visual:body,false,CombatFx.Cyan);}
  public void BindVisual(Transform visual,bool jetpack,Color color){
   foreach(var f in flames)if(f)Destroy(f.gameObject);flames.Clear();jets.Clear();leftLeg=null;rightLeg=null;body=visual;accent=color;lift=0;
   rig=visual?visual.GetComponentInChildren<MeshyCharacterAnimator>():null;
   if(!visual)return;
   foreach(var t in visual.GetComponentsInChildren<Transform>()){
    if(!rig&&t.name=="LeftLeg")leftLeg=t;if(!rig&&t.name=="RightLeg")rightLeg=t;
    if(jetpack&&(t.name=="JetLeft"||t.name=="JetRight")){
     jets.Add(t);var line=CombatFx.Line("Jet exhaust",color*2,.10f);line.positionCount=2;line.gameObject.SetActive(false);flames.Add(line);
    }
   }
   if(leftLeg)leftRest=leftLeg.localRotation;if(rightLeg)rightRest=rightLeg.localRotation;
  }
  void Update(){
   time+=Time.deltaTime;
   if(halo)halo.Rotate(Vector3.up,Time.deltaTime*24,Space.Self);
   if(!motor)return;
   if(!motor.GetComponent<Health>().Alive){lift=0;if(body)body.localPosition=Vector3.zero;foreach(var flame in flames)if(flame)flame.gameObject.SetActive(false);return;}
   float movement=motor.MoveDirection.magnitude;float stride=Mathf.Sin(time*12)*22*movement;
   lift=Mathf.MoveTowards(lift,motor.IsJetDashing ? .75f : 0,Time.deltaTime*7);
   if(leftLeg)leftLeg.localRotation=leftRest*Quaternion.Euler(motor.IsJetDashing?-20:stride,0,0);
   if(rightLeg)rightLeg.localRotation=rightRest*Quaternion.Euler(Time.time<kickUntil?-75:motor.IsJetDashing?-32:-stride,0,0);
   if(body)body.localPosition=Vector3.up*(lift+(rig?0:Mathf.Abs(Mathf.Sin(time*12))*.045f*movement));
   for(int i=0;i<flames.Count;i++){
    if(!flames[i]||!jets[i])continue;flames[i].gameObject.SetActive(motor.IsJetDashing);
    if(motor.IsJetDashing){flames[i].SetPosition(0,jets[i].position);flames[i].SetPosition(1,jets[i].position-Vector3.up*(.5f+Mathf.Sin(time*65)*.09f)-motor.transform.forward*.22f);}
   }
   if(motor.IsDashing&&Time.time>trailAt){trailAt=Time.time+.035f;CombatFx.Burst(motor.transform.position+Vector3.up*.7f,accent,2,.5f);}
  }
  void OnDestroy(){foreach(var f in flames)if(f)Destroy(f.gameObject);}
 }
}

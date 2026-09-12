using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [DefaultExecutionOrder(-70)]
 public sealed class PlayerTargeting:MonoBehaviour {
  public Health Current{get;private set;}
  readonly List<Health> cycle=new List<Health>();int cycleIndex;
  PlayerInputReader input;Health owner;LineRenderer ring,ringOutline;readonly LineRenderer[] markers=new LineRenderer[4];
  void Awake(){input=GetComponent<PlayerInputReader>();owner=GetComponent<Health>();}
  public static Vector3 Center(Health target){var body=target?target.GetComponent<Collider>():null;return body?body.bounds.center:target?target.transform.position+Vector3.up:Vector3.zero;}
  void Update(){
   if(!owner.Alive){ClearMarker();return;}if(Time.timeScale==0)return;
   if(Current&&(!Current.Alive||!Current.gameObject.activeInHierarchy||Vector3.Distance(transform.position,Current.transform.position)>35))Current=null;
   if(input.TargetPressed)Cycle();
   if(input.SelectPressed&&Camera.main){
    Current=null;cycle.Clear();cycleIndex=0;
    var ray=Camera.main.ScreenPointToRay(input.Pointer);
    if(Physics.Raycast(ray,out RaycastHit hit,150,~(1<<8),QueryTriggerInteraction.Ignore)){
     var h=hit.collider.GetComponentInParent<Health>();if(h&&h.team==Team.Hostile&&h.Alive)Current=h;
    }
   }
   if(Current){
    if(!ring){
     ringOutline=CombatFx.Ring("Target contrast rim",Vector3.zero,1,new Color(.18f,.005f,.01f),.26f);
     ring=CombatFx.Ring("Selected target / red",Vector3.zero,1,new Color(2,.025f,.04f),.15f);
     for(int i=0;i<4;i++){markers[i]=CombatFx.Line("Target bracket",new Color(2,.025f,.04f),.11f);markers[i].positionCount=3;}
    }
    var body=Current.GetComponent<Collider>();float radius=body?Mathf.Max(body.bounds.extents.x,body.bounds.extents.z)+.3f:.9f;
    Vector3 center=new Vector3(Current.transform.position.x,.12f,Current.transform.position.z);
    CombatFx.SetRing(ringOutline,center-Vector3.up*.025f,radius);CombatFx.SetRing(ring,center,radius);
    ring.widthMultiplier=.15f+.025f*(.5f+.5f*Mathf.Sin(Time.time*5));
    for(int i=0;i<4;i++){
     Vector3 d=Quaternion.Euler(0,i*90+45,0)*Vector3.forward,t=Vector3.Cross(Vector3.up,d);
     markers[i].SetPosition(0,center+d*(radius+.38f)-t*.22f);
     markers[i].SetPosition(1,center+d*(radius+.12f));
     markers[i].SetPosition(2,center+d*(radius+.38f)+t*.22f);
    }
   }else ClearMarker();
  }
  bool Available(Health h)=>h&&h.Alive&&h.gameObject.activeInHierarchy&&h.team==Team.Hostile&&(h.transform.position-transform.position).sqrMagnitude<=900;
  void Cycle(){
   // Freeze distance order during a Tab sequence so moving enemies do not cause skips.
   bool fresh=cycleIndex>=cycle.Count;
   if(fresh){
    cycle.Clear();cycleIndex=0;
    foreach(var bar in EnemyHealthBar.All)if(bar&&Available(bar.Health)&&!cycle.Contains(bar.Health))cycle.Add(bar.Health);
    cycle.Sort((a,b)=>{int c=(a.transform.position-transform.position).sqrMagnitude.CompareTo((b.transform.position-transform.position).sqrMagnitude);return c!=0?c:EnemyHealthBar.All.IndexOf(a.GetComponent<EnemyHealthBar>()).CompareTo(EnemyHealthBar.All.IndexOf(b.GetComponent<EnemyHealthBar>()));});
   }
   while(cycleIndex<cycle.Count){var next=cycle[cycleIndex++];if(Available(next)){Current=next;return;}}
   if(cycle.Count>0){cycle.Clear();Cycle();}else Current=null;
  }
  void ClearMarker(){if(ring)Destroy(ring.gameObject);if(ringOutline)Destroy(ringOutline.gameObject);foreach(var marker in markers)if(marker)Destroy(marker.gameObject);}
  void OnDisable(){ClearMarker();}
  void OnDestroy(){ClearMarker();}
 }
}

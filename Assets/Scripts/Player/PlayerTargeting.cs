using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [DefaultExecutionOrder(-70)]
 public sealed class PlayerTargeting:MonoBehaviour {
  public Health Current{get;private set;}
  readonly List<Health> cycle=new List<Health>();int cycleIndex;
  PlayerInputReader input;Health owner;GameObject indicator;
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
  }
  // Selection runs before ability input; the marker follows after enemies have moved.
  void LateUpdate(){
   if(!owner||!owner.Alive||!Current||!Current.Alive||!Current.gameObject.activeInHierarchy){ClearMarker();return;}
   if(!indicator){CreateMarker();if(!indicator)return;}
   var body=Current.GetComponent<Collider>();
   float radius=body?Mathf.Max(body.bounds.extents.x,body.bounds.extents.z)+.38f:1.05f;
   Vector3 center=body?body.bounds.center:Current.transform.position;
   center.y=Current.transform.position.y;
   if(Physics.Raycast(center+Vector3.up,Vector3.down,out var ground,10,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))center.y=ground.point.y;
   center.y+=.06f;
   indicator.transform.position=center;
   indicator.transform.localScale=new Vector3(Mathf.Max(.7f,radius),1,Mathf.Max(.7f,radius));
  }
  void CreateMarker(){
   var shader=Resources.Load<Shader>("TargetSelection");if(!shader)return;
   indicator=new GameObject("Selected target / ground circle");
   var mesh=new Mesh{name="Target selection plane"};
   mesh.vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)};
   mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
   mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
   indicator.AddComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=indicator.AddComponent<MeshRenderer>();
   var material=new Material(shader);material.SetColor("_BaseColor",new Color(1,.06f,.09f,1));
   renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
   var owned=indicator.AddComponent<OwnedResources>();owned.mesh=mesh;owned.material=material;
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
  void ClearMarker(){if(indicator){indicator.SetActive(false);Destroy(indicator);indicator=null;}}
  void OnDisable(){ClearMarker();}
  void OnDestroy(){ClearMarker();}
 }
}

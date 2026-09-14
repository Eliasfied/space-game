using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(BossBrain))]
 public sealed class BossReinforcements:MonoBehaviour {
  BossBrain boss;Health health;readonly List<AddDrone> drones=new List<AddDrone>();readonly List<GameObject> portals=new List<GameObject>();
  Material armor,metal;Material[] lights;float nextWave;int serial;bool spawning;
  void Awake(){boss=GetComponent<BossBrain>();health=GetComponent<Health>();}
  void Start(){
   armor=Material(new Color(.065f,.095f,.13f),false);metal=Material(new Color(.32f,.4f,.46f),false);
   lights=new[]{Material(new Color(1,.18f,.08f),true),Material(new Color(1,.65f,.12f),true),Material(new Color(.7f,.25f,1),true)};
  }
  void Update(){
   drones.RemoveAll(d=>!d);
   if(!health.Alive||!boss.player||!boss.player.GetComponent<Health>().Alive){Clear();return;}
   if(!boss.Engaged||Time.timeScale==0)return;
   if(nextWave==0)nextWave=Time.time+7;
   if(!spawning&&Time.time>=nextWave){nextWave=Time.time+Random.Range(18f,26f);StartCoroutine(Wave());}
  }
  IEnumerator Wave(){
   spawning=true;int count=Mathf.Min(boss.Phase==2?3:2,5-drones.Count);
   for(int i=0;i<count;i++){
    if(!FindSpawn(out Vector3 at))continue;
    var portal=CombatFx.Ring("Reinforcement arrival",at+Vector3.up*.08f,.8f,CombatFx.Amber,.07f);portals.Add(portal.gameObject);
    yield return new WaitForSeconds(1.2f);
    if(!health.Alive||!boss.player||!boss.player.GetComponent<Health>().Alive)break;
    Destroy(portal.gameObject);portals.Remove(portal.gameObject);
    // The player may have moved into the warning during the arrival delay.
    if(!ClearSpot(at))continue;
    var order=new[]{AddDrone.Kind.Rusher,AddDrone.Kind.Repair,AddDrone.Kind.Gunner,AddDrone.Kind.Mortar};
    drones.Add(Create(at,order[serial++%order.Length]));yield return new WaitForSeconds(.35f);
   }
   foreach(var portal in portals)if(portal)Destroy(portal);portals.Clear();spawning=false;
  }
  bool ClearSpot(Vector3 at){return Vector3.Distance(at,boss.player.position)>3&&!Physics.CheckCapsule(at+Vector3.up*.55f,at+Vector3.up*1.55f,.5f,~0,QueryTriggerInteraction.Ignore);}
  bool FindSpawn(out Vector3 at){
   for(int i=0;i<30;i++){
    float angle=Random.Range(0,Mathf.PI*2),radius=Random.Range(AegisLevel.Instance?10:7,AegisLevel.Instance?17:11.5f);at=new Vector3(Mathf.Cos(angle)*radius,.08f,Mathf.Sin(angle)*radius+(AegisLevel.Instance?2:0));
    if(AegisLevel.Instance&&!AegisLevel.Instance.Layout.rooms[0].Contains(at,1))continue;
    if(ClearSpot(at))return true;
   }at=default;return false;
  }
  AddDrone Create(Vector3 position,AddDrone.Kind kind){
   var root=new GameObject(kind.ToString()+" reinforcement");root.transform.position=position;root.layer=9;
   var collider=root.AddComponent<CapsuleCollider>();collider.radius=.43f;collider.height=1.9f;collider.center=Vector3.up*.95f;
   var hp=root.AddComponent<Health>();hp.team=Team.Hostile;hp.maximum=kind==AddDrone.Kind.Rusher?70:kind==AddDrone.Kind.Repair?80:kind==AddDrone.Kind.Gunner?90:110;hp.ResetHealth();
   Transform visual=null;
   if(kind==AddDrone.Kind.Rusher)visual=EnemyVisuals.Spawn("CrimsonSentinel",root.transform);
   if(kind==AddDrone.Kind.Repair)visual=EnemyVisuals.Spawn("RepairDrone",root.transform);
   if(kind==AddDrone.Kind.Rusher){collider.height=2.05f;collider.center=Vector3.up*1.025f;}
   if(kind==AddDrone.Kind.Repair){collider.height=1.7f;collider.radius=.65f;collider.center=Vector3.up*1.2f;}
   if(!visual){
   visual=new GameObject("Drone chassis").transform;visual.SetParent(root.transform,false);var glow=lights[Mathf.Min((int)kind,lights.Length-1)];
   Part(visual,PrimitiveType.Sphere,new Vector3(0,1.1f,0),new Vector3(.85f,.65f,.85f),armor);
   Part(visual,PrimitiveType.Sphere,new Vector3(0,1.18f,.36f),new Vector3(.35f,.2f,.16f),glow);
   Part(visual,PrimitiveType.Cylinder,new Vector3(0,.66f,0),new Vector3(.43f,.13f,.43f),metal);
   Part(visual,PrimitiveType.Sphere,new Vector3(0,.49f,0),new Vector3(.26f,.12f,.26f),glow);
   for(int side=-1;side<=1;side+=2){
    Part(visual,PrimitiveType.Cube,new Vector3(side*.5f,1.07f,0),new Vector3(.26f,.3f,.68f),metal);
    if(kind==AddDrone.Kind.Rusher){var blade=Part(visual,PrimitiveType.Cube,new Vector3(side*.64f,.9f,.38f),new Vector3(.09f,.34f,.65f),glow);blade.localRotation=Quaternion.Euler(0,side*-20,0);}
    else if(kind==AddDrone.Kind.Gunner){Part(visual,PrimitiveType.Cube,new Vector3(side*.47f,1.02f,.57f),new Vector3(.15f,.16f,.6f),armor);Part(visual,PrimitiveType.Sphere,new Vector3(side*.47f,1.02f,.89f),Vector3.one*.15f,glow);}
    else {Part(visual,PrimitiveType.Cylinder,new Vector3(side*.34f,1.55f,-.13f),new Vector3(.3f,.32f,.3f),armor);Part(visual,PrimitiveType.Sphere,new Vector3(side*.34f,1.88f,-.13f),Vector3.one*.19f,glow);}
   }
   }
   var bar=root.AddComponent<EnemyHealthBar>();bar.displayName=kind==AddDrone.Kind.Rusher?"CRIMSON SENTINEL":kind==AddDrone.Kind.Repair?"REPAIR-DROHNE":kind==AddDrone.Kind.Gunner?"PLASMASCHÜTZE":"MÖRSERDROHNE";bar.verticalOffset=.15f;
   root.AddComponent<DamageFeedback>();var brain=root.AddComponent<AddDrone>();brain.Initialize(boss,kind,visual);CombatFx.Shockwave(position,CombatFx.Amber,1);return brain;
  }
  static Transform Part(Transform parent,PrimitiveType shape,Vector3 position,Vector3 scale,Material material){
   var go=GameObject.CreatePrimitive(shape);go.layer=9;var collider=go.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);
   go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
  }
  static Material Material(Color color,bool glow){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.65f);m.SetFloat("_Smoothness",.45f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2);}return m;}
  void Clear(){StopAllCoroutines();spawning=false;foreach(var drone in drones)if(drone)Destroy(drone.gameObject);drones.Clear();foreach(var portal in portals)if(portal)Destroy(portal);portals.Clear();}
  void OnDisable(){Clear();}
  void OnDestroy(){Clear();if(armor)Destroy(armor);if(metal)Destroy(metal);if(lights!=null)foreach(var m in lights)if(m)Destroy(m);}
 }
}

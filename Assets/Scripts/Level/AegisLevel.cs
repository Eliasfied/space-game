using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsterionGame {
 [Serializable] public struct AegisMapRoom {
  public float x,z,width,depth;
  public Rect Rect=>new Rect(x,z,width,depth);
  public bool Contains(Vector3 point,float inset=0)=>point.x>=x+inset&&point.x<=x+width-inset&&point.z>=z+inset&&point.z<=z+depth-inset;
 }
 [Serializable] public sealed class AegisLevelLayout {
  public AegisMapRoom[] rooms,covers;
  public Vector3[] guards;
  public Vector3 playerSpawn,bossSpawn;
  public float gateZ;
 }
 [DefaultExecutionOrder(-150)]
 public sealed class AegisLevel:MonoBehaviour {
  public static AegisLevel Instance{get;private set;}
  public Transform staticArchitecture,gateLeft,gateRight;
  public AegisLevelLayout Layout{get;private set;}
  public bool BossUnlocked{get;private set;}
  public bool GateOpen=>gateTravel>=1;
  public int GuardsRemaining {get{int count=0;foreach(var guard in guards)if(guard&&guard.Alive)count++;return count;}}
  readonly List<Health> guards=new List<Health>();
  GameSession session;Vector3 leftClosed,rightClosed;float gateTravel;
  void Awake(){
   Instance=this;
   var data=Resources.Load<TextAsset>("Environment/Aegis/LevelLayout");
   if(!data){Debug.LogError("Aegis level layout is missing.");enabled=false;return;}
   Layout=JsonUtility.FromJson<AegisLevelLayout>(data.text);
  }
  void Start(){
   session=GetComponent<GameSession>();
   if(!session||!LayoutExists()){enabled=false;return;}
   session.boss.Invulnerable=true;
   if(gateLeft)leftClosed=gateLeft.localPosition;
   if(gateRight)rightClosed=gateRight.localPosition;
   foreach(var position in Layout.guards)SpawnGuard(position);
   if(staticArchitecture)StaticBatchingUtility.Combine(staticArchitecture.gameObject);
  }
  bool LayoutExists()=>Layout!=null&&Layout.rooms!=null&&Layout.rooms.Length>0&&Layout.guards!=null;
  public void AlertGuardPack(){
   foreach(var guard in guards)if(guard&&guard.Alive){var ai=guard.GetComponent<AddDrone>();if(ai)ai.AlertGuard();}
  }
  void SpawnGuard(Vector3 position){
   var root=new GameObject("Aegis / approach sentinel");root.layer=9;root.transform.position=position;root.transform.rotation=Quaternion.Euler(0,180,0);
   root.transform.SetParent(transform,true);
   var hp=root.AddComponent<Health>();hp.team=Team.Hostile;hp.maximum=85;hp.ResetHealth();
   var body=root.AddComponent<CapsuleCollider>();body.height=2.05f;body.radius=.43f;body.center=Vector3.up*1.025f;
   var visual=EnemyVisuals.Spawn("CrimsonSentinel",root.transform);
   var bar=root.AddComponent<EnemyHealthBar>();bar.displayName="AEGIS-WACHE";bar.verticalOffset=.15f;
   root.AddComponent<DamageFeedback>();
   root.AddComponent<AddDrone>().InitializeGuard(session.player,visual);
   guards.Add(hp);
  }
  void Update(){
   if(!session||session.IsSelecting||session.Ended||Time.timeScale<=0)return;
   if(GuardsRemaining==0){
    gateTravel=Mathf.MoveTowards(gateTravel,1,Time.deltaTime*.65f);
    float offset=Mathf.SmoothStep(0,4.4f,gateTravel);
    if(gateLeft)gateLeft.localPosition=leftClosed+Vector3.left*offset;
    if(gateRight)gateRight.localPosition=rightClosed+Vector3.right*offset;
   }
   if(!BossUnlocked&&GateOpen&&session.player.transform.position.z>Layout.gateZ+1){
    BossUnlocked=true;session.boss.Invulnerable=false;
   }
  }
  public bool InsideFloor(Vector3 position,float inset=0){
   if(!LayoutExists())return false;
   foreach(var room in Layout.rooms)if(room.Contains(position,inset))return true;
   return false;
  }
  public Vector3 ClampToFloor(Vector3 position){
   if(!LayoutExists()||InsideFloor(position))return position;
   Vector3 nearest=position;float closest=float.PositiveInfinity;
   foreach(var room in Layout.rooms){
    Vector3 point=new Vector3(Mathf.Clamp(position.x,room.x+.05f,room.x+room.width-.05f),position.y,Mathf.Clamp(position.z,room.z+.05f,room.z+room.depth-.05f));
    float distance=(point-position).sqrMagnitude;if(distance<closest){nearest=point;closest=distance;}
   }
   return nearest;
  }
  void OnDestroy(){if(Instance==this)Instance=null;}
 }
}

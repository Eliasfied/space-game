using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AsterionGame {
 // Navigation only chooses a route. Swept collision checks own the actual movement.
 public sealed class BossNavigation:System.IDisposable {
  const int GeometryMask=~((1<<8)|(1<<9));
  readonly Transform actor;readonly float radius,height;
  readonly NavMeshPath path=new NavMeshPath();
  NavMeshData data;NavMeshDataInstance instance;
  Vector3[] corners=System.Array.Empty<Vector3>();int corner;float nextRoute;
  Bounds bounds;int agentType;
  public BossNavigation(Transform actor,CapsuleCollider body){
   this.actor=actor;radius=body?body.radius:1.05f;height=body?body.height:4.2f;
   var level=AegisLevel.Instance;
   if(level&&level.Layout!=null){
    var room=level.Layout.rooms[0];bounds=new Bounds(new Vector3(room.x+room.width*.5f,2,room.z+room.depth*.5f),new Vector3(room.width,12,room.depth));
   }else bounds=new Bounds(Vector3.up*2,new Vector3(28,12,28));
   var settings=NavMesh.GetSettingsByID(0);agentType=settings.agentTypeID;
   settings.agentRadius=radius+.12f;settings.agentHeight=height;settings.agentClimb=.25f;
   settings.overrideVoxelSize=true;settings.voxelSize=.17f;
   var sources=new List<NavMeshBuildSource>();
   Physics.SyncTransforms();
   if(level&&level.staticArchitecture)NavMeshBuilder.CollectSources(level.staticArchitecture,GeometryMask,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
   else NavMeshBuilder.CollectSources(bounds,GeometryMask,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
   data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
   if(data)instance=NavMesh.AddNavMeshData(data);
   else Debug.LogWarning("Boss navigation could not build its arena surface.");
  }
  public void ResetPath(){corners=System.Array.Empty<Vector3>();corner=0;nextRoute=0;}
  public void MoveTowards(Vector3 target,float speed,float stopDistance){
   if(!actor||speed<=0||Time.deltaTime<=0)return;
   Vector3 toward=target-actor.position;toward.y=0;
   if(toward.magnitude<=stopDistance)return;
   if(Time.time>=nextRoute){
    nextRoute=Time.time+.4f;corner=1;corners=System.Array.Empty<Vector3>();
    Vector3 goal=target;goal.y=actor.position.y;
    goal.x=Mathf.Clamp(goal.x,bounds.min.x+radius+.2f,bounds.max.x-radius-.2f);
    goal.z=Mathf.Clamp(goal.z,bounds.min.z+radius+.2f,bounds.max.z-radius-.2f);
    var filter=new NavMeshQueryFilter{agentTypeID=agentType,areaMask=NavMesh.AllAreas};
    if(data&&NavMesh.SamplePosition(actor.position,out var start,2,filter)&&NavMesh.SamplePosition(goal,out var end,2,filter)&&
       NavMesh.CalculatePath(start.position,end.position,filter,path)&&path.status!=NavMeshPathStatus.PathInvalid)corners=path.corners;
   }
   Vector3 destination=target;
   if(corners.Length>1){
    while(corner<corners.Length-1&&FlatDistance(actor.position,corners[corner])<.2f)corner++;
    destination=corners[corner];
   }
   Vector3 direction=destination-actor.position;direction.y=0;
   float distance=direction.magnitude;if(distance<.025f)return;direction/=distance;
   float step=Mathf.Min(speed*Time.deltaTime,distance,Mathf.Max(0,toward.magnitude-stopDistance));
   Vector3 p1=actor.position+Vector3.up*(radius+.05f),p2=actor.position+Vector3.up*Mathf.Max(radius+.05f,height-radius);
   if(Physics.CapsuleCast(p1,p2,radius,direction,out var hit,step+.06f,GeometryMask,QueryTriggerInteraction.Ignore))step=Mathf.Min(step,Mathf.Max(0,hit.distance-.06f));
   Vector3 next=actor.position+direction*step;
   var level=AegisLevel.Instance;
   if(level&&!level.Layout.rooms[0].Contains(next,radius+.05f))return;
   if(step<=.001f){nextRoute=0;return;}
   actor.position=next;
   actor.rotation=Quaternion.Slerp(actor.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-8*Time.deltaTime));
  }
  static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
  public void Dispose(){if(instance.valid)instance.Remove();if(data)Object.Destroy(data);data=null;}
 }
}

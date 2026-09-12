using UnityEngine;
namespace AsterionGame {
 // Visual and damage use the same swept projectile, not a persistent beam.
 public sealed class BlasterProjectile:MonoBehaviour {
  const float Radius=.12f;
  float speed=34,boltLength=.52f;
  AbilityCaster source;Health target;Vector3 direction;float remaining,damage,energy;Color color;LineRenderer line,core;bool spent;bool vanguardPulse;
  public static void Spawn(AbilityCaster owner,Vector3 origin,Vector3 direction,float range,float damage,float energy,Color color,Health target=null,float speed=34,float length=.52f,float width=.19f,bool vanguardPulse=false){
   var line=CombatFx.Line("Blaster projectile",color*2.7f,width);line.positionCount=2;
   var bolt=line.gameObject.AddComponent<BlasterProjectile>();bolt.source=owner;bolt.vanguardPulse=vanguardPulse;bolt.target=target;bolt.direction=direction;bolt.remaining=range;bolt.damage=damage;bolt.energy=energy;bolt.color=color;bolt.line=line;bolt.transform.position=origin;bolt.speed=Mathf.Max(1,speed);bolt.boltLength=Mathf.Max(.1f,length);
   bolt.core=CombatFx.Line("Bolt white core",new Color(3,2.7f,2),width*(.065f/.19f));bolt.core.positionCount=2;bolt.core.transform.SetParent(bolt.transform);
   var trail=bolt.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=CombatFx.LineMat;trail.startColor=color*1.8f;trail.endColor=color*.3f;trail.startWidth=width*(.12f/.19f);trail.endWidth=0;trail.time=.055f;trail.minVertexDistance=.08f;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   bolt.Draw();
   // Prevent a muzzle that clips through cover from firing through that cover.
   Vector3 chest=owner.transform.position+Vector3.up*1.3f;
   Vector3 toMuzzle=origin-chest;
   if(toMuzzle.sqrMagnitude>.0001f&&Physics.Raycast(chest,toMuzzle.normalized,out RaycastHit hit,toMuzzle.magnitude,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))bolt.Impact(hit.collider,hit.point);
  }
  void Update(){
   if(spent||Time.deltaTime<=0)return;
   if(!source||!source.GetComponent<Health>().Alive||!target||!target.Alive){Destroy(gameObject);return;}
   direction=(PlayerTargeting.Center(target)-transform.position).normalized;
   float step=Mathf.Min(remaining,speed*Time.deltaTime);Vector3 at=transform.position;
   // SphereCast does not report a collider containing its initial sphere.
   foreach(var collider in Physics.OverlapSphere(at,Radius,~(1<<8),QueryTriggerInteraction.Ignore)){
    var h=collider.GetComponentInParent<Health>();if(h&&h!=target)continue;Impact(collider,collider.ClosestPoint(at));return;
   }
   RaycastHit nearest=default;float closest=float.PositiveInfinity;
   foreach(var hit in Physics.SphereCastAll(at,Radius,direction,step,~(1<<8),QueryTriggerInteraction.Ignore)){
    var h=hit.collider.GetComponentInParent<Health>();if(h&&h!=target)continue;
    if(hit.distance<closest){closest=hit.distance;nearest=hit;}
   }
   if(!float.IsPositiveInfinity(closest)){Impact(nearest.collider,nearest.point);return;}
   transform.position+=direction*step;remaining-=step;Draw();if(remaining<=0)Destroy(gameObject);
  }
  void Draw(){line.SetPosition(0,transform.position);line.SetPosition(1,transform.position-direction*boltLength);core.SetPosition(0,transform.position+direction*.035f);core.SetPosition(1,transform.position-direction*(boltLength*(.38f/.52f)));}
  void Impact(Collider collider,Vector3 at){
   if(spent)return;spent=true;
   var health=collider.GetComponentInParent<Health>();if(CombatShots.Damage(health,damage,source,energy,vanguardPulse)&&vanguardPulse&&source)source.RollVanguardProc();
   CombatFx.PistolImpact(at,direction,color);Destroy(gameObject);
  }
 }
}

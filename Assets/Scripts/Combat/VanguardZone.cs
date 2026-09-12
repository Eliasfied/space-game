using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 public sealed class VanguardZone:MonoBehaviour {
  static readonly List<VanguardZone> fields=new List<VanguardZone>();
  float radius,until,tick;bool boost;LineRenderer ring;AbilityCaster owner;
  public static void Create(Vector3 at,float radius,float duration,bool boost,AbilityCaster owner){
   var go=new GameObject(boost?"Kinetic damage zone":"Graviton field");var zone=go.AddComponent<VanguardZone>();at.y=.06f;go.transform.position=at;zone.radius=radius;zone.until=Time.time+duration;zone.boost=boost;zone.owner=owner;
   Color color=boost?CombatFx.Amber:CombatFx.Cyan;zone.ring=CombatFx.Ring(go.name,at+Vector3.up*.03f,radius,color,.13f);zone.ring.transform.SetParent(go.transform);
   var disc=CombatFx.Disc(at,radius,new Color(color.r,color.g,color.b,.13f));disc.transform.SetParent(go.transform);fields.Add(zone);
  }
  public static bool Intercepts(Vector3 start,Vector3 end){
   foreach(var f in fields){if(!f||f.boost||Time.time>=f.until)continue;
    Vector3 d=end-start;float t=d.sqrMagnitude>.00001f?Mathf.Clamp01(Vector3.Dot(f.transform.position-start,d)/d.sqrMagnitude):0;
    Vector3 at=start+t*d;Vector3 flat=at-f.transform.position;flat.y=0;
    if(flat.sqrMagnitude<=f.radius*f.radius&&at.y<3.5f){CombatFx.Burst(at,CombatFx.Cyan,5,.35f);return true;}
   }return false;
  }
  void Update(){
   if(Time.time>=until||!owner||!owner.GetComponent<Health>().Alive){Destroy(gameObject);return;}
   if(Time.time<tick)return;tick=Time.time+.1f;
   foreach(var h in Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude)){
    Vector3 d=h.transform.position-transform.position;d.y=0;if(!h.Alive||d.sqrMagnitude>radius*radius)continue;
    if(boost&&h.team==Team.Player)VanguardStatus.For(h).Boost(.2f);
    if(!boost&&h.team==Team.Hostile)CrowdControl.For(h).Slow(.5f,.25f);
   }
  }
  void OnDestroy(){fields.Remove(this);}
 }
 public sealed class VanguardOrbital:MonoBehaviour {
  AbilityCaster owner;float damage,at;LineRenderer warning;
  public static void Create(Vector3 point,AbilityCaster owner,float damage){var o=new GameObject("Kinetic strike telegraph").AddComponent<VanguardOrbital>();point.y=.07f;o.transform.position=point;o.owner=owner;o.damage=damage;o.at=Time.time+1.2f;o.warning=CombatFx.Ring("Orbital impact",point,4,CombatFx.Amber,.15f);o.warning.transform.SetParent(o.transform);}
  void Update(){
   if(!owner||!owner.GetComponent<Health>().Alive){Destroy(gameObject);return;}if(Time.time<at)return;
   var hit=new HashSet<Health>();foreach(var c in Physics.OverlapSphere(transform.position,4,1<<9,QueryTriggerInteraction.Ignore)){var h=c.GetComponentInParent<Health>();if(!h||!hit.Add(h))continue;h.BreakShield();CombatShots.Damage(h,damage,owner,0,true);}
   CombatFx.Beam(transform.position+Vector3.up*20,transform.position,CombatFx.Amber,1.2f,.4f);CombatFx.Shockwave(transform.position,CombatFx.Amber,4);CombatFx.Burst(transform.position+Vector3.up,CombatFx.Amber,40,4);Camera.main?.GetComponent<FollowCamera>()?.Shake(.35f);SynthAudio.Play(SoundKind.Impact,transform.position,.65f);
   VanguardZone.Create(transform.position,4,6,true,owner);Destroy(gameObject);
  }
 }
 public sealed class SupplyDroneVisual:MonoBehaviour {
  Health target;float until;LineRenderer tether,halo;
  public static void Create(Health target){
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.Destroy(go.GetComponent<Collider>());go.name="Aegis supply drone";go.transform.localScale=new Vector3(.55f,.22f,.55f);go.GetComponent<Renderer>().sharedMaterial=CombatFx.LineMat;
   var d=go.AddComponent<SupplyDroneVisual>();d.target=target;d.until=Time.time+6;d.tether=CombatFx.Line("Supply tether",CombatFx.Cyan,.055f);d.tether.positionCount=2;d.halo=CombatFx.Ring("Aegis shield",target.transform.position,1,CombatFx.Cyan,.09f);CombatFx.Shockwave(target.transform.position,CombatFx.Cyan,1.5f);d.Follow();
  }
  void Follow(){transform.position=PlayerTargeting.Center(target)+Vector3.up*1.5f+Vector3.right*.55f;transform.Rotate(Vector3.up,60*Time.deltaTime);tether.SetPosition(0,transform.position);tether.SetPosition(1,PlayerTargeting.Center(target));CombatFx.SetRing(halo,target.transform.position+Vector3.up*.15f,1);}
  void Update(){if(!target||!target.Alive||Time.time>=until){Destroy(gameObject);return;}Follow();}
  void OnDestroy(){if(tether)Destroy(tether.gameObject);if(halo)Destroy(halo.gameObject);}
 }
}

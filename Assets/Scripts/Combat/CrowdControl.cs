using System;
using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class CrowdControl:MonoBehaviour {
  public float SlowRemaining=>Mathf.Max(0,slowUntil-Time.time);
  public float StunRemaining=>Mathf.Max(0,stunUntil-Time.time);
  float slowUntil,slowFraction;public float MovementMultiplier=>Time.time<slowUntil?1-slowFraction:1;
  public void Slow(float fraction,float duration){if(Immune)return;slowFraction=Mathf.Clamp01(fraction);slowUntil=Time.time+duration;}
  bool Immune{get{var status=GetComponent<VanguardStatus>();return status&&status.ImmunityRemaining>0;}}
  public void Cleanse(){stunUntil=0;slowUntil=0;pushRemaining=0;wallStun=0;}
  public void Interrupt(){if(!Immune)Stunned?.Invoke();}
  float wallStun;
  public bool IsKnockedBack=>pushRemaining>0;
  public event Action Stunned;public bool IsStunned=>Time.time<stunUntil;float stunUntil,pushRemaining;Vector3 pushDirection;Health health;Collider body;LineRenderer ring;
  void Awake(){health=GetComponent<Health>();body=GetComponent<Collider>();}
  public static CrowdControl For(Health health){var cc=health.GetComponent<CrowdControl>();return cc?cc:health.gameObject.AddComponent<CrowdControl>();}
  public void Stun(float seconds){if(!health.Alive||Immune)return;stunUntil=Mathf.Max(stunUntil,Time.time+seconds);Stunned?.Invoke();if(!ring)ring=CombatFx.Ring("Stunned",transform.position,.55f,new Color(1,.85f,.25f),.07f);}
  public void Knockback(Vector3 direction,float distance,float stunOnWall=0){if(!health.Alive||Immune)return;wallStun=stunOnWall;direction.y=0;pushDirection=direction.normalized;pushRemaining=Mathf.Max(0,distance);}
  void Update(){
   if(!health.Alive){if(ring)Destroy(ring.gameObject);return;}
   if(ring){if(!IsStunned)Destroy(ring.gameObject);else CombatFx.SetRing(ring,transform.position+Vector3.up*(body?body.bounds.size.y+.25f:2.4f),.55f);}
   if(pushRemaining<=0||Time.deltaTime<=0)return;
   float step=Mathf.Min(pushRemaining,12*Time.deltaTime),allowed=step,radius=.35f;Vector3 p1=transform.position+Vector3.up*.4f,p2=transform.position+Vector3.up*1.4f;
   if(body){Bounds b=body.bounds;radius=Mathf.Min(b.extents.x,b.extents.z,b.extents.y)*.9f;p1=new Vector3(b.center.x,b.min.y+radius+.04f,b.center.z);p2=new Vector3(b.center.x,Mathf.Max(p1.y,b.max.y-radius),b.center.z);}
   bool hitWall=false;
   foreach(var hit in Physics.CapsuleCastAll(p1,p2,radius,pushDirection,step+.04f,~0,QueryTriggerInteraction.Ignore)){
    if(hit.collider.GetComponentInParent<Health>()==health)continue;float hitDistance=Mathf.Max(0,hit.distance-.04f);if(hitDistance<allowed){allowed=hitDistance;hitWall=!hit.collider.GetComponentInParent<Health>();}
   }
   bool collision=allowed<step-.001f;
   Vector3 at=transform.position+pushDirection*allowed;Vector2 flat=Vector2.ClampMagnitude(new Vector2(at.x,at.z),Mathf.Max(1,13-radius));bool boundary=Mathf.Abs(at.x-flat.x)>.001f||Mathf.Abs(at.z-flat.y)>.001f;collision|=boundary;hitWall|=boundary;at.x=flat.x;at.z=flat.y;transform.position=at;
   pushRemaining=collision?0:pushRemaining-step;if(collision&&hitWall&&wallStun>0){float duration=wallStun;wallStun=0;Stun(duration);}
  }
  void OnDestroy(){if(ring)Destroy(ring.gameObject);}
 }
}

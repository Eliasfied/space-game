using UnityEngine;
namespace AsterionGame {
 public sealed class HostileProjectile:MonoBehaviour {
  Vector3 velocity;float damage,age;Vector3 previous;
  public void Setup(Vector3 position,Vector3 direction,float speed,float damage){transform.position=position;previous=position;velocity=direction.normalized*speed;this.damage=damage;}
  void Update(){
   float step=velocity.magnitude*Time.deltaTime;
   if(VanguardZone.Intercepts(transform.position,transform.position+velocity*Time.deltaTime)){Destroy(gameObject);return;}
   if(Physics.SphereCast(transform.position,.19f,velocity.normalized,out RaycastHit hit,step,~(1<<9),QueryTriggerInteraction.Ignore)){
    var h=hit.collider.GetComponentInParent<Health>();if(h&&h.team==Team.Player)h.ApplyDamage(damage);
    CombatFx.Burst(hit.point,CombatFx.Amber,5,.4f);Destroy(gameObject);return;
   }
   previous=transform.position;transform.position+=velocity*Time.deltaTime;age+=Time.deltaTime;if(age>5)Destroy(gameObject);
  }
 }
}

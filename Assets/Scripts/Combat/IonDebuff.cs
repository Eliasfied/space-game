using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class IonDebuff:MonoBehaviour {
  Health health;AbilityCaster source;float until,nextTick,damage;LineRenderer ring;
  public float Remaining=>Mathf.Max(0,until-Time.time);
  void Awake(){health=GetComponent<Health>();}
  public static void Apply(Health target,AbilityCaster source,float duration,float tickDamage,float slow){
   if(!target.Alive)return;var effect=target.GetComponent<IonDebuff>();if(!effect)effect=target.gameObject.AddComponent<IonDebuff>();
   effect.source=source;effect.until=Time.time+duration;effect.nextTick=Time.time+1;effect.damage=tickDamage;
   CrowdControl.For(target).Slow(slow,duration);
   if(!effect.ring)effect.ring=CombatFx.Ring("Ion debuff",target.transform.position,.6f,new Color(.2f,1,.55f),.045f);
  }
  void Update(){
   if(!health.Alive){Destroy(this);return;}
   while(Time.time>=nextTick&&nextTick<=until){nextTick+=1;CombatShots.Damage(health,damage,source,0);if(!health.Alive)break;}
   if(Time.time>=until){Destroy(this);return;}
   if(ring)CombatFx.SetRing(ring,new Vector3(transform.position.x,.13f,transform.position.z),.6f);
  }
  void OnDestroy(){if(ring)Destroy(ring.gameObject);}
 }
}

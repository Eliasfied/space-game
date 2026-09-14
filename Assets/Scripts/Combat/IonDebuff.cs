using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class IonDebuff:MonoBehaviour {
  Health health;AbilityCaster source;float until,nextTick,damage,energyGain,procChance;LineRenderer ring;
  public float Remaining=>Mathf.Max(0,until-Time.time);
  void Awake(){health=GetComponent<Health>();}
  public static void Apply(Health target,AbilityCaster source,float duration,float tickDamage,float slow,float energyGain=0,float procChance=0){
   if(!target.Alive)return;var effect=target.GetComponent<IonDebuff>();if(!effect)effect=target.gameObject.AddComponent<IonDebuff>();
   if(effect.until<=Time.time)effect.nextTick=Time.time+1;
   effect.source=source;effect.until=Time.time+duration;effect.damage=tickDamage;effect.energyGain=energyGain;effect.procChance=procChance;
   CrowdControl.For(target).Slow(slow,duration);
   if(!effect.ring)effect.ring=CombatFx.Ring("Ion debuff",target.transform.position,.6f,new Color(.2f,1,.55f),.045f);
  }
  void Update(){
   if(!health.Alive){Destroy(this);return;}
   while(Time.time>=nextTick&&nextTick<=until){nextTick+=1;if(CombatShots.Damage(health,damage,source,energyGain)&&source)source.RollHunterChargedProc(procChance);if(!health.Alive)break;}
   if(Time.time>=until){Destroy(this);return;}
   if(ring)CombatFx.SetRing(ring,new Vector3(transform.position.x,.13f,transform.position.z),.6f);
  }
  void OnDestroy(){if(ring)Destroy(ring.gameObject);}
 }
}

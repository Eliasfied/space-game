using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class Energy : MonoBehaviour {
  public float maximum=100,regeneration=18,rechargeDelay=.7f;public bool buildOnHits;
  public float Current {get;private set;}float rechargeAt;Health health;
  void Awake(){health=GetComponent<Health>();ResetEnergy();}
  public void ResetEnergy(){Current=buildOnHits?0:maximum;rechargeAt=0;}
  public void Gain(float amount){if(buildOnHits&&health.Alive)Current=Mathf.Min(maximum,Current+Mathf.Max(0,amount));}
  public bool CanSpend(float amount)=>Current>=Mathf.Max(0,amount);
  public bool Spend(float amount){amount=Mathf.Max(0,amount);if(!CanSpend(amount))return false;Current-=amount;if(amount>0)rechargeAt=Time.time+rechargeDelay;return true;}
  void Update(){if(!buildOnHits && health.Alive && Time.time>=rechargeAt)Current=Mathf.Min(maximum,Current+regeneration*Time.deltaTime);}
 }
}

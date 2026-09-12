using UnityEngine;
namespace AsterionGame {
 // Only the local selection calls this. Later the chosen class ID can come from a lobby.
 public sealed class PlayerLoadout : MonoBehaviour {
  public PlayerClassDefinition Definition {get;private set;}
  public void Apply(PlayerClassDefinition definition){
   if(!definition || !definition.Ready)return;
   var motor=GetComponent<PlayerMotor>();
   if(motor.visual){motor.visual.gameObject.SetActive(false);Destroy(motor.visual.gameObject);}
   var model=Instantiate(definition.model,transform,false);model.name=definition.displayName+" / Visual";model.transform.localScale*=definition.gameplayVisualScale;
   foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=gameObject.layer;
   motor.visual=model.transform;motor.speed=definition.moveSpeed;
   var health=GetComponent<Health>();health.maximum=definition.maximumHealth;health.ResetHealth();
   var energy=GetComponent<Energy>();if(energy){energy.buildOnHits=definition.buildEnergyOnHits;energy.ResetEnergy();}
   GetComponent<AbilityCaster>().Configure(definition.abilities,model.transform);
   GetComponent<MechAnimator>().BindVisual(model.transform,definition.jetpack,definition.accent);
   GetComponent<DamageFeedback>().RefreshRenderers();
   Definition=definition;
  }
 }
}

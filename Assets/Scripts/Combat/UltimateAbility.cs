using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Ultimate")]
 public sealed class UltimateAbility:AbilityDefinition {
  // AbilityCaster executes this once per channel tick; energy is paid only at channel start.
  public override bool RequiresTarget=>true;
  public override void Execute(AbilityContext c){CombatShots.Fire(c,damage,range,new Color(.4f,.8f,1),.48f,0);Camera.main?.GetComponent<FollowCamera>()?.Shake(.035f);}
 }
}

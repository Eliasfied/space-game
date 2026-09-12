using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Dash")]
 public sealed class DashAbility:AbilityDefinition {
  public bool jetAssisted;
  public override void Execute(AbilityContext c){c.caster.GetComponent<PlayerMotor>().Dash(range,jetAssisted);SpawnVfx(c.caster.transform.position);SynthAudio.Play(SoundKind.Dash,c.caster.transform.position,.32f);}
 }
}

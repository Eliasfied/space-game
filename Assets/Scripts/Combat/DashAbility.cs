using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Dash")]
 public sealed class DashAbility:AbilityDefinition {
  public bool jetAssisted;
  [Min(1)] public float travelSpeed=23;
  public override void Execute(AbilityContext c){c.caster.GetComponent<PlayerMotor>().Dash(range,jetAssisted,travelSpeed);SpawnVfx(c.caster.transform.position);SynthAudio.Play(SoundKind.Dash,c.caster.transform.position,.32f);}
 }
}

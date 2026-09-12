using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Orbital")]
 public sealed class OrbitalAbility:AbilityDefinition {
  public float radius=3.2f;public Color effectColor=new Color(.12f,.92f,1);
  public override bool RequiresGroundPoint=>true;
  public override bool RequiresTarget=>true;
  public override void Execute(AbilityContext c){
   var go=new GameObject("Orbital strike");go.AddComponent<GroundHazard>().Setup(c.point,radius,.7f,damage,Team.Hostile,effectColor,true);SpawnVfx(c.point);SynthAudio.Play(SoundKind.Charge,c.point,.25f);
  }
 }
}

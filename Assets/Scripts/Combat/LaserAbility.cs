using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Laser")]
 public sealed class LaserAbility:AbilityDefinition {
  public bool twinProjectiles;
  public Color beamColor=new Color(.12f,.92f,1);
  public override bool RequiresTarget=>true;
  public override void Execute(AbilityContext c){
   if(twinProjectiles){
    c.caster.FireTwin(c,damage,range,energyOnHit,beamColor);
   }else CombatShots.Fire(c,damage,range,beamColor,.07f,energyOnHit);
  }
 }
}

using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Charged Shot")]
 public sealed class ChargedShotAbility:AbilityDefinition {
  [Min(1)] public float projectileSpeed=16;
  [Min(.1f)] public float projectileLength=1.8f;
  [Min(.01f)] public float projectileWidth=.55f;
  public override bool RequiresTarget=>true;
  public override void Execute(AbilityContext c){
   if(!c.target||!c.target.Alive)return;
   Vector3 origin=c.caster.ShotOrigin;
   Vector3 direction=(PlayerTargeting.Center(c.target)-origin).normalized;
   Color color=new Color(1,.75f,.24f);
   CombatFx.PistolFlash(c.caster.ShotMuzzle,origin,direction,color);
   // Damage and energy are awarded on impact by the travelling projectile.
   BlasterProjectile.Spawn(c.caster,origin,direction,range,damage*(c.damageMultiplier>0?c.damageMultiplier:1),energyOnHit,color,c.target,projectileSpeed,projectileLength,projectileWidth);
   c.caster.AdvanceMuzzle();
   CombatFx.Burst(origin,color,12,.6f);
   SynthAudio.Play(SoundKind.Laser,c.caster.transform.position,.24f);
  }
 }
}

using UnityEngine;
namespace AsterionGame {
 public struct AbilityContext {public AbilityCaster caster;public Vector3 point;public Vector3 direction;public Health target;public float damageMultiplier;}
 public abstract class AbilityDefinition:ScriptableObject {
  public string displayName;[TextArea] public string description;public float cooldown,castTime,range,damage;
  [Min(0)] public float energyCost,energyOnHit;
  [Range(0,1)] public float energyFraction;
  public float channelDuration,channelTickInterval=.2f;public bool lockMovement;
  public GameObject vfxPrefab;
  public virtual bool RequiresTarget=>false;
  public virtual bool RequiresGroundPoint=>false;
  public float Cost(Energy energy)=>energyFraction>0?(energy?energy.maximum:100)*energyFraction:energyCost;
  public abstract void Execute(AbilityContext context);
  protected void SpawnVfx(Vector3 at){if(vfxPrefab)Destroy(Instantiate(vfxPrefab,at,Quaternion.identity),5);}
 }
}

using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Player Class")]
 public sealed class PlayerClassDefinition : ScriptableObject {
  public string classId,displayName,role;
  [TextArea] public string description;
  public Color accent=CombatFx.Cyan;
  public GameObject model;
  public float maximumHealth=120,moveSpeed=6.3f;[Min(.1f)]public float gameplayVisualScale=1;
  public bool jetpack,buildEnergyOnHits;[HideInInspector]public int loadoutVersion;
  public AbilityDefinition[] abilities;
  public bool Ready {get{if(!model||abilities==null||abilities.Length<3)return false;foreach(var a in abilities)if(!a)return false;return true;}}
 }
}

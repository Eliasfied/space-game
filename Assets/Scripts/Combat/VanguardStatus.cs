using UnityEngine;
namespace AsterionGame {
 // Status effects live on their recipient so every player's damage uses them.
 public sealed class VanguardStatus:MonoBehaviour {
  float exposedUntil,boostUntil,immuneUntil,matrixUntil;LineRenderer marker;
  public float ExposedRemaining=>Mathf.Max(0,exposedUntil-Time.time);
  public float BoostRemaining=>Mathf.Max(0,boostUntil-Time.time);
  public float ImmunityRemaining=>Mathf.Max(0,immuneUntil-Time.time);
  public float MatrixRemaining=>Mathf.Max(0,matrixUntil-Time.time);
  public static VanguardStatus For(Health h){var s=h.GetComponent<VanguardStatus>();return s?s:h.gameObject.AddComponent<VanguardStatus>();}
  public void Expose(float seconds){exposedUntil=Mathf.Max(exposedUntil,Time.time+seconds);}
  public void Boost(float seconds){boostUntil=Mathf.Max(boostUntil,Time.time+seconds);}
  public void Immune(float seconds){immuneUntil=Mathf.Max(immuneUntil,Time.time+seconds);var cc=GetComponent<CrowdControl>();if(cc)cc.Cleanse();}
  public void Mark(float seconds){matrixUntil=Time.time+seconds;if(!marker){marker=CombatFx.Ring("Targeting Matrix",transform.position,1,CombatFx.Cyan,.11f);marker.transform.SetParent(transform);}}
  public void CriticalHit(){
   if(MatrixRemaining<=0)return;matrixUntil+=1;
   foreach(var caster in Object.FindObjectsByType<AbilityCaster>(FindObjectsInactive.Exclude)){
    var h=caster.GetComponent<Health>();if(h&&h.Alive&&h.team==Team.Player)caster.ReduceCooldowns(1);
   }
  }
  void Update(){if(!marker)return;if(MatrixRemaining<=0||!GetComponent<Health>().Alive){Destroy(marker.gameObject);return;}CombatFx.SetRing(marker,PlayerTargeting.Center(GetComponent<Health>())+Vector3.up*.7f,.7f+.08f*Mathf.Sin(Time.time*4));}
 }
}

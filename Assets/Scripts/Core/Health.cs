using System;
using UnityEngine;
namespace AsterionGame {
 public enum Team { Player, Hostile }
 public sealed class Health : MonoBehaviour {
  public Team team; public float maximum = 100; public float Current { get; private set; }
  public bool Invulnerable { get; set; } public bool Alive => Current > 0;
  public float ProtectionRemaining=>Mathf.Max(0,protectionUntil-Time.time);
  float protectionUntil,reduction;public float DamageReduction=>Time.time<protectionUntil?reduction:0;
  public void Protect(float fraction,float seconds){reduction=Mathf.Clamp01(fraction);protectionUntil=Time.time+Mathf.Max(0,seconds);}
  float shield,shieldUntil;
  public float Shield=>Time.time<shieldUntil?shield:0;
  public void AddShield(float value,float seconds){shield=Mathf.Max(Shield,Mathf.Max(0,value));shieldUntil=Time.time+seconds;}
  public void BreakShield(){shield=0;shieldUntil=0;protectionUntil=0;reduction=0;}
  public event Action<float> Damaged; public event Action Died;
  void Awake() { ResetHealth(); }
  public void ResetHealth() { shield=0;shieldUntil=0;Current = maximum; Invulnerable = false;protectionUntil=0;reduction=0; }
  public float Heal(float value) {
   if(!Alive||value<=0)return 0;float healed=Mathf.Min(value,maximum-Current);Current+=healed;return healed;
  }
  public bool ApplyDamage(float value) {
   if (!Alive || Invulnerable || value <= 0) return false;
   var status=GetComponent<VanguardStatus>();if(status&&status.ExposedRemaining>0)value*=1.15f;
   value*=1-DamageReduction;
   float absorbed=Mathf.Min(Shield,value);shield=Mathf.Max(0,Shield-absorbed);value-=absorbed;
   Current = Mathf.Max(0, Current-value); Damaged?.Invoke(value);
   if (!Alive) Died?.Invoke(); return true;
  }
 }
}

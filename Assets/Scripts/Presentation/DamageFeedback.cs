using UnityEngine;
namespace AsterionGame {
 public sealed class DamageFeedback:MonoBehaviour {
  Health health;Renderer[] renderers;MaterialPropertyBlock block;float flashUntil;bool flashing;
  void Awake(){health=GetComponent<Health>();renderers=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();health.Damaged+=Hit;}
  public void RefreshRenderers(){renderers=GetComponentsInChildren<Renderer>();flashing=false;}
  void Hit(float amount){
   Hud.AddDamage(transform.position+Vector3.up*(health.team==Team.Player?2.5f:3),amount,health.team==Team.Player);
   flashUntil=Time.time+.07f;flashing=true;block.SetColor("_BaseColor",Color.white*1.7f);foreach(var r in renderers)if(r)r.SetPropertyBlock(block);
   if(health.team==Team.Player){Camera.main?.GetComponent<FollowCamera>()?.Shake(.17f);SynthAudio.Play(SoundKind.Hurt,transform.position,.35f);}
  }
  void Update(){if(flashing&&Time.time>flashUntil){flashing=false;foreach(var r in renderers)if(r)r.SetPropertyBlock(null);}}
  void OnDestroy(){health.Damaged-=Hit;}
 }
}

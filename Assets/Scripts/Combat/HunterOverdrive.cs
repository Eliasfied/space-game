using UnityEngine;
namespace AsterionGame {
 public sealed class HunterOverdrive:MonoBehaviour {
  float until;public float Remaining=>Mathf.Max(0,until-Time.time);
  public float CastMultiplier=>Remaining>0?.7f:1;
  public float MoveMultiplier=>Remaining>0?1.5f:1;
  public void Activate(float duration){until=Time.time+duration;}
  public void Clear(){until=0;}
  void Update(){var health=GetComponent<Health>();if(!health||!health.Alive)Clear();}
 }
}

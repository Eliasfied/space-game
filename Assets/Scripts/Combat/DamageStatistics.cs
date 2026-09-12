using UnityEngine;
namespace AsterionGame {
 // Per combatant and encounter; counts actual HP removed, including DoT/AoE.
 public sealed class DamageStatistics {
  public float OverallDamage{get;private set;}
  float startedAt,finishedAt;bool started,finished;
  public float Dps=>started?OverallDamage/Mathf.Max(1,(finished?finishedAt:Time.time)-startedAt):0;
  public void Record(float damage){
   if(finished||damage<=0)return;
   if(!started){started=true;startedAt=Time.time;}
   OverallDamage+=damage;
  }
  public void Finish(){if(finished)return;finished=true;finishedAt=Time.time;}
  public void Reset(){OverallDamage=0;startedAt=finishedAt=0;started=finished=false;}
 }
}

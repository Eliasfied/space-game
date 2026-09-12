using UnityEngine;
namespace AsterionGame {
 public static class EnemyVisuals {
  public static Transform Spawn(string resource,Transform parent){
   var prefab=Resources.Load<GameObject>("Enemies/"+resource);if(!prefab){Debug.LogWarning("Gegner-Modell fehlt: "+resource+". Asterion > Meshy > Rebuild Enemy Integration ausführen.");return null;}
   var visual=Object.Instantiate(prefab,parent,false);foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;return visual.transform;
  }
  public static void ReplaceBoss(BossBrain boss){
   var visual=Spawn("AegisWarden",boss.transform);if(!visual)return;
   if(boss.visual){boss.visual.gameObject.SetActive(false);Object.Destroy(boss.visual.gameObject);}boss.visual=visual;
   var old=boss.GetComponent<MechAnimator>();if(old)old.enabled=false;
   var collider=boss.GetComponent<CapsuleCollider>();if(collider){collider.height=4.2f;collider.center=Vector3.up*2.1f;collider.radius=1.05f;}
   var bar=boss.GetComponent<EnemyHealthBar>();if(bar){bar.displayName="AEGIS WARDEN";bar.verticalOffset=.3f;}
   var feedback=boss.GetComponent<DamageFeedback>();if(feedback)feedback.RefreshRenderers();
  }
 }
}

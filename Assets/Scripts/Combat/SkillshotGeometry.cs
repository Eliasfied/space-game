using UnityEngine;
namespace AsterionGame {
 public static class SkillshotGeometry {
  public static bool InCone(Vector3 origin,Vector3 direction,Vector3 point,float range,float angle){
   Vector3 delta=point-origin;delta.y=0;direction.y=0;
   return delta.sqrMagnitude<=range*range&&(delta.sqrMagnitude<.0001f||Vector3.Dot(direction.normalized,delta.normalized)>=Mathf.Cos(angle*.5f*Mathf.Deg2Rad));
  }
  public static Vector3 GroundPoint(Vector3 origin,Vector3 mousePoint,float range){
   Vector3 delta=mousePoint-origin;delta.y=0;Vector3 point=origin+Vector3.ClampMagnitude(delta,range);point.y=.08f;return point;
  }
 }
}

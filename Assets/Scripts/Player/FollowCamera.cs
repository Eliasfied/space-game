using UnityEngine;
namespace AsterionGame {
 [DefaultExecutionOrder(-80)]
 [RequireComponent(typeof(Camera))]
 public sealed class FollowCamera : MonoBehaviour {
  public Transform target;
  [Range(5,12)] public float viewSize=8.2f;
  [Range(40,70)] public float pitch=55;
  public float distance=20, smoothing=7, forwardFraming=1.1f;
  [SerializeField,HideInInspector] int perspectiveVersion;
  void Awake(){
   // Upgrade the saved camera preset while keeping its fixed angle and mouse controls.
   if(perspectiveVersion<2){pitch=55;distance=20;viewSize=7.4f;perspectiveVersion=2;}
   if(perspectiveVersion<3){viewSize=8.2f;perspectiveVersion=3;}
  }
  public float yaw;
  public Quaternion Heading=>Quaternion.Euler(0,yaw,0);
  // The camera follows position only. Mouse steering belongs to the player.
  Vector3 center;float shake;Camera view;
  void Start(){view=GetComponent<Camera>();view.orthographic=true;view.orthographicSize=viewSize;if(target)center=FocusPoint();PlaceCamera();}
  Vector3 FocusPoint()=>new Vector3(target.position.x,.65f,target.position.z)+(Heading*Vector3.forward)*forwardFraming;
  void PlaceCamera(){transform.rotation=Quaternion.Euler(pitch,yaw,0);transform.position=center-transform.forward*distance+Random.insideUnitSphere*shake;}
  public void Shake(float strength){shake=Mathf.Max(shake,strength);}
  void LateUpdate(){if(!target)return;center=Vector3.Lerp(center,FocusPoint(),1-Mathf.Exp(-smoothing*Time.deltaTime));shake=Mathf.MoveTowards(shake,0,Time.unscaledDeltaTime*1.7f);view.orthographicSize=viewSize;PlaceCamera();}
 }
}

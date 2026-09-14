using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(CharacterController),typeof(Health),typeof(PlayerInputReader))]
 [DefaultExecutionOrder(-50)]
 public sealed class PlayerMotor : MonoBehaviour {
  public float speed=6.3f, acceleration=32, rotationSpeed=18; public Transform visual;
  [Min(0)] public float mouseSensitivity=.18f;
  public Vector3 AimPoint { get; private set; } public Vector3 MoveDirection { get; private set; }
  public bool MovementLocked{get;set;}
  public bool IsDashing => dashRemaining > 0;
  public bool IsJetDashing => IsDashing && jetDash;bool jetDash;
  CharacterController controller; Health health; PlayerInputReader input; Vector3 velocity,dashDirection;float dashRemaining,gravity,dashSpeed=23;
  void Awake(){controller=GetComponent<CharacterController>();health=GetComponent<Health>();input=GetComponent<PlayerInputReader>();}
  void Update(){
   if(Time.timeScale==0 || !health.Alive) return;
   var camera=Camera.main;
   if(camera){Ray ray=camera.ScreenPointToRay(input.Pointer);if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))AimPoint=ray.GetPoint(distance);}
   var cc=GetComponent<CrowdControl>();
   if(cc&&cc.IsStunned){dashRemaining=0;velocity=Vector3.zero;health.Invulnerable=false;}
   // Turn before reading movement so W/S and strafing use this frame's heading.
   var activeCaster=GetComponent<AbilityCaster>();bool aimedChannel=activeCaster&&activeCaster.IsChanneling&&activeCaster.ActiveAbility.AimShape!=AbilityAimShape.None;
   if(input.MouseLookActive&&!aimedChannel&&!(cc&&(cc.IsStunned||cc.IsKnockedBack)))
    transform.rotation=Quaternion.AngleAxis(input.LookDelta.x*mouseSensitivity,Vector3.up)*transform.rotation;
   MoveDirection=MovementLocked||(cc&&(cc.IsStunned||cc.IsKnockedBack))?Vector3.zero:DesiredMovement()*(cc?cc.MovementMultiplier:1);
   if(MovementLocked)velocity=Vector3.zero;
   var overdrive=GetComponent<HunterOverdrive>();
   velocity=Vector3.MoveTowards(velocity,MoveDirection*speed*(overdrive?overdrive.MoveMultiplier:1),acceleration*Time.deltaTime);
   if(IsDashing){controller.Move(dashDirection*dashSpeed*Mathf.Min(Time.deltaTime,dashRemaining));dashRemaining-=Time.deltaTime;if(!IsDashing)health.Invulnerable=false;}
   else controller.Move(velocity*Time.deltaTime);
   if(controller.isGrounded)gravity=-2;else gravity-=25*Time.deltaTime;
   controller.Move(Vector3.up*gravity*Time.deltaTime);
   if(aimedChannel)transform.rotation=Quaternion.LookRotation(activeCaster.CastDirection);
   else if(!input.MouseLookActive){
    // Q/E must not turn the body toward its sideways travel (which would spiral).
    Vector3 face=input.Strafe!=0?Vector3.zero:MoveDirection;
    var caster=GetComponent<AbilityCaster>();
    if(caster&&caster.IsBusy&&caster.Target)face=PlayerTargeting.Center(caster.Target)-transform.position;
    face.y=0;
    if(face.sqrMagnitude>.05f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(face),1-Mathf.Exp(-rotationSpeed*Time.deltaTime));
   }
  }
  Vector3 DesiredMovement(){
   Vector2 m=input.Move;
   // Mouse running is the fallback; WASD can override it while mouse steering stays active.
   if(input.MouseForward&&!input.MovementKeysHeld)m.y=1;
   if(input.MouseLookActive){
    m.x=Mathf.Clamp(m.x+input.Strafe,-1,1);m=Vector2.ClampMagnitude(m,1);
    return transform.rotation*new Vector3(m.x,0,m.y);
   }
   var camera=Camera.main;var follow=camera?camera.GetComponent<FollowCamera>():null;
   Quaternion heading=follow?follow.Heading:Quaternion.identity;
   return Vector3.ClampMagnitude(heading*new Vector3(m.x,0,m.y)+transform.right*input.Strafe,1);
  }
  public void PullTo(Vector3 destination){
   Vector3 delta=destination-transform.position;delta.y=0;
   if(delta.magnitude>1.4f){dashRemaining=0;health.Invulnerable=false;controller.Move(delta.normalized*(delta.magnitude-1.4f));velocity=Vector3.zero;}
  }
  public void Dash(float distance,bool jetAssisted=false,float travelSpeed=23){Vector3 direction=DesiredMovement();DashTowards(direction.sqrMagnitude>.05f?direction.normalized:transform.forward,distance,true,jetAssisted,travelSpeed);}
  public void DashTowards(Vector3 direction,float distance,bool invulnerable=false,bool jetAssisted=false,float travelSpeed=23){jetDash=jetAssisted;dashDirection=direction.normalized;dashSpeed=Mathf.Max(1,travelSpeed);dashRemaining=distance/dashSpeed;health.Invulnerable=invulnerable;CombatFx.Burst(transform.position+Vector3.up*.5f,CombatFx.Cyan,12,.8f);}
  void OnDisable(){if(health)health.Invulnerable=false;}
 }
}

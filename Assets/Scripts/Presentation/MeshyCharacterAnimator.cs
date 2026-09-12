using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Animator))]
 public sealed class MeshyCharacterAnimator:MonoBehaviour {
  public Transform visualRoot;public Transform supportGrip;
  public bool preserveModelTransform;Vector3 modelPosition,modelScale;Quaternion modelRotation;public float soleOffset=.1f;
  float previewFloor;bool groundPreview;
  public void SetPreviewFloor(float worldY){previewFloor=worldY;groundPreview=true;}
  void LateUpdate(){
   if(preserveModelTransform){transform.localPosition=modelPosition;transform.localRotation=modelRotation;transform.localScale=modelScale;}
   if(!groundPreview||motor||!animator||!visualRoot)return;
   float lowest=float.PositiveInfinity;
   foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes}){
    var foot=animator.GetBoneTransform(bone);if(foot)lowest=Mathf.Min(lowest,foot.position.y);
   }
   if(float.IsPositiveInfinity(lowest))return;
   // Follow the supporting foot after the walk pose is evaluated. Do not move
   // the platform or modify gameplay movement, root motion or model size.
   float sole=lowest-soleOffset*visualRoot.lossyScale.y;
   visualRoot.position+=Vector3.up*(previewFloor-sole);
  }
  Animator animator;PlayerMotor motor;AbilityCaster caster;Health health;
  float move,actionUntil,shootUntil,fireWeight;int fireLayer=-1;bool died,firing;
  void Awake(){
   modelPosition=transform.localPosition;modelRotation=transform.localRotation;modelScale=transform.localScale;
   animator=GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   fireLayer=animator.GetLayerIndex("Weapon fire");
  }
  void Start(){
   motor=GetComponentInParent<PlayerMotor>();caster=GetComponentInParent<AbilityCaster>();health=GetComponentInParent<Health>();
   animator.updateMode=motor?AnimatorUpdateMode.Normal:AnimatorUpdateMode.UnscaledTime;
   if(!motor&&animator.HasState(0,Animator.StringToHash("Base Layer.PreviewIdle")))animator.Play("Base Layer.PreviewIdle",0,0);
   if(caster)caster.AbilityExecuted+=OnAbility;
  }
  void Update(){
   if(!motor)return;
   if(health&&!health.Alive){
    if(!died){died=true;animator.CrossFadeInFixedTime("Death",.10f,0);}
    if(fireLayer>=0)animator.SetLayerWeight(fireLayer,0);return;
   }
   float desired=motor.IsDashing?0:motor.MoveDirection.magnitude;
   move=Mathf.MoveTowards(move,desired,Time.deltaTime*5);animator.SetFloat("Move",move);
   bool channel=caster&&caster.ActiveAbility is UltimateAbility;
   bool readyToShoot=caster&&(caster.ActiveAbility is LaserAbility||caster.ActiveAbility is VanguardAbility v&&v.IsWeapon)&&caster.CastRemaining<.18f;
   bool wantsFire=(Time.time<shootUntil||channel||readyToShoot)&&Time.time>=actionUntil&&!motor.IsDashing;
   if(fireLayer>=0){
    // Start once, then let the supplied recoil cycle run. Restarting for every
    // rapid projectile would repeatedly show only the first few frames.
    if(wantsFire&&!firing)animator.Play("Weapon fire.Shooting",fireLayer,0);
    fireWeight=Mathf.MoveTowards(fireWeight,wantsFire?1:0,Time.deltaTime*12);
    animator.SetLayerWeight(fireLayer,fireWeight);
   }
   firing=wantsFire;
  }
  void OnAnimatorIK(int layerIndex){
   if(!supportGrip||layerIndex!=fireLayer)return;
   animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,fireWeight);
   animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,0);
   animator.SetIKPosition(AvatarIKGoal.LeftHand,supportGrip.position);
  }
  void StopFire(){shootUntil=0;firing=false;fireWeight=0;if(fireLayer>=0)animator.SetLayerWeight(fireLayer,0);}
  public void PlayKick(){if(!animator||died)return;actionUntil=Time.time+.8f;StopFire();animator.CrossFadeInFixedTime("Kick",.045f,0);}
  void OnAbility(AbilityDefinition ability){
   if(died)return;
   if(ability is VanguardAbility weapon&&weapon.IsWeapon){shootUntil=Time.time+.5f;fireWeight=1;if(fireLayer>=0){animator.Play("Weapon fire.Shooting",fireLayer,0);animator.SetLayerWeight(fireLayer,1);}}
   if(ability is LaserAbility){shootUntil=Time.time+Mathf.Max(.4f,ability.cooldown+.06f);fireWeight=1;if(fireLayer>=0)animator.SetLayerWeight(fireLayer,1);}
   if(ability is ChargedShotAbility)shootUntil=Time.time+.5f;
   if(ability is GrenadeAbility||ability is ShieldAbility||ability is OrbitalAbility||ability is VanguardAbility support&&!support.IsWeapon&&!support.IsMovement){actionUntil=Time.time+.65f;StopFire();animator.CrossFadeInFixedTime("Skill",.075f,0);}
  }
  void OnDestroy(){if(caster)caster.AbilityExecuted-=OnAbility;}
 }
}

using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [DefaultExecutionOrder(-30)]
 public sealed class AbilityCaster:MonoBehaviour {
  public DamageStatistics Statistics{get;}=new DamageStatistics();
  public AbilityDefinition[] abilities;public float[] ReadyAt{get;private set;}
  public const float GlobalCooldown=1;
  public bool IsChanneling{get;private set;}bool casting;
  public float CastDuration{get;private set;}
  public float CastProgress=>casting?Mathf.Clamp01(1-(castEnd-Time.time)/Mathf.Max(.01f,CastDuration)):IsChanneling?Mathf.Clamp01((Time.time-channelStart)/Mathf.Max(.01f,CastDuration)):0;
  public Vector3 CastDirection=>pending.direction;
  public bool IsAiming=>aimSlot>=0;
  public string AimName=>IsAiming?abilities[aimSlot].displayName:"";
  int aimSlot=-1;AbilityAimPreview aimPreview;Vector3 aimPoint,aimDirection;
  public float CastRemaining=>casting?Mathf.Max(0,castEnd-Time.time):IsChanneling?Mathf.Max(0,channelEnd-Time.time):0;
  public string CastName=>IsBusy?abilities[castSlot].displayName:"";
  public string Message{get;private set;}public float MessageUntil{get;private set;}
  public bool IsBusy=>casting||IsChanneling;
  public AbilityDefinition ActiveAbility=>IsBusy?abilities[castSlot]:null;
  public event System.Action<AbilityDefinition> AbilityExecuted;
  public bool ChargedProc{get;private set;}public float ProcFlashedAt{get;private set;}
  public bool KickResetReady{get;private set;}
  public bool SlotHighlighted(int slot)=>ProcReady(slot)||!IsVanguard&&slot==3&&KickResetReady;
  public void RollHunterChargedProc(float chance){if(IsVanguard||!health.Alive||Random.value>=chance)return;ChargedProc=true;ProcFlashedAt=Time.time;Hint("ION OVERCHARGE! F: SOFORT / +20% SCHADEN");}
  public bool IsVanguard=>abilities!=null&&abilities.Length>0&&abilities[0] is VanguardAbility;
  public int MovementSlot=>IsVanguard?2:1;
  public int ProcSlot=>IsVanguard?1:5;
  public bool ProcReady(int slot)=>slot==ProcSlot&&ChargedProc;
  public float EnergyCost(int slot){
   if(abilities==null||slot<0||slot>=abilities.Length||!abilities[slot])return 0;
   return IsVanguard&&ProcReady(slot)?0:abilities[slot].Cost(energy);
  }
  public void ReduceCooldowns(float seconds){if(ReadyAt==null)return;for(int i=0;i<ReadyAt.Length;i++)ReadyAt[i]=Mathf.Max(Time.time,ReadyAt[i]-seconds);}
  public void RollVanguardProc(){if(!IsVanguard||Random.value>=.15f)return;ChargedProc=true;ProcFlashedAt=Time.time;Hint("RAILGUN BEREIT! 2: SOFORT / KOSTENLOS");}
  public float GlobalRemaining=>Mathf.Max(0,globalUntil-Time.time);
  public Health Target=>targeting?targeting.Current:null;
  struct Execution{public AbilityDefinition ability;public AbilityContext context;}
  struct Bolt{public int muzzle;public AbilityContext context;public float at,damage,range,gain;public Color color;public bool vanguardPulse;}
  readonly List<Execution> frameShots=new List<Execution>();readonly List<Bolt> bolts=new List<Bolt>();
  Transform[] muzzles=System.Array.Empty<Transform>();int muzzleIndex;
  public Transform ShotMuzzle=>muzzles.Length>0?muzzles[muzzleIndex%muzzles.Length]:null;
  public Vector3 ShotOrigin=>ShotMuzzle?ShotMuzzle.position:transform.position+Vector3.up*1.25f+transform.forward*.7f;
  public void AdvanceMuzzle(){muzzleIndex=(muzzleIndex+1)%Mathf.Max(1,muzzles.Length);}
  PlayerInputReader input;PlayerMotor motor;Health health;Energy energy;PlayerTargeting targeting;
  float castEnd,channelStart,channelEnd,globalUntil;int castSlot,channelTick;AbilityContext pending;bool reservedProc;
  void Awake(){
   ReadyAt=new float[Mathf.Max(8,abilities==null?0:abilities.Length)];input=GetComponent<PlayerInputReader>();motor=GetComponent<PlayerMotor>();health=GetComponent<Health>();energy=GetComponent<Energy>();if(!energy)energy=gameObject.AddComponent<Energy>();
   targeting=GetComponent<PlayerTargeting>();if(!targeting)targeting=gameObject.AddComponent<PlayerTargeting>();
  }
  void Start(){BindMuzzles(motor.visual?motor.visual:transform);}
  void BindMuzzles(Transform visual){var found=new List<Transform>();foreach(var t in visual.GetComponentsInChildren<Transform>())if(t.name=="Muzzle"||t.name=="MuzzleLeft"||t.name=="MuzzleRight")found.Add(t);found.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));muzzles=found.ToArray();muzzleIndex=0;}
  public void Configure(AbilityDefinition[] definitions,Transform visual){Statistics.Reset();CancelAim();CancelCast();frameShots.Clear();bolts.Clear();abilities=definitions;ReadyAt=new float[Mathf.Max(8,definitions.Length)];ChargedProc=false;KickResetReady=false;GetComponent<HunterOverdrive>()?.Clear();globalUntil=0;Message="";MessageUntil=0;BindMuzzles(visual);}
  void Update(){
   if(!health.Alive){CancelAim();CancelCast();frameShots.Clear();bolts.Clear();return;}if(Time.timeScale==0){CancelAim();return;}
   var cc=GetComponent<CrowdControl>();if(cc&&cc.IsStunned){CancelAim();CancelCast();return;}
   if(input.DashPressed)TryCast(1);
   if(input.OrbitalPressed&&MovementSlot==2)TryCast(2);
   if(IsBusy&&ActiveAbility.RequiresTarget&&!ValidTarget(pending.target,ActiveAbility.range,false)){CancelCast();Hint("ZIEL NICHT ERREICHBAR");}
   if(casting&&Time.time>=castEnd){casting=false;Resolve(abilities[castSlot]);}
   if(IsChanneling){
    var a=abilities[castSlot];float interval=Mathf.Max(.05f,a.channelTickInterval);int ticks=Mathf.CeilToInt(a.channelDuration/interval);
    while(channelTick<ticks&&Time.time>=channelStart+channelTick*interval){ExecuteAfterPose(a,pending);channelTick++;}
    if(Time.time>=channelEnd)CancelCast();
   }
   if(IsAiming){
    UpdateAim();
    if(input.AimCancelPressed){CancelAim();return;}
    if(input.AimConfirmPressed){int slot=aimSlot;Vector3 point=aimPoint,direction=aimDirection;CancelAim();BeginCast(slot,point,direction,true);return;}
   }
   if(input.OrbitalPressed&&MovementSlot!=2)TryCast(2);
   for(int i=3;i<8;i++)if(input.AbilityPressed(i))TryCast(i);
   if(!IsAiming&&input.FireHeld)TryCast(0);
  }
  bool ValidTarget(Health target,float range,bool notify){
   string reason=null;
   if(!target||!target.Alive||!target.gameObject.activeInHierarchy)reason="KEIN ZIEL — TAB ODER GEGNER ANKLICKEN";
   else if(Vector3.Distance(transform.position,target.transform.position)>range)reason="ZIEL AUSSER REICHWEITE";
   else if(Physics.Linecast(transform.position+Vector3.up*1.3f,PlayerTargeting.Center(target),~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))reason="KEINE SICHTLINIE";
   if(reason!=null&&notify)Hint(reason);return reason==null;
  }
  AbilityContext RefreshContext(AbilityDefinition ability,AbilityContext context){
   if(ability.AimShape!=AbilityAimShape.None)return context;
   Vector3 aim=context.target&&context.target.Alive?PlayerTargeting.Center(context.target):ShotAimPoint();
   Vector3 direction=aim-ShotOrigin;if(direction.sqrMagnitude<.0001f)direction=transform.forward;
   context.direction=direction.normalized;
   if(ability is KickAbility||ability is GrenadeAbility){context.direction.y=0;context.direction.Normalize();}
   if(context.target&&ability.RequiresTarget){context.point=context.target.transform.position;context.point.y=.05f;}
   return context;
  }
  public Vector3 ShotAimPoint()=>Target?PlayerTargeting.Center(Target):motor.AimPoint+Vector3.up;
  void Resolve(AbilityDefinition a){
   if(a.RequiresTarget&&!ValidTarget(pending.target,a.range,true)){CancelCast();return;}
   if(reservedProc){ChargedProc=false;pending.damageMultiplier=IsVanguard?1:1.2f;ReadyAt[castSlot]=IsVanguard?Time.time+a.cooldown:0;}
   else ReadyAt[castSlot]=Time.time+a.cooldown;
   reservedProc=false;
   energy.Gain(a.energyOnCast);
   // One kick-reset roll per completed cast, independent of its two projectiles.
   if(a is LaserAbility laser&&laser.twinProjectiles&&Random.value<laser.kickResetChance){ReadyAt[3]=Time.time;KickResetReady=true;ProcFlashedAt=Time.time;Hint("PULSE KICK ZURÜCKGESETZT! 4");}
   if(a is KickAbility)KickResetReady=false;
   a.OnStarted(pending);
   AbilityExecuted?.Invoke(a);
   if(a.channelDuration>0){IsChanneling=true;CastDuration=a.channelDuration;channelStart=Time.time;channelEnd=Time.time+a.channelDuration;channelTick=0;}
   else{ExecuteAfterPose(a,pending);motor.MovementLocked=false;}
  }
  void ExecuteAfterPose(AbilityDefinition ability,AbilityContext context){
   if(ability is LaserAbility||ability is ChargedShotAbility||ability is UltimateAbility||ability is ExplosiveShotAbility||ability is VanguardAbility vanguard&&vanguard.IsWeapon)frameShots.Add(new Execution{ability=ability,context=context});
   else ability.Execute(RefreshContext(ability,context));
  }
  public void FireTwin(AbilityContext context,float damage,float range,float gain,Color color){
   // Store the original cast target: a later Tab press must not redirect its second shot.
   FireBolt(new Bolt{muzzle=0,context=context,damage=damage,range=range,gain=gain,color=color});
   bolts.Add(new Bolt{muzzle=1,context=context,at=Time.time+.12f,damage=damage,range=range,gain=gain,color=color});
  }
  public void FireBurst(AbilityContext context,float damage,float range,float gain,Color color,int count,float interval,bool vanguardPulse){
   for(int i=0;i<count;i++){
    var bolt=new Bolt{muzzle=0,context=context,damage=damage,range=range,gain=gain,color=color,at=Time.time+i*interval,vanguardPulse=vanguardPulse};
    if(i==0)FireBolt(bolt);else bolts.Add(bolt);
   }
  }
  void FireBolt(Bolt bolt){
   if(!bolt.context.target||!bolt.context.target.Alive)return;
   var socket=muzzles.Length>0?muzzles[bolt.muzzle%muzzles.Length]:null;Vector3 origin=socket?socket.position:ShotOrigin;Vector3 direction=(PlayerTargeting.Center(bolt.context.target)-origin).normalized;
   CombatFx.PistolFlash(socket,origin,direction,bolt.color);
   BlasterProjectile.Spawn(this,origin,direction,bolt.range,bolt.damage,bolt.gain,bolt.color,bolt.context.target,vanguardPulse:bolt.vanguardPulse);SynthAudio.Play(SoundKind.Pistol,transform.position,.27f);
  }
  void LateUpdate(){
   if(health.Alive&&Time.timeScale>0){
    if(frameShots.Count>0||bolts.Count>0)Physics.SyncTransforms();
    foreach(var shot in frameShots)if(!shot.ability.RequiresTarget||ValidTarget(shot.context.target,shot.ability.range,false))shot.ability.Execute(RefreshContext(shot.ability,shot.context));
    for(int i=bolts.Count-1;i>=0;i--)if(Time.time>=bolts[i].at){FireBolt(bolts[i]);bolts.RemoveAt(i);}
   }
   frameShots.Clear();
  }
  public void CancelCast(){casting=false;IsChanneling=false;reservedProc=false;if(motor)motor.MovementLocked=false;}
  void OnDisable(){CancelAim();CancelCast();frameShots.Clear();bolts.Clear();}
  public float AbilityCooldownRemaining(int slot){
   if(ReadyAt==null||slot<0||slot>=ReadyAt.Length)return 0;
   return !IsVanguard&&ProcReady(slot)?0:Mathf.Max(0,ReadyAt[slot]-Time.time);
  }
  public float Remaining(int slot){if(ReadyAt==null||slot<0||slot>=ReadyAt.Length)return 0;return Mathf.Max(AbilityCooldownRemaining(slot),slot==MovementSlot?0:GlobalRemaining);}
  public bool TryCast(int slot){
   if(!CanStart(slot))return false;
   var a=abilities[slot];
   if(a.AimShape!=AbilityAimShape.None){
    if(aimSlot==slot){CancelAim();return true;}
    aimSlot=slot;input.SetAbilityAiming(true);
    if(!aimPreview)aimPreview=new GameObject("Ability ground preview").AddComponent<AbilityAimPreview>();
    aimPreview.gameObject.SetActive(true);UpdateAim();return true;
   }
   return BeginCast(slot,default,default,false);
  }
  bool CanStart(int slot){
   if(Time.timeScale<=0||!health.Alive||abilities==null||slot<0||slot>=abilities.Length||!abilities[slot]||Remaining(slot)>0)return false;
   if(IsBusy&&slot!=MovementSlot||motor.IsDashing&&!motor.IsJetDashing)return false;
   var control=GetComponent<CrowdControl>();if(control&&control.IsStunned)return false;
   return true;
  }
  void UpdateAim(){
   var a=abilities[aimSlot];Vector3 origin=transform.position,mouse=motor.AimPoint;
   if(Camera.main){Ray ray=Camera.main.ScreenPointToRay(input.Pointer);if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))mouse=ray.GetPoint(distance);}
   aimPoint=SkillshotGeometry.GroundPoint(origin,mouse,a.range);
   if(a.RequiresGroundPoint&&AegisLevel.Instance)aimPoint=AegisLevel.Instance.ClampToFloor(aimPoint);
   aimDirection=aimPoint-origin;aimDirection.y=0;aimDirection=aimDirection.sqrMagnitude>.001f?aimDirection.normalized:transform.forward;
   aimPreview.Draw(a,origin,aimPoint,aimDirection,energy.Current>=EnergyCost(aimSlot)&&GroundAimInRange(a,aimPoint));
  }
  bool GroundAimInRange(AbilityDefinition ability,Vector3 point){Vector3 delta=point-transform.position;delta.y=0;return !ability.RequiresGroundPoint||delta.sqrMagnitude<=(ability.range+.01f)*(ability.range+.01f);}
  public void CancelAim(){if(IsAiming&&input)input.SetAbilityAiming(false);aimSlot=-1;if(aimPreview)aimPreview.gameObject.SetActive(false);}
  void OnDestroy(){if(aimPreview)Destroy(aimPreview.gameObject);}
  bool BeginCast(int slot,Vector3 selectedPoint,Vector3 selectedDirection,bool aimed){
   if(!CanStart(slot))return false;
   var a=abilities[slot];var target=Target;
   if(a.RequiresTarget&&!ValidTarget(target,a.range,true))return false;
   if(a is VanguardAbility support&&support.skill==VanguardSkill.TractorBeam&&!VanguardAbility.Partner(this,a.range,true)){Hint("KEIN PARTNER IN REICHWEITE");return false;}
   if(a is ShieldAbility shield)target=shield.Recipient(this);
   if(aimed&&!GroundAimInRange(a,selectedPoint)){Hint("ZIELPUNKT AUSSER REICHWEITE");return false;}
   Vector3 point=aimed?selectedPoint:target?target.transform.position:motor.AimPoint;point.y=.05f;
   Vector3 direction=aimed?selectedDirection:point-transform.position;direction.y=0;if(direction.sqrMagnitude<.001f)direction=transform.forward;direction.Normalize();
   if(!energy.Spend(EnergyCost(slot))){Hint("NICHT GENUG ENERGIE");return false;}
   var context=new AbilityContext{caster=this,target=aimed?null:target,point=point,direction=direction,damageMultiplier=1};
   // Jetpack is independent of the active spell. It must not replace its pending
   // target, cast timer, channel, proc reservation or movement lock.
   if(a is DashAbility dash&&dash.jetAssisted){ReadyAt[slot]=Time.time+a.cooldown;AbilityExecuted?.Invoke(a);a.Execute(context);return true;}
   CancelAim();CancelCast();castSlot=slot;reservedProc=ProcReady(slot);pending=context;motor.MovementLocked=a.lockMovement;
   if(aimed)transform.rotation=Quaternion.LookRotation(direction);
   if(slot!=MovementSlot)globalUntil=Time.time+GlobalCooldown;
   var buff=GetComponent<HunterOverdrive>();CastDuration=a.castTime*(buff?buff.CastMultiplier:1);
   if(CastDuration>0&&!reservedProc){casting=true;castEnd=Time.time+CastDuration;}else Resolve(a);
   return true;
  }
  void Hint(string message){Message=message;MessageUntil=Time.time+2.5f;}
 }
}

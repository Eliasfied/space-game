using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [DefaultExecutionOrder(-30)]
 public sealed class AbilityCaster:MonoBehaviour {
  public DamageStatistics Statistics{get;}=new DamageStatistics();
  public AbilityDefinition[] abilities;public float[] ReadyAt{get;private set;}
  public const float GlobalCooldown=1;
  public bool IsChanneling{get;private set;}bool casting;
  public float CastProgress=>casting?Mathf.Clamp01(1-(castEnd-Time.time)/Mathf.Max(.01f,abilities[castSlot].castTime)):IsChanneling?Mathf.Clamp01((Time.time-channelStart)/Mathf.Max(.01f,abilities[castSlot].channelDuration)):0;
  public float CastRemaining=>casting?Mathf.Max(0,castEnd-Time.time):IsChanneling?Mathf.Max(0,channelEnd-Time.time):0;
  public string CastName=>IsBusy?abilities[castSlot].displayName:"";
  public string Message{get;private set;}public float MessageUntil{get;private set;}
  public bool IsBusy=>casting||IsChanneling;
  public AbilityDefinition ActiveAbility=>IsBusy?abilities[castSlot]:null;
  public event System.Action<AbilityDefinition> AbilityExecuted;
  public bool ChargedProc{get;private set;}public float ProcFlashedAt{get;private set;}
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
  public void Configure(AbilityDefinition[] definitions,Transform visual){Statistics.Reset();CancelCast();frameShots.Clear();bolts.Clear();abilities=definitions;ReadyAt=new float[Mathf.Max(8,definitions.Length)];ChargedProc=false;globalUntil=0;Message="";MessageUntil=0;BindMuzzles(visual);}
  void Update(){
   if(!health.Alive){CancelCast();frameShots.Clear();bolts.Clear();return;}if(Time.timeScale==0)return;
   var cc=GetComponent<CrowdControl>();if(cc&&cc.IsStunned){CancelCast();return;}
   if(input.DashPressed)TryCast(1);
   if(input.OrbitalPressed&&MovementSlot==2)TryCast(2);
   if(IsBusy&&ActiveAbility.RequiresTarget&&!ValidTarget(pending.target,ActiveAbility.range,false)){CancelCast();Hint("ZIEL NICHT ERREICHBAR");}
   if(casting&&Time.time>=castEnd){casting=false;Resolve(abilities[castSlot]);}
   if(IsChanneling){
    var a=abilities[castSlot];float interval=Mathf.Max(.05f,a.channelTickInterval);int ticks=Mathf.CeilToInt(a.channelDuration/interval);
    while(channelTick<ticks&&Time.time>=channelStart+channelTick*interval){ExecuteAfterPose(a,pending);channelTick++;}
    if(Time.time>=channelEnd)CancelCast();
   }
   if(input.OrbitalPressed&&MovementSlot!=2)TryCast(2);
   for(int i=3;i<8;i++)if(input.AbilityPressed(i))TryCast(i);
   if(input.FireHeld)TryCast(0);
  }
  bool ValidTarget(Health target,float range,bool notify){
   string reason=null;
   if(!target||!target.Alive||!target.gameObject.activeInHierarchy)reason="KEIN ZIEL — TAB ODER GEGNER ANKLICKEN";
   else if(Vector3.Distance(transform.position,target.transform.position)>range)reason="ZIEL AUSSER REICHWEITE";
   else if(Physics.Linecast(transform.position+Vector3.up*1.3f,PlayerTargeting.Center(target),~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))reason="KEINE SICHTLINIE";
   if(reason!=null&&notify)Hint(reason);return reason==null;
  }
  AbilityContext RefreshContext(AbilityDefinition ability,AbilityContext context){
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
   if(reservedProc){ChargedProc=false;pending.damageMultiplier=IsVanguard?1:1.5f;ReadyAt[castSlot]=IsVanguard?Time.time+a.cooldown:0;}
   else ReadyAt[castSlot]=Time.time+a.cooldown;
   reservedProc=false;
   // One roll per completed Twin Pulses cast, not per bullet or frame.
   if(a is LaserAbility laser&&laser.twinProjectiles&&Random.value<.2f){ChargedProc=true;ReadyAt[5]=0;ProcFlashedAt=Time.time;Hint("OVERCHARGE! F: SOFORT / +50% SCHADEN");}
   AbilityExecuted?.Invoke(a);
   if(a.channelDuration>0){IsChanneling=true;channelStart=Time.time;channelEnd=Time.time+a.channelDuration;channelTick=0;}
   else{ExecuteAfterPose(a,pending);motor.MovementLocked=false;}
  }
  void ExecuteAfterPose(AbilityDefinition ability,AbilityContext context){
   if(ability is LaserAbility||ability is ChargedShotAbility||ability is UltimateAbility||ability is VanguardAbility vanguard&&vanguard.IsWeapon)frameShots.Add(new Execution{ability=ability,context=context});
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
  void OnDisable(){CancelCast();frameShots.Clear();bolts.Clear();}
  public float Remaining(int slot){if(ReadyAt==null||slot<0||slot>=ReadyAt.Length)return 0;return Mathf.Max(!IsVanguard&&ProcReady(slot)?0:Mathf.Max(0,ReadyAt[slot]-Time.time),slot==MovementSlot?0:GlobalRemaining);}
  public bool TryCast(int slot){
   if(!health.Alive||abilities==null||slot<0||slot>=abilities.Length||!abilities[slot]||Remaining(slot)>0)return false;
   if(IsBusy&&slot!=MovementSlot||motor.IsDashing)return false;
   var control=GetComponent<CrowdControl>();if(control&&control.IsStunned)return false;
   var a=abilities[slot];var target=Target;
   if(a.RequiresTarget&&!ValidTarget(target,a.range,true))return false;
   if(a is VanguardAbility support&&support.skill==VanguardSkill.TractorBeam&&!VanguardAbility.Partner(this,a.range,true)){Hint("KEIN PARTNER IN REICHWEITE");return false;}
   Vector3 point=target?target.transform.position:motor.AimPoint;point.y=.05f;
   if(!energy.Spend(EnergyCost(slot))){Hint("NICHT GENUG ENERGIE");return false;}
   CancelCast();castSlot=slot;reservedProc=ProcReady(slot);pending=new AbilityContext{caster=this,target=target,point=point,damageMultiplier=1};motor.MovementLocked=a.lockMovement;
   if(slot!=MovementSlot)globalUntil=Time.time+GlobalCooldown;
   if(a.castTime>0&&!reservedProc){casting=true;castEnd=Time.time+a.castTime;}else Resolve(a);
   return true;
  }
  void Hint(string message){Message=message;MessageUntil=Time.time+2.5f;}
 }
}

using System.Collections.Generic;
using UnityEngine;

namespace AsterionGame {
 // Aegis vector artwork is exported as transparent textures. Text and combat state stay live.
 public sealed class SciFiActionBar : System.IDisposable {
  const float Width=520,Height=152,SlotSize=60,SlotStep=64,SlotY=68;
  static readonly Color Cyan=new Color(.16f,.94f,.91f),Crimson=new Color(1,.25f,.36f),Gold=new Color(1,.74f,.32f);
  static readonly Color Pale=new Color(.86f,.94f,.96f),Muted=new Color(.51f,.64f,.69f);
  static readonly string[] ArtNames={"proc-glow","compact-frame","compact-mask","compact-glow",
   "twin-pulses","jet-burst","ion-grenade","pulse-kick","repulsor-grenade","explosive-shot","charged-shot","aegis-shield","overdrive",
   "pulse-burst","overcharge-railgun","graviton-dash","supply-drone","concussive-blast","tractor-beam","targeting-matrix","orbital-strike"};
  readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
  readonly AegisBarRenderer bars=new AegisBarRenderer();
  GUIStyle text,heavy,tooltip;
  bool initialized;

  void Init(){
   if(initialized)return;
   initialized=true;
   foreach(var name in ArtNames){
    var texture=Resources.Load<Texture2D>("UI/Aegis/"+name);
    if(texture)art[name]=texture;else Debug.LogWarning("Aegis UI texture missing: "+name);
   }
   text=new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),alignment=TextAnchor.MiddleCenter};
   heavy=new GUIStyle(text){fontStyle=FontStyle.Bold};
   tooltip=new GUIStyle(text){wordWrap=true,alignment=TextAnchor.UpperLeft,fontSize=13,normal={textColor=Pale}};
  }

  public void Draw(float availableWidth,Health health,Energy energy,AbilityCaster caster,PlayerClassDefinition definition,bool interactive){
   Init();
   if(!health||!caster)return;
   Matrix4x4 oldMatrix=GUI.matrix;Color oldColor=GUI.color;
   float fit=Mathf.Min(1,Mathf.Max(1,availableWidth-24)/Width);
   GUI.matrix=oldMatrix*Matrix4x4.TRS(new Vector3((availableWidth-Width*fit)*.5f,900-(Height+10)*fit,0),Quaternion.identity,new Vector3(fit,fit,1));
   try{
    Color accent=definition&&definition.jetpack?Gold:Cyan;
    Rect playerFrame=new Rect(6,0,248,60),targetFrame=new Rect(266,0,248,60);
    Rect healthTrack=new Rect(playerFrame.x,playerFrame.y,playerFrame.width,32),energyTrack=new Rect(playerFrame.x,playerFrame.y+32,playerFrame.width,28);
    bars.PlayerResources(playerFrame,health,energy,accent);
    var target=caster.Target;
    bool showTarget=target&&target.Alive&&target.team==Team.Hostile&&target.gameObject.activeInHierarchy;
    string targetName="";
    if(showTarget){
     var targetBar=target.GetComponent<EnemyHealthBar>();
     targetName=targetBar?targetBar.displayName:target.name;
     bars.TargetResource(targetFrame,target,targetName);
    }
    // Use the same transform as the slots so the cast bar stays attached when the HUD scales.
    if(caster.IsBusy)bars.Cast(new Rect((Width-420)*.5f,-32,420,24),caster);
    else if(caster.IsAiming){
     Fill(new Rect(20,-32,Width-40,24),new Color(.025f,.06f,.08f,.94f));
     Label(new Rect(24,-32,Width-48,24),caster.AimName+"  ·  LMB: WIRKEN   RMB / ESC: ABBRUCH",10,Cyan);
    }
    Fill(new Rect(3,SlotY-3,Width-6,66),new Color(.015f,.03f,.04f,.65f));
    int hovered=-1;
    for(int i=0;i<8;i++){
     Rect slot=SlotRect(i);
     var ability=caster.abilities!=null&&i<caster.abilities.Length?caster.abilities[i]:null;
     bool hover=interactive&&new Rect(slot.x,slot.y,slot.width,Height-slot.y).Contains(Event.current.mousePosition);
     if(hover)hovered=i;
     DrawSlot(i,slot,ability,caster,energy,hover);
    }
    if(hovered>=0)Tooltip(hovered,caster);
    else if(interactive){
     Vector2 pointer=Event.current.mousePosition;
     if(healthTrack.Contains(pointer))ResourceTooltip("LEBEN  "+Mathf.CeilToInt(health.Current)+" / "+Mathf.CeilToInt(health.maximum)+(health.Shield>0?"   +"+Mathf.CeilToInt(health.Shield)+" SCHILD":""),Cyan);
     else if(energyTrack.Contains(pointer))ResourceTooltip((caster.IsVanguard?"GLEITENERGIE  ":"ENERGIE  ")+Mathf.CeilToInt(energy?energy.Current:0)+" / "+Mathf.CeilToInt(energy?energy.maximum:100),accent);
     else if(showTarget&&targetFrame.Contains(pointer))TargetTooltip(target,targetName);
    }
   }finally{GUI.matrix=oldMatrix;GUI.color=oldColor;}
  }

  static Rect SlotRect(int slot)=>new Rect(6+slot*SlotStep,SlotY,SlotSize,SlotSize);

  void TargetTooltip(Health target,string name){
   string details="HP  "+Mathf.CeilToInt(target.Current)+" / "+Mathf.CeilToInt(target.maximum);
   var resource=target.GetComponent<Energy>();
   if(resource&&resource.maximum>0)details+="\n"+resource.displayName+"  "+Mathf.CeilToInt(resource.Current)+" / "+Mathf.CeilToInt(resource.maximum);
   if(target.Shield>0)details+="  ·  SCHILD "+Mathf.CeilToInt(target.Shield);
   var mark=target.GetComponent<VanguardStatus>();
   if(mark&&mark.MatrixRemaining>0)details+="\nMATRIX "+Mathf.CeilToInt(mark.MatrixRemaining)+"s";
   if(mark&&mark.ExposedRemaining>0)details+="\nSCHADEN +15% "+Mathf.CeilToInt(mark.ExposedRemaining)+"s";
   var ion=target.GetComponent<IonDebuff>();
   if(ion&&ion.Remaining>0)details+="\nION −50% "+Mathf.CeilToInt(ion.Remaining)+"s";
   string content=name+"\n"+details;
   float height=tooltip.CalcHeight(new GUIContent(content),328)+20;
   Rect panel=new Rect(Width-360,-height-10,360,height);
   Fill(panel,new Color(.035f,.065f,.085f,.99f));
   Fill(new Rect(panel.x,panel.y,panel.width,1),Crimson);
   GUI.Label(new Rect(panel.x+16,panel.y+10,panel.width-32,panel.height-20),content,tooltip);
  }

  void ResourceTooltip(string value,Color tint){
   Fill(new Rect(90,-43,Width-180,31),new Color(.035f,.065f,.085f,.99f));
   Fill(new Rect(90,-43,Width-180,1),tint);
   Label(new Rect(98,-41,Width-196,27),value,11,Pale);
  }

  void DrawSlot(int index,Rect rect,AbilityDefinition ability,AbilityCaster caster,Energy energy,bool hover){
   bool ultimate=index==7,occupied=ability;
   const string frame="compact-frame",mask="compact-mask",glow="compact-glow";
   float abilityRemaining=occupied?caster.AbilityCooldownRemaining(index):0;
   float globalRemaining=occupied&&index!=caster.MovementSlot?caster.GlobalRemaining:0;
   float remaining=Mathf.Max(abilityRemaining,globalRemaining),cost=occupied?caster.EnergyCost(index):0;
   bool affordable=!energy||energy.CanSpend(cost),proc=occupied&&caster.SlotHighlighted(index);
   bool active=occupied&&caster.IsBusy&&caster.ActiveAbility==ability;
   Color tint=ultimate?Crimson:Cyan;
   Color state=active?Pale:remaining>0?Crimson:!occupied||!affordable?Muted:tint;
   Texture(frame,rect,Color.white);
   Texture(mask,rect,new Color(tint.r*.12f,tint.g*.12f,tint.b*.12f,.48f));
   if(hover)Texture(mask,rect,new Color(state.r,state.g,state.b,.12f));

   if(occupied){
    const float iconSize=42;
    Rect icon=new Rect(rect.center.x-iconSize*.5f,rect.center.y-iconSize*.5f,iconSize,iconSize);
    Texture(IconName(ability),icon,remaining>0?new Color(state.r,state.g,state.b,.32f):!affordable?new Color(.35f,.47f,.52f,.7f):state);
    if(remaining>0){
     // Clip only the inner panel so the metallic frame and keycap never disappear.
     // Each timer fills the panel against its own duration; the GCD has no numeric label.
     float ratio=Mathf.Clamp01(Mathf.Max(ability.cooldown>0?abilityRemaining/ability.cooldown:0,globalRemaining/AbilityCaster.GlobalCooldown));
     ClippedTexture(mask,rect,ratio,new Color(.03f,.005f,.015f,.78f));
     if(abilityRemaining>0)Label(rect,abilityRemaining>=10?Mathf.CeilToInt(abilityRemaining).ToString():abilityRemaining.ToString("0.0"),20,Pale,true);
    }
   }
   Color edge=state;edge.a=occupied&&(remaining<=0&&affordable||active||proc)?.55f:.2f;
   if(active)edge.a=1;
   if(hover)edge=Color.Lerp(edge,Color.white,.35f);
   Texture(glow,rect,edge);
   if(proc){
    Color halo=new Color(1,.85f,.34f,.8f+.2f*Mathf.Sin(Time.unscaledTime*6));
    Texture("proc-glow",new Rect(rect.x-7.5f,rect.y-7.5f,rect.width+15,rect.height+15),halo);
   }
   Rect key=new Rect(rect.center.x-10,rect.yMax+6,20,17);
   Fill(new Rect(key.x-1,key.y-1,key.width+2,key.height+2),new Color(.23f,.31f,.35f));
   Fill(key,hover?new Color(.18f,.29f,.33f):new Color(.10f,.15f,.18f));
   Label(key,occupied?PlayerInputReader.SlotKeys[index]:"—",11,Pale,true);
  }

  void Tooltip(int index,AbilityCaster caster){
   var ability=caster.abilities!=null&&index<caster.abilities.Length?caster.abilities[index]:null;
   const float width=360;
   string description=ability?ability.description:"Hier ist noch keine Fähigkeit ausgerüstet.";
   float bodyHeight=tooltip.CalcHeight(new GUIContent(description),width-32);
   float height=bodyHeight+91;
   Rect slot=SlotRect(index);
   Rect panel=new Rect(Mathf.Clamp(slot.center.x-width*.5f,0,Width-width),-height-10,width,height);
   Fill(new Rect(panel.x-1,panel.y-1,panel.width+2,panel.height+2),new Color(.36f,.5f,.56f));
   Fill(panel,new Color(.035f,.065f,.085f,.99f));
   Fill(new Rect(panel.x,panel.y,panel.width,3),index==7?Crimson:Cyan);
   Label(new Rect(panel.x+16,panel.y+13,width-32,21),ability?ability.displayName.ToUpperInvariant():"FREIER SLOT",14,Pale,true,TextAnchor.MiddleLeft);
   GUI.Label(new Rect(panel.x+16,panel.y+43,width-32,bodyHeight),description,tooltip);
   string detail=ability?"["+PlayerInputReader.SlotKeys[index]+"]   "+caster.EnergyCost(index).ToString("0")+" EN   /   "+ability.cooldown.ToString("0.#")+" S COOLDOWN":"KEINE FÄHIGKEIT";
   Label(new Rect(panel.x+16,panel.yMax-32,width-32,19),detail,10,Muted,false,TextAnchor.MiddleLeft);
  }

  static string IconName(AbilityDefinition ability){
   if(ability is VanguardAbility v){
    switch(v.skill){
     case VanguardSkill.PulseBurst:return "pulse-burst";
     case VanguardSkill.OverchargeRailgun:return "overcharge-railgun";
     case VanguardSkill.GravitonDash:return "graviton-dash";
     case VanguardSkill.AegisSupplyDrone:return "supply-drone";
     case VanguardSkill.ConcussiveBlast:return "concussive-blast";
     case VanguardSkill.TractorBeam:return "tractor-beam";
     case VanguardSkill.TargetingMatrix:return "targeting-matrix";
     case VanguardSkill.OrbitalKineticStrike:return "orbital-strike";
    }
   }
   if(ability is LaserAbility laser)return laser.twinProjectiles?"twin-pulses":"pulse-burst";
   if(ability is DashAbility)return "jet-burst";
   if(ability is GrenadeAbility grenade)return grenade.forwardThrow?"repulsor-grenade":"ion-grenade";
   if(ability is KickAbility)return "pulse-kick";
   if(ability is ChargedShotAbility)return "charged-shot";
   if(ability is ExplosiveShotAbility)return "explosive-shot";
   if(ability is ShieldAbility)return "aegis-shield";
   if(ability is UltimateAbility)return "overdrive";
   return "orbital-strike";
  }

  void Texture(string name,Rect rect,Color tint){
   if(!art.TryGetValue(name,out var texture))return;
   GUI.color=tint;GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,true);GUI.color=Color.white;
  }
  void ClippedTexture(string name,Rect rect,float fraction,Color tint){
   if(!art.TryGetValue(name,out var texture)||fraction<=0)return;
   GUI.color=tint;
   GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,rect.width,rect.height*fraction),texture,new Rect(0,1-fraction,1,fraction));
   GUI.color=Color.white;
  }
  void Label(Rect rect,string value,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleCenter){
   var style=bold?heavy:text;style.fontSize=size;style.alignment=alignment;style.normal.textColor=color;GUI.Label(rect,value,style);
  }
  static void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
  public void Dispose(){art.Clear();initialized=false;}
 }
}

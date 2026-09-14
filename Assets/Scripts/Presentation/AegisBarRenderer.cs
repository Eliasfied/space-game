using UnityEngine;
using System.Collections.Generic;

namespace AsterionGame {
 // Resource panels and subdued cast indicators share the Aegis palette.
 public sealed class AegisBarRenderer {
  public static readonly Color Cyan=new Color(.16f,.94f,.91f),Crimson=new Color(1,.25f,.36f),Gold=new Color(1,.74f,.32f);
  static readonly Color CastYellow=new Color(.94f,.75f,.16f,.94f);
  static readonly Color Pale=new Color(.86f,.94f,.96f),Ink=new Color(.025f,.06f,.08f);
  Texture2D frame;
  Texture2D resourceStackFrame,bossFrame;
  readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
  GUIStyle label;

  void Init(){
   if(label!=null)return;
   frame=Resources.Load<Texture2D>("UI/Aegis/resource-frame");
   resourceStackFrame=Resources.Load<Texture2D>("UI/Aegis/resource-stack-frame");
   bossFrame=Resources.Load<Texture2D>("UI/Aegis/boss-frame");
   label=new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),fontStyle=FontStyle.Bold,clipping=TextClipping.Clip};
  }

  public void PartyMember(Rect rect,string name,string role,string icon,float health,float maximumHealth,float energy,float maximumEnergy,Color energyTint){
   Init();Color old=GUI.color;
   try{
    Frame(rect,Cyan,resourceStackFrame);
    if(!icons.TryGetValue(icon,out var emblem)){emblem=Resources.Load<Texture2D>("UI/Aegis/"+icon);icons[icon]=emblem;}
    if(emblem){GUI.color=Cyan;GUI.DrawTexture(new Rect(rect.x+11,rect.y+8,23,23),emblem);GUI.color=Color.white;}
    Label(new Rect(rect.x+42,rect.y+6,rect.width-52,14),Ellipsis(name,rect.width-52,11),11,Pale,TextAnchor.MiddleLeft);
    Label(new Rect(rect.x+42,rect.y+20,rect.width-52,11),role,8,new Color(.61f,.74f,.78f),TextAnchor.MiddleLeft);
    float hpFraction=Mathf.Clamp01(health/Mathf.Max(1,maximumHealth));
    Rect hp=new Rect(rect.x+10,rect.y+34,rect.width-20,12);
    Track(hp,hpFraction,Cyan,false);
    OutlinedLabel(hp,Mathf.CeilToInt(health)+" / "+Mathf.CeilToInt(maximumHealth)+"  ·  "+Mathf.RoundToInt(hpFraction*100)+"%",9,Pale,TextAnchor.MiddleCenter);
    Track(new Rect(rect.x+10,rect.y+51,rect.width-20,5),energy/Mathf.Max(1,maximumEnergy),energyTint,false);
   }finally{GUI.color=old;}
  }

  public void DamageMeter(Rect rect,float damage,float dps){
   Init();Color old=GUI.color;
   try{
    Frame(rect,Cyan,resourceStackFrame);
    float columnWidth=(rect.width-32)*.5f;
    Label(new Rect(rect.x+12,rect.y+7,columnWidth,15),"SCHADEN GESAMT",9,new Color(.61f,.74f,.78f),TextAnchor.MiddleLeft);
    Label(new Rect(rect.center.x+4,rect.y+7,columnWidth,15),"DPS",9,Cyan,TextAnchor.MiddleRight);
    Fill(new Rect(rect.center.x,rect.y+8,1,rect.height-16),new Color(.32f,.44f,.49f,.4f));
    Label(new Rect(rect.x+12,rect.y+31,columnWidth,20),damage.ToString("N0"),16,Pale,TextAnchor.MiddleLeft);
    Label(new Rect(rect.center.x+4,rect.y+31,columnWidth,20),dps.ToString("N1"),16,Cyan,TextAnchor.MiddleRight);
   }finally{GUI.color=old;}
  }

  public void SmallHealth(Rect rect,Health health,Color tint,bool showValues=false){
   if(!health)return;
   Init();Color old=GUI.color;
   try{
    float fraction=health.maximum>0?Mathf.Clamp01(health.Current/health.maximum):0;
    Fill(rect,new Color(.36f,.46f,.51f,.9f));
    Rect track=new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2);
    Fill(track,Ink);Fill(new Rect(track.x,track.y,track.width*fraction,track.height),tint);
    if(fraction>0)Fill(new Rect(track.x,track.y,track.width*fraction,1),Color.Lerp(tint,Color.white,.35f));
    string value=(showValues?Mathf.CeilToInt(health.Current)+" / "+Mathf.CeilToInt(health.maximum)+"  ·  ":"")+Mathf.RoundToInt(fraction*100)+"%";
    OutlinedLabel(rect,value,showValues?10:8,Pale,TextAnchor.MiddleCenter);
   }finally{GUI.color=old;}
  }

  public void PlayerResources(Rect rect,Health health,Energy energy,Color energyTint){
   if(!health)return;
   Init();Color old=GUI.color;
   try{
    Color healthTint=health.Current<health.maximum*.25f?Crimson:Cyan;
    Frame(rect,healthTint,resourceStackFrame);
    ResourceRow(new Rect(rect.x+10,rect.y+6,rect.width-20,22),health.Current,health.maximum,healthTint,"HP");
    ResourceRow(new Rect(rect.x+10,rect.y+32,rect.width-20,22),energy?energy.Current:0,energy?energy.maximum:100,energyTint,"ENERGIE");
   }finally{GUI.color=old;}
  }

  public void TargetResource(Rect rect,Health health,string name){
   if(!health)return;
   Init();Color old=GUI.color;
   try{
    var energy=health.GetComponent<Energy>();
    bool hasEnergy=energy&&energy.maximum>0;
    Frame(rect,Crimson,resourceStackFrame);
    ResourceRow(new Rect(rect.x+10,rect.y+6,rect.width-20,22),health.Current,health.maximum,Crimson,name.ToUpperInvariant(),88);
    if(hasEnergy)ResourceRow(new Rect(rect.x+10,rect.y+32,rect.width-20,22),energy.Current,energy.maximum,energy.displayColor,string.IsNullOrWhiteSpace(energy.displayName)?"ENERGIE":energy.displayName.ToUpperInvariant());
   }finally{GUI.color=old;}
  }

  void ResourceRow(Rect rect,float current,float maximum,Color tint,string title,float titleWidth=55){
   float fraction=maximum>0?Mathf.Clamp01(current/maximum):0;
   string value=Mathf.CeilToInt(current)+" / "+Mathf.CeilToInt(maximum)+"  ·  "+Mathf.RoundToInt(fraction*100)+"%";
   Label(new Rect(rect.x,rect.y,titleWidth-4,13),Ellipsis(title,titleWidth-4,9),9,tint,TextAnchor.MiddleLeft);
   Label(new Rect(rect.x+titleWidth,rect.y,rect.width-titleWidth,13),value,11,Pale,TextAnchor.MiddleRight);
   Track(new Rect(rect.x,rect.yMax-8,rect.width,8),fraction,tint,true);
  }

  public void Boss(Rect rect,Health health,string name,int phase){
   if(!health)return;
   Init();Color old=GUI.color;
   try{
    float fraction=health.maximum>0?Mathf.Clamp01(health.Current/health.maximum):0;
    OutlinedLabel(new Rect(rect.x+12,rect.y,rect.width-24,17),Ellipsis(name.ToUpperInvariant(),rect.width-24,12),12,Pale,TextAnchor.MiddleCenter);
    Rect shell=new Rect(rect.x,rect.y+20,rect.width,22);
    GUI.color=Color.white;
    if(bossFrame)GUI.DrawTexture(shell,bossFrame,ScaleMode.StretchToFill,true);else Frame(shell,Crimson);
    Rect track=new Rect(shell.x+10,shell.y+7,shell.width-20,8);
    Track(track,fraction,new Color(.9f,.13f,.2f),false);
    // The current encounter changes phase at half health.
    Fill(new Rect(track.center.x,track.y-2,1,track.height+4),new Color(.62f,.65f,.65f,.65f));
    Color secondary=new Color(.65f,.72f,.75f);
    OutlinedLabel(new Rect(rect.x+10,rect.y+42,62,13),"PHASE "+phase,9,secondary,TextAnchor.MiddleLeft);
    OutlinedLabel(new Rect(rect.xMax-62,rect.y+42,52,13),Mathf.RoundToInt(fraction*100)+"%",9,Pale,TextAnchor.MiddleRight);
   }finally{GUI.color=old;}
  }

  public void Cast(Rect rect,AbilityCaster caster,bool compact=false){
   if(!caster||!caster.IsBusy)return;
   var ability=caster.ActiveAbility;
   float duration=ability?Mathf.Max(0,caster.CastDuration):0;
   Cast(rect,caster.CastName,caster.CastProgress,duration,caster.CastRemaining,compact);
  }

  public void Cast(Rect rect,string name,float progress,float duration,float remaining,bool compact=false){
   Init();Color old=GUI.color;FontStyle oldStyle=label.fontStyle;
   try{
    FilledCast(rect,name,progress,duration,remaining,compact);
   }finally{GUI.color=old;label.fontStyle=oldStyle;}
  }

  void FilledCast(Rect rect,string castName,float progress,float duration,float remaining,bool compact){
   // The full inner width represents the cast duration; both labels overlay the same track.
   float timeWidth=compact?38:82,padding=compact?3:7;
   int nameSize=compact?8:12,timeSize=compact?8:11;
   Color yellow=CastYellow;
   Fill(rect,new Color(.48f,.43f,.24f,.75f));
   Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),new Color(.025f,.035f,.04f,.95f));
   Rect track=new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4);
   Fill(track,new Color(.14f,.115f,.04f,.9f));
   float fill=track.width*Mathf.Clamp01(progress);
   if(fill>0){
    Fill(new Rect(track.x,track.y,fill,track.height),yellow);
    Fill(new Rect(track.x,track.center.y,fill,track.height*.5f),new Color(.22f,.13f,0,.2f));
    Fill(new Rect(track.x,track.y,fill,1),new Color(1,.91f,.46f));
    Fill(new Rect(track.x+Mathf.Max(0,fill-1),track.y,Mathf.Min(1,fill),track.height),new Color(1,.91f,.46f));
   }
   label.fontStyle=FontStyle.Bold;
   Rect nameRect=new Rect(track.x+padding,track.y,track.width-timeWidth-padding*2,track.height);
   string name=Ellipsis(castName,nameRect.width,nameSize);
   OutlinedLabel(nameRect,name,nameSize,new Color(1,.99f,.9f),TextAnchor.MiddleLeft);
   float elapsed=Mathf.Clamp(duration-remaining,0,Mathf.Max(0,duration));
   OutlinedLabel(new Rect(rect.xMax-timeWidth,rect.y+2,timeWidth-(compact?4:8),rect.height-4),elapsed.ToString("0.0")+(compact?"/":" / ")+duration.ToString("0.0"),timeSize,Pale,TextAnchor.MiddleRight);
  }

  void OutlinedLabel(Rect rect,string value,int size,Color tint,TextAnchor alignment){
   // Keep name and timing readable as the yellow fill passes behind them.
   Color shadow=new Color(.025f,.03f,.015f,.9f);
   for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
    Label(new Rect(rect.x+x,rect.y+y,rect.width,rect.height),value,size,shadow,alignment);
   Label(rect,value,size,tint,alignment);
  }

  void Frame(Rect rect,Color tint,Texture2D artwork=null){
   GUI.color=Color.white;
   var texture=artwork?artwork:frame;
   if(texture)GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,true);
   else{
    Fill(rect,new Color(.35f,.45f,.5f));
    Fill(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),Ink);
   }
   Fill(new Rect(rect.x+12,rect.y+2,32,1),tint);
  }

  static void Track(Rect rect,float progress,Color tint,bool segmented){
   Fill(rect,Ink);
   float filled=rect.width*Mathf.Clamp01(progress);
   if(filled>0){
    Fill(new Rect(rect.x,rect.y,filled,rect.height),tint);
    Fill(new Rect(rect.x,rect.y+rect.height*.625f,filled,rect.height*.375f),new Color(0,0,0,.23f));
    Fill(new Rect(rect.x,rect.y,filled,1),Color.Lerp(tint,Color.white,.5f));
    if(!segmented)Fill(new Rect(rect.x+Mathf.Max(0,filled-1),rect.y,Mathf.Min(1,filled),rect.height),Color.Lerp(tint,Color.white,.65f));
   }
   if(segmented)for(int i=1;i<4;i++)Fill(new Rect(rect.x+rect.width*i/4,rect.y,1,rect.height),new Color(.02f,.05f,.07f,.6f));
  }

  string Ellipsis(string value,float width,int size){
   label.fontSize=size;
   if(label.CalcSize(new GUIContent(value)).x<=width)return value;
   while(value.Length>0&&label.CalcSize(new GUIContent(value+"…")).x>width)value=value.Substring(0,value.Length-1);
   return value+"…";
  }
  void Label(Rect rect,string value,int size,Color tint,TextAnchor alignment){
   GUI.color=Color.white;label.fontSize=size;label.normal.textColor=tint;label.alignment=alignment;GUI.Label(rect,value,label);
  }
  static void Fill(Rect rect,Color tint){GUI.color=tint;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
 }
}

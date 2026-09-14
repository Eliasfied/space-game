using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 public sealed class Hud:MonoBehaviour {
  public Health player,boss;public BossBrain brain;public AbilityCaster caster;public PlayerInputReader input;public PlayerMotor motor;public GameSession session;
  GUIStyle regular,bold,small;Color cyan=>session&&session.ActiveClass?session.ActiveClass.accent:new Color(.32f,.91f,.94f);static readonly Color amber=new Color(1,.48f,.23f),muted=new Color(.49f,.63f,.67f),paper=new Color(.87f,.93f,.92f),panel=new Color(.027f,.06f,.075f,.93f);
  struct Damage {public Vector3 at;public float value,time;public bool player;}
  static readonly List<Damage> numbers=new List<Damage>();float hitFlash,lastHealth;float width,scale;
  public static void AddDamage(Vector3 at,float value,bool isPlayer){numbers.Add(new Damage{at=at+new Vector3(Random.Range(-.35f,.35f),0,0),value=value,time=Time.time,player=isPlayer});}
  void Start(){numbers.Clear();lastHealth=player.Current;}
  void Update(){if(player.Current<lastHealth)hitFlash=.22f;lastHealth=player.Current;hitFlash=Mathf.Max(0,hitFlash-Time.unscaledDeltaTime);numbers.RemoveAll(n=>Time.time-n.time>1.1f);}
  void Init(){if(regular!=null)return;var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");regular=new GUIStyle{font=font,fontSize=14,normal={textColor=paper},alignment=TextAnchor.MiddleLeft};bold=new GUIStyle(regular){fontStyle=FontStyle.Bold};small=new GUIStyle(regular){fontSize=11,normal={textColor=muted}};}
  void OnGUI(){
   if(session.IsSelecting)return;
   Init();scale=Screen.height/900f;width=Screen.width/scale;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
   minimap.Draw(new Rect(width-252,24,228,244),player,boss,caster.Target,session.Elapsed);
   BossBar();Bottom();Floating();DamageMeter();
   PartyFrames();
   PlayerBars();
   if(Time.time<caster.MessageUntil)Label(width/2-250,596,500,25,caster.Message,12,amber,true,TextAnchor.MiddleCenter);
   if(hitFlash>0){Color c=new Color(1,.15f,.03f,hitFlash*.8f);RectFill(0,0,8,900,c);RectFill(width-8,0,8,900,c);}
   if(session.Paused)Overlay(false);else if(session.Ended)Overlay(true);
   GUI.matrix=Matrix4x4.identity;
  }
  void DamageMeter(){
   unitBars.DamageMeter(new Rect(width-272,639,248,60),caster.Statistics.OverallDamage,caster.Statistics.Dps);
  }
  void BossBar(){
   if(!boss||!brain||!brain.Engaged||!boss.Alive||!boss.gameObject.activeInHierarchy||session.Ended)return;
   var bar=boss.GetComponent<EnemyHealthBar>();
   float bossWidth=Mathf.Min(560,Mathf.Max(220,width-700));
   Rect frame=new Rect((width-bossWidth)*.5f,24,bossWidth,55);
   unitBars.Boss(frame,boss,bar?bar.displayName:"ASTERION",brain.Phase);
   if(brain.IsCasting)unitBars.Cast(new Rect(frame.x+10,frame.yMax+3,frame.width-20,18),brain.CastName,brain.CastProgress,brain.CastDuration,brain.CastRemaining);
  }
  void PartyFrames(){
   // UI-only preview data: no dummy actor is added to the combat scene.
   bool partnerIsVanguard=session.ActiveClass&&session.ActiveClass.jetpack;
   float partyY=width<1016?660:810;
   unitBars.PartyMember(new Rect(24,partyY,208,64),"NOVA",partnerIsVanguard?"VANGUARD":"BOUNTY HUNTER",partnerIsVanguard?"aegis-shield":"twin-pulses",840,1000,72,100,partnerIsVanguard?AegisBarRenderer.Cyan:AegisBarRenderer.Gold);
  }
  readonly SciFiActionBar actionBar=new SciFiActionBar();
  readonly AegisMinimap minimap=new AegisMinimap();
  readonly AegisBarRenderer unitBars=new AegisBarRenderer();Energy energy;
  void Bottom(){if(!energy)energy=player.GetComponent<Energy>();actionBar.Draw(width,player,energy,caster,session.ActiveClass,!session.Paused&&!session.Ended);}
  void OnDestroy(){actionBar.Dispose();}
  void Floating(){
   if(!Camera.main)return;
   foreach(var n in numbers){float age=Time.time-n.time;Vector3 p=Camera.main.WorldToScreenPoint(n.at+Vector3.up*age*.85f);if(p.z<0)continue;Color color=n.player?amber:paper;color.a=1-age/1.1f;Label(p.x/scale-45,(Screen.height-p.y)/scale,90,30,n.value.ToString("0"),n.value>=100?26:18,color,true,TextAnchor.MiddleCenter);}
   foreach(var enemy in EnemyHealthBar.All){
    if(!enemy || !enemy.Health || enemy.Health.team!=Team.Hostile || !enemy.Health.Alive)continue;
    Vector3 point=Camera.main.WorldToViewportPoint(enemy.WorldPosition+Vector3.up*.4f);
    if(point.z<=0 || point.x<0 || point.x>1 || point.y<0 || point.y>1)continue;
    float x=point.x*width,y=(1-point.y)*900,w=enemy.Health==boss?112:82;
    unitBars.SmallHealth(new Rect(x-w/2,y,w,12),enemy.Health,AegisBarRenderer.Crimson);
    var ion=enemy.GetComponent<IonDebuff>();if(ion)Label(x-70,y-20,140,18,"ION / −50%  "+Mathf.CeilToInt(ion.Remaining)+"s",10,new Color(.3f,1,.6f),false,TextAnchor.MiddleCenter);
   }
  }
  struct StatusBadge {public string text;public Color color;}
  readonly List<StatusBadge> statuses=new List<StatusBadge>();Transform statusVisual,statusHead;
  void PlayerBars(){
   if(!player.Alive||!Camera.main)return;
   if(statusVisual!=motor.visual){
    statusVisual=motor.visual;statusHead=null;
    var rig=statusVisual?statusVisual.GetComponentInChildren<Animator>():null;
    if(rig&&rig.isHuman)statusHead=rig.GetBoneTransform(HumanBodyBones.Head);
   }
   Vector3 at=statusHead?statusHead.position+Vector3.up*.95f:player.transform.position+Vector3.up*3.55f;
   Vector3 point=Camera.main.WorldToViewportPoint(at);
   if(point.z<=0||point.x<0||point.x>1||point.y<0||point.y>1)return;
   // Small, lightly framed HP bar with its percentage inside the green fill area.
   float x=Mathf.Clamp(point.x*width,94,width-94),y=Mathf.Max(4,(1-point.y)*900-32);
   float hpFraction=player.maximum>0?Mathf.Clamp01(player.Current/player.maximum):0;
   Color hpTint=new Color(.58f,.88f,.4f,.9f);
   Rect hpRect=new Rect(x-50,y+18,100,14);
   RectFill(hpRect.x,hpRect.y,hpRect.width,hpRect.height,new Color(.3f,.42f,.3f,.8f));
   RectFill(x-49,y+19,98,12,new Color(.025f,.065f,.035f,.85f));
   RectFill(x-49,y+19,98*hpFraction,12,hpTint);
   if(hpFraction>0)RectFill(x-49,y+19,98*hpFraction,1,new Color(.76f,1,.61f,.8f));
   string hpPercent=Mathf.RoundToInt(hpFraction*100)+"%";
   Label(x-48,y+19,98,14,hpPercent,9,new Color(.015f,.04f,.02f,.95f),true,TextAnchor.MiddleCenter);
   Label(x-49,y+18,98,14,hpPercent,9,new Color(.96f,1,.93f),true,TextAnchor.MiddleCenter);
   if(caster.IsBusy)unitBars.Cast(new Rect(hpRect.x,hpRect.yMax,hpRect.width,17),caster,true);
   statuses.Clear();
   if(player.DamageReduction>0)Status("SCHILD "+player.ProtectionRemaining.ToString("0.0")+"s",new Color(.3f,.75f,1));
   if(caster.ChargedProc)Status("PROC · "+PlayerInputReader.SlotKeys[caster.ProcSlot],new Color(1,.72f,.16f));
   if(caster.KickResetReady)Status("KICK · 4",amber);
   var overdrive=player.GetComponent<HunterOverdrive>();if(overdrive&&overdrive.Remaining>0)Status("OVERDRIVE "+Mathf.CeilToInt(overdrive.Remaining)+"s",amber);
   if(player.Shield>0)Status("AEGIS "+Mathf.CeilToInt(player.Shield),cyan);
   var vanguardStatus=player.GetComponent<VanguardStatus>();
   if(vanguardStatus){if(vanguardStatus.ImmunityRemaining>0)Status("CC IMMUN "+Mathf.CeilToInt(vanguardStatus.ImmunityRemaining)+"s",cyan);if(vanguardStatus.BoostRemaining>0)Status("DMG +30%",amber);}
   if(player.Invulnerable)Status("IMMUN",new Color(.4f,.95f,1));
   var cc=player.GetComponent<CrowdControl>();
   if(cc){
    if(cc.IsStunned)Status("STUN "+cc.StunRemaining.ToString("0.0")+"s",amber);
    if(cc.SlowRemaining>0&&cc.MovementMultiplier<1)Status("SLOW "+Mathf.CeilToInt(cc.SlowRemaining)+"s",new Color(1,.35f,.4f));
    if(cc.IsKnockedBack)Status("RÜCKSTOSS",amber);
   }
   var ion=player.GetComponent<IonDebuff>();if(ion&&ion.Remaining>0)Status("ION "+Mathf.CeilToInt(ion.Remaining)+"s",new Color(.65f,1,.25f));
   for(int i=0;i<statuses.Count;i++){
    int row=i/3,col=i%3,count=Mathf.Min(3,statuses.Count-row*3);float bx=x-count*74*.5f+col*74,by=y-25-row*24;
    RectFill(bx,by,70,20,panel);RectFill(bx,by+18,70,2,statuses[i].color);
    Label(bx,by,70,18,statuses[i].text,9,statuses[i].color,true,TextAnchor.MiddleCenter);
   }
  }
  void Status(string text,Color color){statuses.Add(new StatusBadge{text=text,color=color});}
  void Overlay(bool ended){
   RectFill(0,0,width,900,new Color(.01f,.025f,.035f,.84f));float x=width/2-255;
   RectFill(x,270,510,320,panel);RectFill(x,270,510,3,ended&&!session.Won?amber:cyan);
   Label(x+35,300,440,24,ended?"ENCOUNTER REPORT":"FIELD OPERATIONS",11,cyan,true);
   Label(x+35,345,440,54,ended?(session.Won?"CORE SECURED":"SIGNAL LOST"):"PAUSED",38,paper,true);
   Label(x+35,410,440,50,ended?(session.Won?"Prüfung bestanden. Der Weg zum Hauptkern führt weiter.":"Orange Flächen verlassen. 2 schützt beim Ausweichen."):"Deine Mission wartet.",14,muted);
   if(Button(new Rect(x+35,490,215,50),ended?"R  /  KLASSENWAHL":"MISSION FORTSETZEN")){if(ended)session.Restart();else session.TogglePause();}
   if(Button(new Rect(x+267,490,207,50),"BEENDEN")){
    #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying=false;
    #else
    Application.Quit();
    #endif
   }
  }
  bool Button(Rect r,string s){bool hover=r.Contains(Event.current.mousePosition);RectFill(r.x,r.y,r.width,r.height,hover?new Color(.12f,.32f,.36f):new Color(.08f,.19f,.22f));Label(r.x,r.y,r.width,r.height,s,11,paper,true,TextAnchor.MiddleCenter);return GUI.Button(r,GUIContent.none,GUIStyle.none);}
  void Label(float x,float y,float w,float h,string text,int size,Color color,bool heavy=false,TextAnchor alignment=TextAnchor.MiddleLeft){var style=heavy?bold:regular;style.fontSize=size;style.normal.textColor=color;style.alignment=alignment;GUI.Label(new Rect(x,y,w,h),text,style);}
  void RectFill(float x,float y,float w,float h,Color color){GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
 }
}

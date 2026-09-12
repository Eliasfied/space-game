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
   RectFill(0,0,width,3,cyan);Label(35,27,280,28,"T H E  C R U C I B L E",19,paper,true);Label(36,58,300,18,"GLADIATOREN-ARCHE / KAMMER 07",10,muted);
   RectFill(35,91,3,63,cyan);Label(49,88,240,18,"SIMULATION / REKRUTIERUNG",10,cyan,true);Label(49,111,240,21,brain.Engaged?"Besiege den simulierten Wächter.":"Betritt die Prüfung der Rekrutierungs-KI.",13,paper);Label(49,136,240,18,"ZIEL: HAUPTKERN ERREICHEN",10,muted);
   Label(width-205,31,170,18,"SYSTEMS ONLINE   •   01",10,cyan,false,TextAnchor.MiddleRight);Label(width-205,55,170,18,System.TimeSpan.FromSeconds(session.Elapsed).ToString(@"mm\:ss")+"  /  FIELD TEST",11,muted,false,TextAnchor.MiddleRight);
   BossBar();Bottom();Floating();DamageMeter();
   TargetBar();
   if(caster.IsBusy)CastBar(width/2-220,628,440,32,16);
   PlayerBars();
   if(Time.time<caster.MessageUntil)Label(width/2-250,596,500,25,caster.Message,12,amber,true,TextAnchor.MiddleCenter);
   if(hitFlash>0){Color c=new Color(1,.15f,.03f,hitFlash*.8f);RectFill(0,0,8,900,c);RectFill(width-8,0,8,900,c);}
   if(session.Paused)Overlay(false);else if(session.Ended)Overlay(true);
   GUI.matrix=Matrix4x4.identity;
  }
  void DamageMeter(){
   float x=width-301,y=639;
   RectFill(x,y,266,48,panel);RectFill(x,y,2,48,cyan);
   Label(x+12,y+4,242,15,"LIVE DAMAGE",10,muted,true);
   Label(x+12,y+20,242,23,"Overall Damage  "+caster.Statistics.OverallDamage.ToString("N0")+" ("+caster.Statistics.Dps.ToString("0.0")+" DPS)",12,paper,true,TextAnchor.MiddleRight);
  }
  void TargetBar(){
   float x=width/2-245;var target=caster.Target;
   RectFill(x-18,24,526,91,panel);RectFill(x-18,24,3,91,cyan);
   if(!target){Label(x,34,490,28,"KEIN ZIEL",15,muted,true);Label(x,76,490,22,"TAB / LINKSKLICK: ZIEL WÄHLEN",11,cyan);return;}
   var bar=target.GetComponent<EnemyHealthBar>();
   Label(x,34,426,24,bar?bar.displayName:target.name,15,paper,true);
   Label(x+426,35,64,20,"ZIEL",11,cyan,false,TextAnchor.MiddleRight);
   RectFill(x,70,490,8,new Color(.13f,.19f,.2f));
   RectFill(x,70,490*Mathf.Clamp01(target.Current/Mathf.Max(1,target.maximum)),8,new Color(.93f,.12f,.18f));
   for(int i=1;i<10;i++)RectFill(x+49*i,70,2,8,panel);
   var mark=target.GetComponent<VanguardStatus>();
   if(mark&&(mark.MatrixRemaining>0||mark.ExposedRemaining>0))Label(x,105,490,20,(mark.MatrixRemaining>0?"MATRIX "+Mathf.CeilToInt(mark.MatrixRemaining)+"s   ":"")+(mark.ExposedRemaining>0?"SCHADEN +15% "+Mathf.CeilToInt(mark.ExposedRemaining)+"s":""),10,cyan);
   var ion=target.GetComponent<IonDebuff>();
   if(ion)Label(x,86,320,17,"ION / −50%  "+Mathf.CeilToInt(ion.Remaining)+"s",10,new Color(.3f,1,.6f));
   Label(x+340,86,150,17,Mathf.CeilToInt(target.Current)+" / "+target.maximum.ToString("0"),10,paper,false,TextAnchor.MiddleRight);
  }
  void BossBar(){
   float x=width-285;RectFill(x,90,250,76,panel);RectFill(x,90,3,76,amber);
   Label(x+10,93,230,22,"ASTERION / BOSS",12,amber,true);
   RectFill(x+10,120,230,7,new Color(.13f,.19f,.2f));
   RectFill(x+10,120,230*Mathf.Clamp01(boss.Current/Mathf.Max(1,boss.maximum)),7,new Color(.93f,.12f,.18f));
   Label(x+10,132,110,15,Mathf.CeilToInt(boss.Current)+" / "+boss.maximum.ToString("0"),10,paper);
   Label(x+125,132,115,15,"PHASE 0"+brain.Phase,10,muted,false,TextAnchor.MiddleRight);
   Label(x+10,149,230,15,brain.Engaged?brain.AttackName:"DORMANT",9,muted);
   if(brain.Engaged&&brain.AttackProgress>0&&boss.Alive)RectFill(x,166,250*brain.AttackProgress,2,amber);
  }
  readonly SciFiActionBar actionBar=new SciFiActionBar();Energy energy;
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
    RectFill(x-w/2-2,y-2,w+4,10,new Color(.025f,.018f,.022f,.95f));
    RectFill(x-w/2,y,w,6,new Color(.22f,.035f,.045f));
    RectFill(x-w/2,y,w*Mathf.Clamp01(enemy.Health.Current/Mathf.Max(1,enemy.Health.maximum)),6,new Color(.93f,.12f,.18f));
    var ion=enemy.GetComponent<IonDebuff>();if(ion)Label(x-70,y-20,140,18,"ION / −50%  "+Mathf.CeilToInt(ion.Remaining)+"s",10,new Color(.3f,1,.6f),false,TextAnchor.MiddleCenter);
   }
  }
  void CastBar(float x,float y,float w,float h,int fontSize){
   RectFill(x-2,y-2,w+4,h+4,new Color(.015f,.035f,.05f,.95f));
   RectFill(x,y,w,h,new Color(.055f,.13f,.17f,.95f));
   RectFill(x,y,w*caster.CastProgress,h,new Color(cyan.r*.5f,cyan.g*.5f,cyan.b*.5f,.95f));
   Label(x+7,y,w-54,h,caster.CastName,fontSize,paper,true);
   Label(x+w-49,y,42,h,caster.CastRemaining.ToString("0.0")+" s",fontSize,paper,true,TextAnchor.MiddleRight);
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
   float x=point.x*width,y=(1-point.y)*900;
   RectFill(x-57,y-2,114,16,panel);RectFill(x-55,y,110,12,new Color(.035f,.15f,.1f));
   RectFill(x-55,y,110*Mathf.Clamp01(player.Current/Mathf.Max(1,player.maximum)),12,new Color(.14f,.65f,.4f));
   Label(x-55,y,110,12,Mathf.CeilToInt(player.Current)+" / "+player.maximum.ToString("0"),9,paper,true,TextAnchor.MiddleCenter);
   if(caster.IsBusy)CastBar(x-55,y+18,110,17,8);
   statuses.Clear();
   if(player.DamageReduction>0)Status("SCHILD "+player.ProtectionRemaining.ToString("0.0")+"s",new Color(.3f,.75f,1));
   if(caster.ChargedProc)Status("PROC · "+PlayerInputReader.SlotKeys[caster.ProcSlot],new Color(1,.72f,.16f));
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

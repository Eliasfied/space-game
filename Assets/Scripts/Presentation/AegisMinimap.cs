using UnityEngine;

namespace AsterionGame {
 // The same authored room data drives the minimap and the enemy movement boundary.
 public sealed class AegisMinimap {
  Texture2D frame,dot,arrow,bossIcon;GUIStyle text;
  readonly Color cyan=new Color(.22f,.95f,.93f),red=new Color(1,.24f,.31f),pale=new Color(.8f,.88f,.9f);
  Rect map;Vector3 center;float pixelsPerMeter;
  void Init(){
   if(text!=null)return;
   frame=Resources.Load<Texture2D>("UI/Aegis/minimap-frame");dot=Resources.Load<Texture2D>("UI/Aegis/minimap-dot");
   arrow=Resources.Load<Texture2D>("UI/Aegis/minimap-player");bossIcon=Resources.Load<Texture2D>("UI/Aegis/minimap-boss");
   text=new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),fontStyle=FontStyle.Bold,clipping=TextClipping.Clip};
  }
  public void Draw(Rect panel,Health player,Health boss,Health target,float elapsed){
   var level=AegisLevel.Instance;if(!level||level.Layout==null||!player)return;
   Init();Color old=GUI.color;Matrix4x4 matrix=GUI.matrix;
   try{
    if(frame)Texture(panel,frame,Color.white);else Fill(panel,new Color(.025f,.055f,.075f,.96f));
    Label(new Rect(panel.x+13,panel.y+7,150,16),"AEGIS / SEKTOR 07",10,pale);
    Label(new Rect(panel.xMax-38,panel.y+7,24,16),"N ↑",10,cyan,TextAnchor.MiddleRight);
    map=new Rect(0,0,panel.width-20,panel.height-56);center=player.transform.position;
    pixelsPerMeter=map.height/42;
    GUI.BeginGroup(new Rect(panel.x+10,panel.y+28,map.width,map.height));
    try{
     Fill(map,new Color(.035f,.065f,.08f,.98f));
     foreach(var room in level.Layout.rooms){
      Rect r=WorldRect(room);Fill(r,new Color(.19f,.25f,.29f));Outline(r,new Color(.51f,.6f,.63f),1.25f);
     }
     foreach(var cover in level.Layout.covers){Rect r=WorldRect(cover);Fill(r,new Color(.075f,.12f,.15f));Outline(r,new Color(.52f,.4f,.27f),1);}
     Vector2 gate=Point(new Vector3(0,0,level.Layout.gateZ));
     Fill(new Rect(gate.x-4*pixelsPerMeter,gate.y-1,8*pixelsPerMeter,2),level.GateOpen?cyan:red);
     foreach(var enemy in EnemyHealthBar.All){
      if(!enemy||!enemy.isActiveAndEnabled||!enemy.Health||!enemy.Health.Alive||enemy.Health.team!=Team.Hostile||enemy.Health==boss)continue;
      Vector2 p=Point(enemy.transform.position);if(!map.Contains(p))continue;
      if(enemy.Health==target)Marker(p,10,dot,new Color(1,.81f,.32f));
      Marker(p,6,dot,red);
     }
     if(boss&&boss.Alive&&boss.gameObject.activeInHierarchy){
      Vector2 p=Point(boss.transform.position);
      p.x=Mathf.Clamp(p.x,8,map.width-8);p.y=Mathf.Clamp(p.y,8,map.height-8);
      if(boss==target)Marker(p,15,bossIcon,new Color(1,.81f,.32f));
      Marker(p,11,bossIcon,red);
     }
     Vector2 playerAt=Point(player.transform.position);
     Matrix4x4 beforeArrow=GUI.matrix;
     GUIUtility.RotateAroundPivot(player.transform.eulerAngles.y,playerAt);
     Marker(playerAt,15,arrow,cyan);GUI.matrix=beforeArrow;
    }finally{GUI.EndGroup();}
    string status=level.BossUnlocked?"WARDEN-KAMMER":level.GuardsRemaining>0?"ZUGANG · "+level.GuardsRemaining+" WACHEN":"ZUGANG FREI";
    Label(new Rect(panel.x+12,panel.yMax-22,panel.width-78,14),status,9,level.GateOpen?cyan:pale);
    Label(new Rect(panel.xMax-60,panel.yMax-22,47,14),System.TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss"),9,pale,TextAnchor.MiddleRight);
   }finally{GUI.matrix=matrix;GUI.color=old;}
  }
  Vector2 Point(Vector3 p)=>new Vector2(map.width*.5f+(p.x-center.x)*pixelsPerMeter,map.height*.5f-(p.z-center.z)*pixelsPerMeter);
  Rect WorldRect(AegisMapRoom room){Vector2 topLeft=Point(new Vector3(room.x,0,room.z+room.depth));return new Rect(topLeft.x,topLeft.y,room.width*pixelsPerMeter,room.depth*pixelsPerMeter);}
  void Marker(Vector2 at,float size,Texture2D icon,Color color){Rect rect=new Rect(at.x-size*.5f,at.y-size*.5f,size,size);if(icon)Texture(rect,icon,color);else Fill(rect,color);}
  static void Outline(Rect r,Color c,float size){Fill(new Rect(r.x,r.y,r.width,size),c);Fill(new Rect(r.x,r.yMax-size,r.width,size),c);Fill(new Rect(r.x,r.y,size,r.height),c);Fill(new Rect(r.xMax-size,r.y,size,r.height),c);}
  void Label(Rect r,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){GUI.color=Color.white;text.fontSize=size;text.alignment=align;text.normal.textColor=color;GUI.Label(r,value,text);}
  static void Texture(Rect r,Texture2D texture,Color tint){GUI.color=tint;GUI.DrawTexture(r,texture,ScaleMode.StretchToFill,true);GUI.color=Color.white;}
  static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 }
}

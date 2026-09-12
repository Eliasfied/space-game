using UnityEngine;
namespace AsterionGame {
 // Code-drawn HUD artwork: shared textures are generated once, then clipped for live resource levels.
 public sealed class SciFiActionBar : System.IDisposable {
  Texture2D sphere,frame,glass;GUIStyle text,heavy,tooltip;
  readonly Color pale=new Color(.83f,.91f,.95f),muted=new Color(.37f,.49f,.57f),red=new Color(1,.13f,.22f),blue=new Color(.12f,.66f,1);
  void Init(){
   if(sphere)return;
   sphere=Texture("Resource plasma",0);frame=Texture("Machined resource bezel",1);glass=Texture("Resource lens",2);
   text=new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),alignment=TextAnchor.MiddleCenter};heavy=new GUIStyle(text){fontStyle=FontStyle.Bold};tooltip=new GUIStyle(text){wordWrap=true,alignment=TextAnchor.UpperLeft,fontSize=13,normal={textColor=pale}};
  }
  Texture2D Texture(string name,int kind){
   const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};var pixels=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float nx=(x+.5f-size*.5f)/(size*.5f),ny=(y+.5f-size*.5f)/(size*.5f),r=Mathf.Sqrt(nx*nx+ny*ny);Color c=Color.clear;
    if(kind==0 && r<1){
     float z=Mathf.Sqrt(Mathf.Max(0,1-r*r));float light=Mathf.Clamp01(-nx*.35f+ny*.4f+z*.7f);
     float flow=Mathf.Sin(nx*13+ny*9+Mathf.Sin(ny*8))*Mathf.Sin(ny*17-nx*5)*.055f;
     float v=Mathf.Clamp01(.12f+light*.65f+flow+Mathf.Pow(z,4)*.14f);c=new Color(v,v,v,Mathf.Clamp01((1-r)*128));
    }else if(kind==1 && r<1 && r>.81f){
     float light=Mathf.Clamp01((ny-nx)*.45f+.5f);float v=.1f+light*.23f;
     if(r>.963f || (r>.842f && r<.86f))v=.42f+light*.35f;
     if(r>.9f && r<.935f)v*=.45f;
     c=new Color(v*.79f,v*.9f,v,Mathf.Min(Mathf.Clamp01((1-r)*128),Mathf.Clamp01((r-.81f)*128)));
    }else if(kind==2 && r<1){
     float glint=Mathf.Exp(-((nx+.30f)*(nx+.30f)*33+(ny-.49f)*(ny-.49f)*60));
     float rim=Mathf.Pow(r,14)*.12f;float a=Mathf.Clamp01(glint*.65f+rim)*Mathf.Clamp01((1-r)*100);c=new Color(.75f,.91f,1,a);
    }
    pixels[y*size+x]=c;
   }
   tex.SetPixels(pixels);tex.Apply(false,true);return tex;
  }
  public void Draw(float availableWidth,Health health,Energy energy,AbilityCaster caster,PlayerClassDefinition definition,bool interactive){
   Init();Matrix4x4 old=GUI.matrix;float fit=Mathf.Min(1,(availableWidth-24)/1120f);
   GUI.matrix=old*Matrix4x4.TRS(new Vector3((availableWidth-1120*fit)*.5f,900-205*fit,0),Quaternion.identity,new Vector3(fit,fit,1));
   Color accent=definition?definition.accent:blue;
   // Recessed central chassis joins the two reactor housings.
   Fill(new Rect(178,44,764,122),new Color(.017f,.029f,.04f,.98f));
   Fill(new Rect(190,49,740,111),new Color(.043f,.063f,.078f,.98f));
   Fill(new Rect(193,51,734,2),new Color(.26f,.38f,.45f));
   Fill(new Rect(193,158,734,2),new Color(.12f,.2f,.25f));
   for(int i=0;i<4;i++){
    float x=182+i*249;Stroke(new Vector2(x,44),new Vector2(x+13,30),2,muted);Stroke(new Vector2(x,164),new Vector2(x+13,178),2,muted);
   }
   Label(new Rect(270,5,580,24),definition?definition.displayName.ToUpperInvariant():"VANGUARD",12,pale,true);
   Stroke(new Vector2(211,17),new Vector2(302,17),1,muted);Stroke(new Vector2(818,17),new Vector2(909,17),1,muted);
   float fraction=energy?Mathf.Clamp01(energy.Current/Mathf.Max(1,energy.maximum)):0;
   for(int i=0;i<40;i++)Fill(new Rect(223+i*17,36,13,4),fraction>i/40f?new Color(.1f,.48f,.7f):new Color(.065f,.10f,.13f));
   int hovered=-1;
   for(int i=0;i<8;i++){
    Rect slot=new Rect(222+i*85,65,76,76);var ability=caster.abilities!=null && i<caster.abilities.Length?caster.abilities[i]:null;
    bool occupied=ability;float cooldown=occupied?caster.Remaining(i):0;float cost=caster.EnergyCost(i);bool affordable=!energy||!occupied||energy.CanSpend(cost);
    Color tint=i==0?accent:i==1?blue:new Color(.3f,.95f,.72f);
    Fill(new Rect(slot.x-2,slot.y-2,80,80),new Color(.23f,.31f,.36f));Fill(slot,new Color(.015f,.027f,.039f));
    Fill(new Rect(slot.x+2,slot.y+2,72,72),occupied?new Color(tint.r*.13f,tint.g*.13f,tint.b*.13f):new Color(.034f,.047f,.055f));
    Fill(new Rect(slot.x+5,slot.y+5,66,1),occupied?tint*.48f:new Color(.08f,.12f,.15f));
    Fill(new Rect(slot.x+5,slot.y+70,66,1),new Color(.06f,.095f,.12f));
    if(occupied){
     Icon(i,new Vector2(slot.x+16,slot.y+13),tint,definition&&definition.jetpack);
     if(cooldown>0){
      float ratio=Mathf.Clamp01(cooldown/Mathf.Max(AbilityCaster.GlobalCooldown,ability.cooldown));Fill(new Rect(slot.x+2,slot.y+2,72,72*ratio),new Color(.008f,.015f,.025f,.82f));
      Label(new Rect(slot.x,slot.y+13,76,37),cooldown.ToString("0.0"),24,pale,true);
     }else if(!affordable){Fill(new Rect(slot.x+2,slot.y+2,72,72),new Color(.015f,.02f,.03f,.66f));Label(new Rect(slot.x,slot.y+23,76,25),"ENERGIE",10,blue,true);}
     Fill(new Rect(slot.x+7,slot.y+72,62,2),cooldown<=0&&affordable?tint:new Color(.08f,.13f,.17f));
     Label(new Rect(slot.x+2,slot.y+54,72,17),cost>0?cost.ToString("0")+" EN":ability.energyOnHit>0?"+"+ability.energyOnHit.ToString("0")+" EN":"FREI",9,muted,true);
    }else{
     Stroke(new Vector2(slot.x+29,slot.y+34),new Vector2(slot.x+47,slot.y+34),1,new Color(.16f,.23f,.28f));
     Stroke(new Vector2(slot.x+38,slot.y+25),new Vector2(slot.x+38,slot.y+43),1,new Color(.16f,.23f,.28f));
     Label(new Rect(slot.x,slot.y+53,76,17),"FREI",8,muted);
    }
    if(caster.ProcReady(i)){
     Color glow=new Color(1,.72f,.16f,.65f+.35f*Mathf.Sin(Time.unscaledTime*9));
     Fill(new Rect(slot.x-4,slot.y-4,84,4),glow);Fill(new Rect(slot.x-4,slot.y+76,84,4),glow);
     Fill(new Rect(slot.x-4,slot.y,4,76),glow);Fill(new Rect(slot.x+76,slot.y,4,76),glow);
     Label(new Rect(slot.x-4,slot.y-22,84,18),caster.IsVanguard?"PROC SOFORT":"PROC +50%",10,glow,true);
    }
    string key=PlayerInputReader.SlotKeys[i];
    Fill(new Rect(slot.x+12,149,52,20),new Color(.07f,.10f,.12f));Label(new Rect(slot.x+10,149,56,20),occupied?key:"—",11,occupied?pale:muted,true);
    if(interactive&&slot.Contains(Event.current.mousePosition))hovered=i;
   }
   Orb(new Vector2(100,104),health.Current,health.maximum,red,"LEBEN");
   Orb(new Vector2(1020,104),energy?energy.Current:0,energy?energy.maximum:100,blue,caster.IsVanguard?"GLEITENERGIE":"ENERGIE");
   Label(new Rect(252,178,616,20),definition&&definition.buildEnergyOnHits?"TAB  ZIEL WÄHLEN    ·    ENERGIE DURCH TREFFER    ·    "+PlayerInputReader.SlotKeys[caster.MovementSlot]+" BRICHT CAST AB":"WASD  BEWEGEN     ·     TAB  ZIEL WÄHLEN     ·     ESC  PAUSE     ·     R  KLASSEN",9,muted);
   if(health.DamageReduction>0)Label(new Rect(27,6,146,18),"SCHILD  −25% SCHADEN",9,blue,true);
   if(hovered>=0){
    var a=caster.abilities!=null&&hovered<caster.abilities.Length?caster.abilities[hovered]:null;
    float x=Mathf.Clamp(222+hovered*85-112,185,605);
    Fill(new Rect(x,-91,330,116),new Color(.022f,.039f,.055f,.98f));Fill(new Rect(x,-91,330,2),accent);
    Label(new Rect(x+14,-80,302,24),a?a.displayName.ToUpperInvariant():"FREIER FÄHIGKEITENSLOT",14,pale,true);
    if(a){GUI.Label(new Rect(x+14,-49,302,40),a.description,tooltip);Label(new Rect(x+14,-4,302,21),caster.EnergyCost(hovered).ToString("0")+" ENERGIE   /   "+a.cooldown.ToString("0.##")+" S COOLDOWN",11,blue);}
    else GUI.Label(new Rect(x+14,-44,302,50),"Platz für eine weitere Fähigkeit.\nHier ist noch keine Fähigkeit ausgerüstet.",tooltip);
   }
   GUI.matrix=old;
  }
  void Orb(Vector2 center,float value,float maximum,Color color,string title){
   float fill=Mathf.Clamp01(value/Mathf.Max(1,maximum));Rect lens=new Rect(center.x-62,center.y-62,124,124);
   GUI.color=new Color(.065f,.085f,.105f);GUI.DrawTexture(lens,sphere);GUI.color=Color.white;
   if(fill>0){GUI.color=color;GUI.DrawTextureWithTexCoords(new Rect(lens.x,lens.y+lens.height*(1-fill),lens.width,lens.height*fill),sphere,new Rect(0,0,1,fill));GUI.color=Color.white;}
   if(fill>0&&fill<1){float half=62*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(2*fill-1,2)));Fill(new Rect(center.x-half,center.y+62-124*fill,half*2,1.5f),color*.85f);}
   GUI.DrawTexture(lens,glass);GUI.DrawTexture(new Rect(center.x-77,center.y-77,154,154),frame);
   for(int i=0;i<36;i++){
    float angle=(i*9+108)*Mathf.Deg2Rad;Vector2 d=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    Stroke(center+d*79,center+d*(i%3==0?84:82),i%3==0?2:1,i/36f<fill?new Color(color.r*.65f,color.g*.65f,color.b*.65f,.9f):new Color(.12f,.18f,.22f));
   }
   Label(new Rect(center.x-56,center.y+2,112,35),Mathf.CeilToInt(value).ToString(),26,pale,true);
   Label(new Rect(center.x-56,center.y+33,112,18),"/ "+maximum.ToString("0"),10,new Color(.65f,.76f,.82f));
   Label(new Rect(center.x-65,185,130,20),title,10,color,true);
  }
  void Icon(int index,Vector2 p,Color color,bool hunter){
   if(index==0){
    for(int i=0;i<(hunter?2:1);i++){
     Vector2 o=p+new Vector2(i*15,0);Stroke(o+new Vector2(4,34),o+new Vector2(29,9),hunter?5:7,color);
     Stroke(o+new Vector2(3,39),o+new Vector2(12,30),3,color);Stroke(o+new Vector2(30,7),o+new Vector2(39,-2),2,color);
     if(!hunter)Stroke(o+new Vector2(17,25),o+new Vector2(27,27),4,color);
    }
   }else if(index==1){
    for(int i=0;i<3;i++){Vector2 o=p+new Vector2(i*12,0);Stroke(o+new Vector2(0,6),o+new Vector2(14,22),3,color);Stroke(o+new Vector2(14,22),o+new Vector2(0,38),3,color);}
   }else if(index==3){
    Stroke(p+new Vector2(4,6),p+new Vector2(18,17),7,color);Stroke(p+new Vector2(18,17),p+new Vector2(34,14),7,color);Stroke(p+new Vector2(34,14),p+new Vector2(40,23),5,color);
    Stroke(p+new Vector2(4,38),p+new Vector2(24,38),2,color);
   }else if(index==4){
    for(int i=0;i<3;i++){float y=7+i*12;Stroke(p+new Vector2(3,y),p+new Vector2(32,y),2,color);Stroke(p+new Vector2(25,y-5),p+new Vector2(33,y),2,color);Stroke(p+new Vector2(33,y),p+new Vector2(25,y+5),2,color);}
   }else if(index==5){
    Stroke(p+new Vector2(3,39),p+new Vector2(39,3),5,color);Stroke(p+new Vector2(5,22),p+new Vector2(5,6),2,color);Stroke(p+new Vector2(5,6),p+new Vector2(21,6),2,color);Stroke(p+new Vector2(23,39),p+new Vector2(39,39),2,color);Stroke(p+new Vector2(39,39),p+new Vector2(39,23),2,color);
   }else if(index==6){
    Vector2[] shield={new Vector2(22,0),new Vector2(39,9),new Vector2(36,28),new Vector2(22,43),new Vector2(8,28),new Vector2(5,9),new Vector2(22,0)};
    for(int i=0;i<shield.Length-1;i++)Stroke(p+shield[i],p+shield[i+1],3,color);Stroke(p+new Vector2(22,12),p+new Vector2(22,29),3,color);
   }else if(index==7){
    for(int i=0;i<3;i++)Stroke(p+new Vector2(8+i*12,40),p+new Vector2(8+i*12,3),3,color);
    Stroke(p+new Vector2(0,15),p+new Vector2(42,15),2,color);Stroke(p+new Vector2(0,25),p+new Vector2(42,25),2,color);
   }else{
    for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;Stroke(p+new Vector2(22+Mathf.Cos(a)*18,26+Mathf.Sin(a)*10),p+new Vector2(22+Mathf.Cos(b)*18,26+Mathf.Sin(b)*10),2,color);}
    Stroke(p+new Vector2(22,0),p+new Vector2(22,27),3,color);Stroke(p+new Vector2(13,18),p+new Vector2(22,28),2,color);Stroke(p+new Vector2(31,18),p+new Vector2(22,28),2,color);
   }
  }
  void Label(Rect rect,string value,int size,Color color,bool bold=false){var style=bold?heavy:text;style.fontSize=size;style.normal.textColor=color;GUI.Label(rect,value,style);}
  void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
  void Stroke(Vector2 a,Vector2 b,float thickness,Color color){
   // Compose in action-bar coordinates; RotateAroundPivot mixes screen-space
   // pivots with our translated/scaled HUD matrix and scatters the icon strokes.
   Matrix4x4 matrix=GUI.matrix;
   GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);
   Fill(new Rect(0,-thickness*.5f,(b-a).magnitude,thickness),color);GUI.matrix=matrix;
  }
  public void Dispose(){if(sphere)Object.Destroy(sphere);if(frame)Object.Destroy(frame);if(glass)Object.Destroy(glass);}
 }
}

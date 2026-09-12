using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AsterionGame {
 public sealed class ClassSelectionScreen : MonoBehaviour {
  const int PreviewLayer=30;
  GameSession session;PlayerClassDefinition[] choices=Array.Empty<PlayerClassDefinition>();int selected;
  GameObject studio,portrait;Transform turntable;Camera portraitCamera;RenderTexture texture;Material stageMaterial;
  GUIStyle label,button,body;float angle=18,lastDrag,reloadAt;bool dragging,launching;int savedMask;Camera gameCamera;
  readonly Color ink=new Color(.018f,.03f,.045f),muted=new Color(.48f,.58f,.65f),paper=new Color(.89f,.93f,.94f);
  PlayerClassDefinition Choice=>choices.Length>0?choices[selected]:null;
  public void Initialize(GameSession owner){session=owner;CreateStudio();Reload();}
  void Reload(){
   choices=Resources.LoadAll<PlayerClassDefinition>("Classes");Array.Sort(choices,(a,b)=>string.CompareOrdinal(a.name,b.name));selected=Mathf.Clamp(selected,0,Mathf.Max(0,choices.Length-1));ShowModel();
  }
  void CreateStudio(){
   studio=new GameObject("Class portrait studio");studio.transform.position=new Vector3(0,-300,0);
   turntable=new GameObject("Portrait turntable").transform;turntable.SetParent(studio.transform,false);
   var cameraObject=new GameObject("Portrait camera");cameraObject.transform.SetParent(studio.transform,false);cameraObject.transform.localPosition=new Vector3(0,1.63f,5.7f);cameraObject.transform.LookAt(studio.transform.position+Vector3.up*1.3f);
   portraitCamera=cameraObject.AddComponent<Camera>();portraitCamera.orthographic=true;portraitCamera.orthographicSize=1.65f;portraitCamera.nearClipPlane=.1f;portraitCamera.farClipPlane=15;portraitCamera.cullingMask=1<<PreviewLayer;portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=Color.clear;portraitCamera.allowHDR=true;
   gameCamera=Camera.main;
   var data=portraitCamera.GetUniversalAdditionalCameraData();
   // The portrait shares the arena's directional/ambient lighting and colour pipeline.
   // Sample volumes at the gameplay camera, not at the off-map portrait studio.
   if(gameCamera){
    var gameData=gameCamera.GetUniversalAdditionalCameraData();
    data.renderPostProcessing=gameData.renderPostProcessing;data.renderShadows=gameData.renderShadows;
    data.volumeLayerMask=gameData.volumeLayerMask;data.volumeTrigger=gameData.volumeTrigger?gameData.volumeTrigger:gameCamera.transform;
    data.antialiasing=gameData.antialiasing;data.antialiasingQuality=gameData.antialiasingQuality;
    portraitCamera.allowHDR=gameCamera.allowHDR;
   }else{data.renderPostProcessing=true;data.renderShadows=true;data.volumeLayerMask=1;}

   // Keep the backdrop transparent through post-processing and composite over the menu.
   texture=new RenderTexture(1000,1100,24,RenderTextureFormat.ARGBHalf){name="Class portrait",antiAliasing=1};texture.Create();portraitCamera.targetTexture=texture;
   var platform=GameObject.CreatePrimitive(PrimitiveType.Cylinder);platform.name="Display platform";platform.transform.SetParent(studio.transform,false);platform.transform.localPosition=new Vector3(0,-.09f,0);platform.transform.localScale=new Vector3(2.25f,.07f,2.25f);platform.layer=PreviewLayer;Destroy(platform.GetComponent<Collider>());
   stageMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));stageMaterial.SetColor("_BaseColor",new Color(.045f,.075f,.10f));stageMaterial.SetFloat("_Metallic",.5f);stageMaterial.SetFloat("_Smoothness",.4f);platform.GetComponent<Renderer>().sharedMaterial=stageMaterial;
   if(gameCamera){savedMask=gameCamera.cullingMask;gameCamera.cullingMask&=~(1<<PreviewLayer);}
  }
  void ShowModel(){
   if(portrait){portrait.SetActive(false);Destroy(portrait);}angle=18;
   if(!Choice || !Choice.model)return;
   portraitCamera.orthographicSize=Choice.classId=="vanguard"?1.95f:1.65f;
   portrait=Instantiate(Choice.model,turntable,false);portrait.name=Choice.displayName+" portrait";
   foreach(var t in portrait.GetComponentsInChildren<Transform>(true))t.gameObject.layer=PreviewLayer;
   foreach(var c in portrait.GetComponentsInChildren<Collider>())c.enabled=false;
   var driver=portrait.GetComponentInChildren<MeshyCharacterAnimator>();if(driver)driver.SetPreviewFloor(studio.transform.position.y-.02f);
  }
  void Update(){
   if(!Choice || !Choice.model){if(Time.unscaledTime>reloadAt){reloadAt=Time.unscaledTime+1;Reload();}}
   if(!dragging && Time.unscaledTime-lastDrag>3)angle+=Time.unscaledDeltaTime*7;
   if(turntable)turntable.localRotation=Quaternion.Euler(0,angle,0);
  }
  void Styles(){
   if(label!=null)return;
   label=new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),normal={textColor=paper},alignment=TextAnchor.MiddleLeft};
   button=new GUIStyle(label){fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
   body=new GUIStyle(label){fontSize=17,wordWrap=true,normal={textColor=muted}};
  }
  void OnGUI(){
   if(session==null)return;Styles();GUI.depth=-20;
   Fill(new Rect(0,0,Screen.width,Screen.height),ink);
   var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
   GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
   Color accent=Choice?Choice.accent:CombatFx.Cyan;
   Rect portraitRect=new Rect(645,30,900,790);
   if(texture)GUI.DrawTexture(portraitRect,texture,ScaleMode.ScaleToFit,true);
   Fill(new Rect(62,53,36,3),accent);Text(new Rect(111,37,600,35),"THE CRUCIBLE  /  REKRUTIERUNG",13,muted,true);
   Text(new Rect(62,101,560,35),"WÄHLE DEINE KLASSE",15,accent,true);
   Text(new Rect(60,143,590,65),Choice?Choice.displayName.ToUpperInvariant():"KLASSEN WERDEN GELADEN",Choice?43:24,paper,true);
   Text(new Rect(64,211,555,24),Choice?Choice.role:"Assets → Refresh in Unity ausführen",12,muted);
   for(int i=0;i<choices.Length;i++){
    Rect r=new Rect(62,267+i*90,490,76);bool active=i==selected,hover=r.Contains(Event.current.mousePosition);
    Fill(r,active?new Color(.075f,.12f,.16f):hover?new Color(.06f,.085f,.11f):new Color(.032f,.05f,.068f));
    Fill(new Rect(r.x,r.y,3,r.height),active?choices[i].accent:new Color(.11f,.16f,.2f));
    Text(new Rect(r.x+22,r.y+14,400,25),"0"+(i+1)+"    "+choices[i].displayName.ToUpperInvariant(),19,active?paper:muted,true);
    Text(new Rect(r.x+62,r.y+43,400,18),choices[i].role,10,muted);
    if(GUI.Button(r,GUIContent.none,GUIStyle.none)&&selected!=i){selected=i;ShowModel();lastDrag=Time.unscaledTime;}
   }
   if(Choice){
    GUI.Label(new Rect(64,469,480,96),Choice.description,body);
    Text(new Rect(64,588,205,24),Choice.maximumHealth.ToString("0")+"  /  LEBEN",14,paper,true);
    Text(new Rect(300,588,270,24),Choice.jetpack?"JETPACK / MOBIL":"GEWEHR / ROBUST",14,accent,true);
    string[] keys=PlayerInputReader.SlotKeys;
    for(int i=0;i<Mathf.Min(8,Choice.abilities==null?0:Choice.abilities.Length);i++){
     float y=632+(i%4)*30,x=64+(i/4)*250;Text(new Rect(x,y,48,25),keys[i],10,accent,true);
     Text(new Rect(x+50,y,190,25),Choice.abilities!=null&&i<Choice.abilities.Length&&Choice.abilities[i]?Choice.abilities[i].displayName.ToUpperInvariant():"Wird vorbereitet …",11,paper);
    }
   }
   Text(new Rect(889,738,470,26),"ZIEHEN ZUM DREHEN",11,muted,false,TextAnchor.MiddleCenter);
   if(!Choice || !Choice.model)Text(new Rect(730,350,720,90),"MODELL WIRD VORBEREITET\nBlender-Export ausführen, danach Assets → Refresh.",18,muted,false,TextAnchor.MiddleCenter);
   Rect play=new Rect(1080,801,455,57);bool ready=Choice&&Choice.Ready&&!launching;
   Fill(play,ready?accent:new Color(.13f,.17f,.20f));button.normal.textColor=ready?ink:muted;GUI.Label(play,launching?"EINSATZ WIRD GESTARTET …":"In den Kampf  →",button);
   if(ready && GUI.Button(play,GUIContent.none,GUIStyle.none)){launching=true;session.BeginMission(Choice);}
   Text(new Rect(64,775,900,68),"DIE GLADIATOREN-ARCHE\nEine verlassene Orbitalstation. Eine fehlerhafte Rekrutierungs-KI.\nBestehe als Söldner ihre simulierten Kammern und erreiche den Hauptkern.",13,muted);
   Event e=Event.current;
   if(e.type==EventType.MouseDown && e.button==0 && portraitRect.Contains(e.mousePosition)){dragging=true;e.Use();}
   if(e.type==EventType.MouseDrag && dragging){angle-=e.delta.x*.45f;lastDrag=Time.unscaledTime;e.Use();}
   if(e.type==EventType.MouseUp)dragging=false;
   GUI.matrix=old;
  }
  void Text(Rect r,string text,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft){label.fontSize=size;label.normal.textColor=color;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;label.alignment=alignment;GUI.Label(r,text,label);}
  void Fill(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
  void OnDestroy(){if(gameCamera)gameCamera.cullingMask=savedMask;if(studio)Destroy(studio);if(texture){texture.Release();Destroy(texture);}if(stageMaterial)Destroy(stageMaterial);}
 }
}

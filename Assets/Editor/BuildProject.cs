using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AsterionGame;
public static class BuildProject {
 static Dictionary<string,Material> materials=new Dictionary<string,Material>();static Transform arena;
 const string ScenePath="Assets/Scenes/Asterion.unity";
 [MenuItem("Asterion/01 Generate Arena")]
 public static void Generate(){
  AssetDatabase.Refresh();materials.Clear();Directory.CreateDirectory("Assets/Art/Generated");Directory.CreateDirectory("Assets/Resources");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  CreateMaterials();ConfigureRendering();arena=new GameObject("ARENA / SECTOR 07").transform;CreateArena();
  GameObject player=new GameObject("Vanguard / Player");player.layer=8;player.transform.position=new Vector3(0,.12f,-7);
  var ph=player.AddComponent<Health>();ph.team=Team.Player;ph.maximum=120;
  var cc=player.AddComponent<CharacterController>();cc.height=2;cc.radius=.36f;cc.center=Vector3.up;cc.stepOffset=.25f;cc.skinWidth=.035f;
  var input=player.AddComponent<PlayerInputReader>();var motor=player.AddComponent<PlayerMotor>();var caster=player.AddComponent<AbilityCaster>();
  GameObject pv=Model("Vanguard",player.transform,Vector3.zero);motor.visual=pv.transform;SetLayer(pv,8);
  var anim=player.AddComponent<MechAnimator>();anim.motor=motor;anim.body=pv.transform;anim.leftLeg=Child(pv,"LeftLeg");anim.rightLeg=Child(pv,"RightLeg");player.AddComponent<DamageFeedback>();
  caster.abilities=new AbilityDefinition[]{Ability<LaserAbility>("Pulse Laser",.22f,0,23,18),Ability<DashAbility>("Phase Shift",2.8f,0,4.8f,0),Ability<OrbitalAbility>("Orbital Strike",6,.38f,15,160)};
  GameObject boss=new GameObject("Asterion / Reactor Warden");boss.layer=9;boss.transform.position=new Vector3(0,0,3.5f);
  var bh=boss.AddComponent<Health>();bh.team=Team.Hostile;bh.maximum=1800;
  var bc=boss.AddComponent<CapsuleCollider>();bc.radius=1.28f;bc.height=2.3f;bc.center=Vector3.up*1.1f;
  GameObject bv=Model("Asterion",boss.transform,Vector3.zero);SetLayer(bv,9);
  var bt=boss.AddComponent<EnemyHealthBar>();bt.displayName="Asterion";
  var brain=boss.AddComponent<BossBrain>();brain.player=player.transform;brain.visual=bv.transform;boss.AddComponent<DamageFeedback>();var ba=boss.AddComponent<MechAnimator>();ba.halo=Child(bv,"Halo");
  CreateDummy(new Vector3(-8,0,-4));
  GameObject cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";var cam=cameraGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=7.4f;cam.nearClipPlane=.1f;cam.farClipPlane=150;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.018f,.034f,.05f);cam.allowHDR=true;
  cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.FastApproximateAntialiasing;
  cameraGo.AddComponent<AudioListener>();var follow=cameraGo.AddComponent<FollowCamera>();follow.target=player.transform;cameraGo.transform.rotation=Quaternion.Euler(follow.pitch,0,0);cameraGo.transform.position=new Vector3(player.transform.position.x,.65f,player.transform.position.z+follow.forwardFraming)-cameraGo.transform.forward*follow.distance;
  GameObject systems=new GameObject("MISSION CONTROL");var session=systems.AddComponent<GameSession>();session.input=input;session.player=ph;session.boss=bh;systems.AddComponent<SynthAudio>();
  var hud=systems.AddComponent<Hud>();hud.player=ph;hud.boss=bh;hud.brain=brain;hud.caster=caster;hud.input=input;hud.motor=motor;hud.session=session;
  PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/Vanguard.prefab");PrefabUtility.SaveAsPrefabAsset(boss,"Assets/Prefabs/Asterion.prefab");
  PlayerSettings.companyName="Asterion Studio";PlayerSettings.productName="Asterion";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);settings.FindProperty("activeInputHandler").intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();
  var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Player";tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue="Hostile";tags.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("ASTERION_SCENE_READY");
 }
 static void CreateMaterials(){
  Mat("Obsidian",new Color(.045f,.075f,.095f),.65f,.38f);Mat("Titanium",new Color(.38f,.49f,.53f),.68f,.4f);Mat("Ivory",new Color(.8f,.84f,.79f),.36f,.38f);Mat("Rubber",new Color(.025f,.032f,.043f),.1f,.2f);Mat("Cyan",new Color(.03f,.72f,.84f),.45f,.6f,3);Mat("Amber",new Color(1,.22f,.035f),.45f,.6f,3);Mat("Gold",new Color(.51f,.29f,.105f),.7f,.42f);Mat("Floor",new Color(.72f,.78f,.81f),.12f,.3f);Mat("FloorAlternate",new Color(.62f,.69f,.73f),.15f,.28f);Mat("Edge",new Color(.045f,.084f,.11f),.55f,.38f);Mat("Marking",new Color(.39f,.54f,.54f),.4f,.3f);
  foreach(string name in new[]{"SuitBlue","ArmorBlue","NavyFabric","StitchBlue","Skin","Hair","VisorGlass","RifleGraphite","IceWhite","MarineCyan"}){
   var authored=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/"+name+".mat");if(authored)materials[name]=authored;
  }
  var line=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));line.SetColor("_BaseColor",Color.white);SaveAsset(line,"Assets/Resources/FxLine.mat");SaveAsset(CombatFx.Unlit(new Color(.1f,.8f,1,.1f),true),"Assets/Resources/FxTransparent.mat");
 }
 static void Mat(string name,Color color,float metal,float smooth,float emission=0){var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=name;mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}string path="Assets/Art/Materials/"+name+".mat";SaveAsset(mat,path);materials[name]=AssetDatabase.LoadAssetAtPath<Material>(path);}
 static void SaveAsset(UnityEngine.Object obj,string path){var existing=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(existing){EditorUtility.CopySerialized(obj,existing);UnityEngine.Object.DestroyImmediate(obj);}else AssetDatabase.CreateAsset(obj,path);}
 static T Ability<T>(string name,float cooldown,float cast,float range,float damage) where T:AbilityDefinition {var a=ScriptableObject.CreateInstance<T>();a.displayName=name;a.cooldown=cooldown;a.castTime=cast;a.range=range;a.damage=damage;a.energyCost=typeof(T)==typeof(DashAbility)?20:typeof(T)==typeof(OrbitalAbility)?35:0;a.description=name=="Pulse Laser"?"Free-aim hitscan laser; cover blocks the beam.":name=="Phase Shift"?"Collision-aware movement dash with temporary invulnerability.":"Delayed circular strike at the cursor, with a visible warning.";string path="Assets/Abilities/"+name.Replace(" ","")+".asset";SaveAsset(a,path);return AssetDatabase.LoadAssetAtPath<T>(path);}
 static void ConfigureRendering(){
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");if(!pipeline)throw new Exception("URP asset missing");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;pipeline.renderScale=1;pipeline.msaaSampleCount=1;pipeline.shadowDistance=45;
  var global=new GameObject("Atmosphere / Bloom and ACES");var volume=global.AddComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.85f);bloom.threshold.Override(1.05f);bloom.scatter.Override(.6f);var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.2f);color.contrast.Override(12);color.saturation.Override(-3);color.colorFilter.Override(new Color(.92f,.97f,1));var vig=profile.Add<Vignette>(true);vig.intensity.Override(.2f);vig.smoothness.Override(.55f);SaveAsset(profile,"Assets/Settings/AsterionVolume.asset");volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/AsterionVolume.asset");foreach(var component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.27f,.4f,.47f);RenderSettings.ambientEquatorColor=new Color(.14f,.22f,.27f);RenderSettings.ambientGroundColor=new Color(.065f,.1f,.14f);RenderSettings.fog=false;
  var sun=new GameObject("Key / cold daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.78f,.9f,1);sun.intensity=2.1f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-32,0);
  var rim=new GameObject("Rim / warm bounce").AddComponent<Light>();rim.type=LightType.Directional;rim.color=new Color(1,.49f,.25f);rim.intensity=.75f;rim.transform.rotation=Quaternion.Euler(25,140,0);
  // Retain the dynamically instantiated VFX shader and its transparent variant.
  var gs=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=gs.FindProperty("m_AlwaysIncludedShaders");var shader=Shader.Find("Universal Render Pipeline/Unlit");bool exists=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;if(!exists){int index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=shader;}gs.ApplyModifiedPropertiesWithoutUndo();
 }
 static GameObject Model(string name,Transform parent,Vector3 at){
  string path="Assets/Art/Models/"+name+".fbx";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("Model missing: "+path+". Run Prepare.command first.");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name+" / Blender model";go.transform.SetParent(parent,false);go.transform.localPosition=at;
  foreach(var r in go.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){string n=mats[i]?mats[i].name.Split('.')[0]:"Obsidian";mats[i]=materials.ContainsKey(n)?materials[n]:materials["Obsidian"];}r.sharedMaterials=mats;}
  return go;
 }
 static Transform Child(GameObject go,string name)=>go.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==name);
 static void SetLayer(GameObject go,int layer){foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=layer;}
 static GameObject Primitive(string name,PrimitiveType type,Vector3 pos,Vector3 scale,string mat,bool collider=false){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(arena);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=materials[mat];if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
  else if(type==PrimitiveType.Cylinder){
   // The default capsule expands with the platform radius. Use the flat mesh instead.
   UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
   var meshCollider=go.AddComponent<MeshCollider>();
   meshCollider.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
   meshCollider.convex=false;
  }
  return go;}
 static void Ring(string name,float r,float width,string material,float y,int segments=128){var go=new GameObject(name);go.transform.parent=arena;var filter=go.AddComponent<MeshFilter>();var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[material];Vector3[] v=new Vector3[segments*2];int[] t=new int[segments*6];for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));v[i*2]=d*(r-width/2)+Vector3.up*y;v[i*2+1]=d*(r+width/2)+Vector3.up*y;int next=(i+1)%segments;t[i*6]=i*2;t[i*6+1]=next*2;t[i*6+2]=i*2+1;t[i*6+3]=i*2+1;t[i*6+4]=next*2;t[i*6+5]=next*2+1;}var mesh=new Mesh{name=name};mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();SaveAsset(mesh,"Assets/Art/Generated/"+name+".asset");filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Generated/"+name+".asset");}
 static void CreateArena(){
  Primitive("Foundation",PrimitiveType.Cylinder,new Vector3(0,-.77f,0),new Vector3(29, .65f,29),"Obsidian",true);
  // Unity cylinder height is 2: top of deck is exactly y=0.
  Primitive("Deck",PrimitiveType.Cylinder,new Vector3(0,-.12f,0),new Vector3(27.8f,.12f,27.8f),"Floor",true);
  Ring("Outer armor",13.5f,.62f,"Titanium",.015f);Ring("Perimeter light",13.12f,.055f,"Cyan",.035f);Ring("Inner circuit",7.6f,.035f,"Marking",.025f);Ring("Core socket",3.3f,.12f,"Gold",.03f);Ring("Lower reactor glow",14.4f,.08f,"Cyan",-.48f);
  // Deck seams, service panels and recessed radial lighting.
  for(int x=-12;x<=12;x+=2){float len=2*Mathf.Sqrt(12.9f*12.9f-x*x);Primitive("Deck seam",PrimitiveType.Cube,new Vector3(x,.013f,0),new Vector3(.022f,.012f,len),"Edge");Primitive("Deck seam",PrimitiveType.Cube,new Vector3(0,.013f,x),new Vector3(len,.012f,.022f),"Edge");}
  for(int i=0;i<24;i++){
   float a=i*15;Vector3 d=Quaternion.Euler(0,a,0)*Vector3.forward;
   var plate=Primitive("Rim armor panel",PrimitiveType.Cube,d*13.85f+Vector3.down*.08f,new Vector3(1.25f,.22f,.85f),i%3==0?"Ivory":"Titanium");plate.transform.rotation=Quaternion.Euler(0,a,0);
   var block=Primitive("Safety railing",PrimitiveType.Cube,d*14.15f+Vector3.up*.42f,new Vector3(3.72f,.85f,.35f),"Obsidian",true);block.transform.rotation=Quaternion.Euler(0,a,0);
   if(i%3!=0){var light=Primitive("Rim inlay",PrimitiveType.Cube,d*13.78f+Vector3.up*.065f,new Vector3(.64f,.025f,.08f),"Cyan");light.transform.rotation=Quaternion.Euler(0,a,0);}
   if(i%3==0){GameObject p=Model("Pylon",arena,d*13.8f);p.transform.rotation=Quaternion.Euler(0,a,0);var col=p.AddComponent<BoxCollider>();col.center=Vector3.up*1.6f;col.size=new Vector3(1,3.2f,1);}
  }
  foreach(var pos in new[]{new Vector3(-5,0,.5f),new Vector3(5.7f,0,-2),new Vector3(5,0,7.6f)}){GameObject cover=Model("Barricade",arena,pos);cover.transform.rotation=Quaternion.Euler(0,pos.z>5?45:-15,0);var c=cover.AddComponent<BoxCollider>();c.size=new Vector3(2.8f,1.2f,1);c.center=Vector3.up*.6f;}
  for(int i=0;i<4;i++){
   float x=(i<2?-1:1)*9,z=i%2==0?-7:7;Primitive("Service hatch",PrimitiveType.Cube,new Vector3(x,.025f,z),new Vector3(1.55f,.045f,2.3f),"Edge");
   for(int j=0;j<7;j++)Primitive("Cooling fin",PrimitiveType.Cube,new Vector3(x,.06f,z-.8f+j*.27f),new Vector3(1.3f,.04f,.055f),"Titanium");
  }
  for(int i=0;i<7;i++){Primitive("Entry runway",PrimitiveType.Cube,new Vector3(-1.3f,.032f,-6-i*.85f),new Vector3(.07f,.025f,.4f),"Cyan");Primitive("Entry runway",PrimitiveType.Cube,new Vector3(1.3f,.032f,-6-i*.85f),new Vector3(.07f,.025f,.4f),"Cyan");}
  FloorText("07",new Vector3(-9,.047f,4),3.5f,new Color(.26f,.38f,.4f));FloorText("ASTERION",new Vector3(2,.047f,-10.1f),.7f,new Color(.45f,.59f,.59f));
  var lightPoint=new GameObject("Reactor glow").AddComponent<Light>();lightPoint.type=LightType.Point;lightPoint.transform.position=new Vector3(0,2.8f,3.5f);lightPoint.color=new Color(1,.27f,.05f);lightPoint.intensity=5;lightPoint.range=7;lightPoint.shadows=LightShadows.None;
 }
 static void FloorText(string s,Vector3 at,float scale,Color color){var go=new GameObject("Deck stencil / "+s);go.transform.parent=arena;go.transform.position=at;go.transform.rotation=Quaternion.Euler(90,0,0);var text=go.AddComponent<TextMesh>();text.text=s;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=100;text.characterSize=scale*.1f;text.anchor=TextAnchor.MiddleCenter;text.color=color;go.GetComponent<Renderer>().sharedMaterial=text.font.material;}
 static void CreateDummy(Vector3 pos){var go=new GameObject("Training unit");go.layer=9;go.transform.position=pos;var h=go.AddComponent<Health>();h.team=Team.Hostile;h.maximum=600;var c=go.AddComponent<CapsuleCollider>();c.height=1.8f;c.radius=.48f;c.center=Vector3.up*.9f;var t=go.AddComponent<EnemyHealthBar>();t.displayName="Training Unit";
  var baseObject=Primitive("Dummy pedestal",PrimitiveType.Cylinder,pos+Vector3.up*.12f,new Vector3(1.35f,.12f,1.35f),"Obsidian");baseObject.transform.SetParent(go.transform,true);
  var core=Primitive("Dummy armor",PrimitiveType.Capsule,pos+Vector3.up*1.15f,new Vector3(.72f,.55f,.65f),"Ivory");core.transform.SetParent(go.transform,true);
  var eye=Primitive("Dummy reactor",PrimitiveType.Sphere,pos+new Vector3(0,1.35f,-.34f),Vector3.one*.25f,"Cyan");eye.transform.SetParent(go.transform,true);SetLayer(go,9);go.AddComponent<DamageFeedback>();}
 [MenuItem("Asterion/Build macOS (optional)")]
 public static void Build(){Generate();string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Build/Asterion.app"));Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);Debug.Log("ASTERION_BUILD_SUCCEEDED "+output);}
}

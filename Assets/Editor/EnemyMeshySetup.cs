using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AsterionGame;

[InitializeOnLoad]
public static class EnemyMeshySetup {
 const string Root=EnemyMeshyImportSettings.Root;
 const string Destination="Assets/Resources/Enemies";
 static bool queued,building;
 static EnemyMeshySetup(){Queue();EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)Queue();if(s==PlayModeStateChange.ExitingEditMode)Build(true);};}
 public static void Queue(){if(queued||building)return;queued=true;EditorApplication.delayCall+=()=>Build(false);}
 [MenuItem("Asterion/Meshy/Rebuild Enemy Integration")]
 public static void Rebuild(){SessionState.EraseString("Asterion.EnemySignature");Queue();}
 static void Build(bool enteringPlay){
  queued=false;if(building||EditorApplication.isPlaying||(!enteringPlay&&EditorApplication.isPlayingOrWillChangePlaymode))return;
  if(EditorApplication.isUpdating||EditorApplication.isCompiling){Queue();return;}
  if(!Directory.Exists(Root+"/Warden/Models")||!Directory.Exists(Root+"/Repair/Models"))return;
  building=true;
  try{
   foreach(string path in Directory.GetFiles(Root,"*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".fbx")||p.EndsWith(".png"))){
    var importer=AssetImporter.GetAtPath(path);if(importer&&importer.userData!=EnemyMeshyImportSettings.Revision)importer.SaveAndReimport();
   }
   string signature=string.Join(";",Directory.GetFiles(Root,"*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".fbx")||p.EndsWith(".png")).OrderBy(p=>p).Select(p=>AssetDatabase.GetAssetDependencyHash(p).ToString()))+"enemy-prefabs-1";
   if(SessionState.GetString("Asterion.EnemySignature","")==signature&&Ready())return;
   Directory.CreateDirectory(Destination);AssetDatabase.Refresh();
   BuildCharacter("Warden","Axe_Stance",4.2f,"AegisWarden",true);
   BuildCharacter("Crimson","Walking",2.05f,"CrimsonSentinel",false);
   BuildRepair();AssetDatabase.SaveAssets();SessionState.SetString("Asterion.EnemySignature",signature);
   Debug.Log("CRUCIBLE_ENEMIES_READY: Aegis Warden + Hammer, Crimson Sentinel, Repair Drone.");
  }catch(Exception e){Debug.LogError("Gegner-Integration: "+e.Message+"\n"+e.StackTrace);}
  finally{building=false;}
 }
 static bool Ready()=>new[]{"AegisWarden","CrimsonSentinel","RepairDrone"}.All(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Destination+"/"+n+".prefab"));
 static AnimationClip Clip(string folder,string name){
  var clip=AssetDatabase.LoadAllAssetsAtPath(Root+"/"+folder+"/Models/"+name+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).FirstOrDefault();
  if(!clip)throw new InvalidOperationException(folder+" / Animation fehlt: "+name);return clip;
 }
 static Material Surface(string folder){
  string path=Root+"/"+folder+"/Surface.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Universal Render Pipeline/Lit");
  if(!shader)throw new InvalidOperationException("URP Lit fehlt");if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;
  Texture2D Texture(string name){var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+folder+"/Textures/"+name+".png");if(!t)throw new InvalidOperationException(folder+" / 2D-Textur fehlt: "+name);return t;}
  mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",Texture("BaseColor"));mat.SetTexture("_BumpMap",Texture("Normal"));mat.SetFloat("_BumpScale",.8f);mat.EnableKeyword("_NORMALMAP");
  mat.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness"));mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",.5f);mat.EnableKeyword("_METALLICSPECGLOSSMAP");
  mat.SetTexture("_EmissionMap",Texture("Emission"));mat.SetColor("_EmissionColor",Color.white*.65f);mat.EnableKeyword("_EMISSION");EditorUtility.SetDirty(mat);return mat;
 }
 static GameObject Model(string folder,string name,Transform parent){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+folder+"/Models/"+name+".fbx");if(!source)throw new InvalidOperationException("Modell fehlt: "+folder+"/"+name);
  var model=UnityEngine.Object.Instantiate(source,parent,false);model.name="Character";
  foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.name=="Icosphere")UnityEngine.Object.DestroyImmediate(r.gameObject);
  var material=Surface(folder);foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
   renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
   if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
  }return model;
 }
 static void GroundAndSize(GameObject model,float height){
  var b=MeshyHunterSetup.GeometryBounds(model);if(b.size.y<.00001f)throw new InvalidOperationException("Modellhöhe ist ungültig: "+model.name);
  model.transform.localScale*=height/b.size.y;b=MeshyHunterSetup.GeometryBounds(model);model.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
 }
 static void BuildCharacter(string folder,string idle,float height,string name,bool boss){
  var wrapper=new GameObject(name);
  try{
   var model=Model(folder,idle,wrapper.transform);GroundAndSize(model,height);
   var animator=model.GetComponent<Animator>();if(!animator||!animator.avatar||!animator.avatar.isValid||!animator.avatar.isHuman)throw new InvalidOperationException(folder+": Humanoid-Avatar ungültig");
   animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   string path=Root+"/"+folder+"/Enemy.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
   foreach(var layer in controller.layers)UnityEngine.Object.DestroyImmediate(layer.stateMachine,true);
   controller.layers=Array.Empty<AnimatorControllerLayer>();controller.parameters=Array.Empty<AnimatorControllerParameter>();controller.AddLayer("Base");controller.AddParameter("ActionSpeed",AnimatorControllerParameterType.Float);
   var sm=controller.layers[0].stateMachine;
   AnimatorState State(string state,string clip,bool action=false){var s=sm.AddState(state);s.motion=Clip(folder,clip);if(action){s.speedParameter="ActionSpeed";s.speedParameterActive=true;}return s;}
   var rest=State("Idle",idle);if(!boss)rest.speed=0;sm.defaultState=rest;
   State("Walk","Walking");State("Run","Running");
   var driver=model.AddComponent<EnemyVisualAnimator>();driver.animator=animator;
   if(boss){
    State("Slam","Charged_Ground_Slam",true);State("Strike","Sword_Judgment",true);State("Channel","Bubble_Dance",true);State("Death","Dead");
    driver.slamLength=Clip(folder,"Charged_Ground_Slam").length;driver.strikeLength=Clip(folder,"Sword_Judgment").length;driver.channelLength=Clip(folder,"Bubble_Dance").length;
    AttachHammer(animator,wrapper.transform);
   }else{State("Strike","Shield_Push_Left",true);driver.strikeLength=Clip(folder,"Shield_Push_Left").length;}
   animator.runtimeAnimatorController=controller;EditorUtility.SetDirty(controller);SetLayer(wrapper);PrefabUtility.SaveAsPrefabAsset(wrapper,Destination+"/"+name+".prefab");
  }finally{UnityEngine.Object.DestroyImmediate(wrapper);}
 }
 static void AttachHammer(Animator animator,Transform wrapper){
  var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);if(!hand)throw new InvalidOperationException("Warden: rechte Hand fehlt");
  var socket=new GameObject("Hammer socket").transform;socket.SetParent(hand,false);socket.position=hand.position;
  // The handle crosses the palm, perpendicular to the fingers.
  var end=hand.Cast<Transform>().FirstOrDefault();Vector3 fingers=end?(end.position-hand.position).normalized:hand.up;
  Vector3 handle=Vector3.Cross(fingers,wrapper.forward).normalized;if(handle.sqrMagnitude<.1f)handle=wrapper.up;if(Vector3.Dot(handle,wrapper.up)<0)handle=-handle;
  socket.rotation=Quaternion.FromToRotation(Vector3.right,handle);var scale=hand.lossyScale;socket.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
  var model=Model("Hammer","Model",socket);model.name="Double hammer";
  var points=model.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(v=>socket.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
  if(points.Length==0)throw new InvalidOperationException("Hammer-Geometrie fehlt");var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
  Vector3 axis=bounds.size.x>=bounds.size.y&&bounds.size.x>=bounds.size.z?Vector3.right:bounds.size.y>=bounds.size.z?Vector3.up:Vector3.forward;
  var align=new GameObject("Handle alignment").transform;align.SetParent(socket,false);model.transform.SetParent(align,false);
  float length=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));align.localRotation=Quaternion.FromToRotation(axis,Vector3.right);align.localScale=Vector3.one*(3.7f/length);align.localPosition=-(align.localRotation*(bounds.center*align.localScale.x));
 }
 static void BuildRepair(){
  var wrapper=new GameObject("RepairDrone");try{
   var model=Model("Repair","Model",wrapper.transform);var b=MeshyHunterSetup.GeometryBounds(model);model.transform.localScale*=1.65f/Mathf.Max(b.size.x,b.size.z);
   b=MeshyHunterSetup.GeometryBounds(model);model.transform.localPosition-=new Vector3(b.center.x,b.min.y-.85f,b.center.z);SetLayer(wrapper);PrefabUtility.SaveAsPrefabAsset(wrapper,Destination+"/RepairDrone.prefab");
  }finally{UnityEngine.Object.DestroyImmediate(wrapper);}
 }
 static void SetLayer(GameObject root){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;}
}

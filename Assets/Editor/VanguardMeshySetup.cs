using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AsterionGame;

[InitializeOnLoad]
public static class VanguardMeshySetup {
 const string Root=MeshyImportSettings.VanguardRoot;
 public const string PrefabPath=Root+"/NeonVanguard.prefab";
 static bool queued,building;
 static VanguardMeshySetup(){Queue();EditorApplication.playModeStateChanged+=s=>{
  if(s==PlayModeStateChange.EnteredEditMode)Queue();
  if(s==PlayModeStateChange.ExitingEditMode)Build(true);
 };}
 public static void Queue(){if(queued||building)return;queued=true;EditorApplication.delayCall+=Build;}
 [MenuItem("Asterion/Meshy/Rebuild Vanguard Integration")]
 public static void Rebuild(){SessionState.EraseString("Asterion.VanguardSignature");Queue();}
 static void Build(){Build(false);}
 static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/"+name+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).FirstOrDefault();
 static bool Complete(GameObject prefab)=>prefab&&prefab.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Rifle"&&t.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh))&&prefab.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Muzzle");
 static void Build(bool enteringPlay){
  queued=false;if(building||EditorApplication.isPlaying||(!enteringPlay&&EditorApplication.isPlayingOrWillChangePlaymode))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
  if(!Directory.Exists(Root+"/Models"))return;
  building=true;GameObject wrapper=null;
  try{
   foreach(string path in Directory.GetFiles(Root,"*.fbx",SearchOption.AllDirectories)){
    var importer=AssetImporter.GetAtPath(path) as ModelImporter;
    string revision=path.Contains("/Weapons/")?MeshyImportSettings.WeaponRevision:MeshyImportSettings.VanguardRevision;
    if(importer&&importer.userData!=revision)importer.SaveAndReimport();
   }
   foreach(string path in Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories)){
    var importer=AssetImporter.GetAtPath(path) as TextureImporter;
    if(importer&&(importer.userData!=MeshyImportSettings.VanguardTextureRevision||importer.textureShape!=TextureImporterShape.Texture2D))importer.SaveAndReimport();
   }
   string signature=string.Join(";",Directory.GetFiles(Root,"*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".fbx")||p.EndsWith(".png")).OrderBy(p=>p).Select(p=>AssetDatabase.GetAssetDependencyHash(p).ToString()))+"sentinel-materials-5";
   if(SessionState.GetString("Asterion.VanguardSignature","")==signature&&Complete(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))){BindClass();return;}
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/Idle.fbx");
   var idle=Clip("Idle");var walk=Clip("Walking");var run=Clip("Running");var shoot=Clip("Shooting");var ready=Clip("ReadyPose");
   if(!source||!idle||!walk||!run||!shoot||!ready)throw new InvalidOperationException("Vanguard-FBX-Import noch nicht vollständig.");
   wrapper=new GameObject("Neon Vanguard");var model=UnityEngine.Object.Instantiate(source,wrapper.transform,false);model.name="Character";
   var animator=model.GetComponent<Animator>();
   if(!animator||!animator.avatar||!animator.avatar.isValid||!animator.avatar.isHuman)throw new InvalidOperationException("Vanguard: Humanoid-Avatar ungültig.");
   foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.name=="Icosphere")UnityEngine.Object.DestroyImmediate(r.gameObject);
   var bounds=MeshyHunterSetup.GeometryBounds(model);float sourceHeight=bounds.size.y;
   if(sourceHeight<.1f)throw new InvalidOperationException("Vanguard-Modell hat keine gültige Größe.");
   const float height=2.513f;model.transform.localScale*=height/sourceHeight;
   bounds=MeshyHunterSetup.GeometryBounds(model);model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   var material=Material(Root,"Sentinel",1.2f);
   foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
    renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
    if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
   }
   // Reuse the existing death/kick clips only as fallback states; all locomotion/fire
   // and the ready pose come from the supplied Sentinel export.
   var death=AssetDatabase.LoadAllAssetsAtPath(MeshyImportSettings.Root+"/Models/Death.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).FirstOrDefault();
   var kick=AssetDatabase.LoadAllAssetsAtPath(MeshyImportSettings.Root+"/Models/Kick.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).FirstOrDefault();
   if(!death||!kick)throw new InvalidOperationException("Gemeinsame Fallback-Animationen fehlen.");
   var controller=MeshyHunterSetup.CreateController(idle,walk,run,kick,death,ready,shoot,Root+"/Sentinel.controller",Root+"/RifleUpperBody.mask");
   var layers=controller.layers;for(int i=0;i<layers.Length;i++)layers[i].iKPass=layers[i].name=="Weapon fire";controller.layers=layers;EditorUtility.SetDirty(controller);
   animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var driver=model.AddComponent<MeshyCharacterAnimator>();driver.visualRoot=wrapper.transform;driver.preserveModelTransform=true;
   float sole=float.PositiveInfinity;
   foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes}){var foot=animator.GetBoneTransform(bone);if(foot)sole=Mathf.Min(sole,foot.position.y);}
   driver.soleOffset=Mathf.Max(0,sole-wrapper.transform.position.y);
   var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
   var end=hand.Cast<Transform>().FirstOrDefault(t=>t.name.ToLowerInvariant().Contains("end"));
   Vector3 forward=end?(end.position-hand.position).normalized:hand.up;
   Vector3 up=wrapper.transform.forward;if(Vector3.Cross(forward,up).sqrMagnitude<.01f)up=wrapper.transform.up;
   var mount=new GameObject("RifleSocket").transform;mount.SetParent(hand,false);mount.position=hand.position+forward*.04f;mount.rotation=Quaternion.LookRotation(forward,up);
   var scale=hand.lossyScale;mount.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
   var rifle=BuildRifle(mount);driver.supportGrip=rifle.transform.Find("Support grip");
   if(!Complete(wrapper))throw new InvalidOperationException("Vanguard-Gewehr oder Mündung fehlt.");
   PrefabUtility.SaveAsPrefabAsset(wrapper,PrefabPath);AssetDatabase.SaveAssets();BindClass();SessionState.SetString("Asterion.VanguardSignature",signature);
   Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Vanguard-Import.txt","Neon Vanguard ready\nHeight: 2.513 m\nIdle: "+idle.length+" s\nWalking: "+walk.length+" s\nRunning: "+run.length+" s\nShooting (01a09187): "+shoot.length+" s\nReady pose (01a0915a): "+ready.length+" s\nRifle: 1.45 m, right-hand socket, left support during fire, muzzle at barrel.\n");
   Debug.Log("NEON_VANGUARD_READY");
  }catch(Exception e){Debug.LogError("Vanguard-Integration: "+e.Message);}
  finally{if(wrapper)UnityEngine.Object.DestroyImmediate(wrapper);building=false;}
 }
 static void BindClass(){
  var definition=AssetDatabase.LoadAssetAtPath<PlayerClassDefinition>("Assets/Resources/Classes/01_Vanguard.asset");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
  if(definition&&Complete(prefab)){VanguardLoadoutSetup.Configure(definition);if(definition.model!=prefab){definition.model=prefab;EditorUtility.SetDirty(definition);}AssetDatabase.SaveAssets();}
 }
 static Material Material(string folder,string name,float emission){
  Texture2D Texture(string map)=>AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+map+".png");
  var shader=Shader.Find("Universal Render Pipeline/Lit");
  if(!shader)throw new InvalidOperationException("URP-Lit-Shader fehlt: "+name);
  foreach(string map in new[]{"BaseColor","Normal","MetallicSmoothness","Emission"})
   if(!Texture(map))throw new InvalidOperationException("Vanguard: 2D-Textur nicht importiert: "+folder+"/Textures/"+map+".png");
  string path=folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
  mat.shader=shader;mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",Texture("BaseColor"));mat.SetTexture("_BumpMap",Texture("Normal"));mat.SetFloat("_BumpScale",.8f);mat.EnableKeyword("_NORMALMAP");
  mat.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness"));mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",.5f);mat.SetFloat("_SmoothnessTextureChannel",0);mat.EnableKeyword("_METALLICSPECGLOSSMAP");
  mat.SetTexture("_EmissionMap",Texture("Emission"));mat.SetColor("_EmissionColor",Color.white*(emission*.35f));mat.EnableKeyword("_EMISSION");EditorUtility.SetDirty(mat);return mat;
 }
 static GameObject BuildRifle(Transform parent){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Weapons/Models/Rifle.fbx");if(!source)throw new InvalidOperationException("Vanguard-Gewehr nicht importiert.");
  var rifle=new GameObject("Rifle");
  try{
  var alignment=new GameObject("Grip alignment").transform;alignment.SetParent(rifle.transform,false);
  var model=UnityEngine.Object.Instantiate(source,alignment,false);model.name="Sentinel rifle";
  var points=model.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
  if(points.Length==0)throw new InvalidOperationException("Gewehr ohne Geometrie.");
  Bounds bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
  Vector3 axis=bounds.size.x>bounds.size.z?Vector3.right:Vector3.forward;
  float rear=points.Where(v=>v.y<bounds.min.y+bounds.size.y*.23f).Average(v=>Vector3.Dot(v-bounds.center,axis));
  if(Mathf.Abs(rear)<.001f)throw new InvalidOperationException("Gewehr-Griffrichtung unklar.");
  Vector3 forward=axis*(rear>0?-1:1);float length=Mathf.Max(bounds.size.x,bounds.size.z);
  Vector3 grip=bounds.center-forward*length*.22f-Vector3.up*bounds.size.y*.20f;
  Vector3 muzzle=bounds.center+forward*length*.505f+Vector3.up*bounds.size.y*.19f;
  Vector3 support=bounds.center+forward*length*.20f-Vector3.up*bounds.size.y*.22f;
  float scale=1.45f/length;Quaternion rotation=Quaternion.Inverse(Quaternion.LookRotation(forward,Vector3.up));
  alignment.localRotation=rotation;alignment.localScale=Vector3.one*scale;alignment.localPosition=-(rotation*grip)*scale;
  foreach(var pair in new[]{("Muzzle",muzzle),("Support grip",support)}){var t=new GameObject(pair.Item1).transform;t.SetParent(rifle.transform,false);t.localPosition=rotation*(pair.Item2-grip)*scale;}
  var material=Material(Root+"/Weapons","Rifle",1.5f);foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
  PrefabUtility.SaveAsPrefabAsset(rifle,Root+"/Weapons/Rifle.prefab");rifle.transform.SetParent(parent,false);return rifle;
  }catch{UnityEngine.Object.DestroyImmediate(rifle);throw;}
 }
}

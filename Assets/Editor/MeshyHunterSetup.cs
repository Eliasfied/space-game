using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AsterionGame;
[InitializeOnLoad]
public static class MeshyHunterSetup {
 const string Root=MeshyImportSettings.Root;
 public const string PrefabPath=Root+"/MeshyBountyHunter.prefab";
 const string ControllerPath=Root+"/MeshyHunter.controller";
 static bool queued,building;
 static MeshyHunterSetup(){Queue();EditorApplication.playModeStateChanged+=OnPlayModeChanged;}
 static void OnPlayModeChanged(PlayModeStateChange state){
  if(state==PlayModeStateChange.EnteredEditMode)Queue();
  // delayCall can arrive after Unity has already started entering Play mode.
  // Finish the shared character prefab synchronously before either view creates it.
  if(state==PlayModeStateChange.ExitingEditMode){
   Build(true);
   if(!HasWeapons(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))){
    EditorApplication.isPlaying=false;
    Debug.LogError("Bounty Hunter: Revolver-Prefab noch nicht bereit. Import abwarten und Asterion > Meshy > Rebuild Character Integration ausführen.");
   }
  }
 }
 static bool HasWeapons(GameObject prefab){
  if(!prefab)return false;
  foreach(string side in new[]{"Left","Right"}){
   var socket=prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="PistolSocket"+side);
   var weapon=socket?socket.Find("Revolver"+side):null;
   if(!weapon||!weapon.Find("Muzzle"+side)||!weapon.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh))return false;
  }
  return true;
 }
 public static void Queue(){if(queued||building)return;queued=true;EditorApplication.delayCall+=Build;}
 [MenuItem("Asterion/Meshy/Rebuild Character Integration")]
 public static void Rebuild(){SessionState.EraseString("Asterion.MeshySignature");Queue();}
 static AnimationClip Clip(string file){return AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/"+file+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).FirstOrDefault();}
 static void Build(){Build(false);}
 static void Build(bool enteringPlay){
  queued=false;if(building||EditorApplication.isPlaying||(!enteringPlay&&EditorApplication.isPlayingOrWillChangePlaymode))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
  building=true;GameObject wrapper=null;
  try{
  // Complete all prerequisite imports in this pass, including on the Play transition.
  foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx")){
   var importer=AssetImporter.GetAtPath(path) as ModelImporter;
   if(importer&&importer.userData!=MeshyImportSettings.Revision){importer.SaveAndReimport();}
  }
  var weaponImporter=AssetImporter.GetAtPath(NeonRevolverSetup.ModelPath) as ModelImporter;
  if(weaponImporter&&weaponImporter.userData!=MeshyImportSettings.WeaponRevision){weaponImporter.SaveAndReimport();}
  string weaponTextures=MeshyImportSettings.WeaponsRoot+"/Textures";
  if(!Directory.Exists(weaponTextures))return;
  foreach(var path in Directory.GetFiles(weaponTextures,"*.png")){
   var textureImporter=AssetImporter.GetAtPath(path) as TextureImporter;
   if(textureImporter&&textureImporter.userData!=MeshyImportSettings.WeaponRevision){textureImporter.SaveAndReimport();}
  }
  if(!NeonRevolverSetup.Ready)return;
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/Character.fbx");
  var walk=Clip("Walking");var run=Clip("Running");var kick=Clip("Kick");var death=Clip("Death");var skill=Clip("Skill");var shooting=Clip("Shooting");var idle=Clip("Idle");
  if(!source||!walk||!run||!kick||!death||!skill||!shooting||!idle)return;
  var shader=Shader.Find("Universal Render Pipeline/Lit");var color=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/BaseColor.png");var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Normal.png");var packed=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/MetallicSmoothness.png");
  if(!shader||!color||!normal||!packed)return;
  string signature=string.Join(";",Directory.GetFiles(Root+"/Models","*.fbx").Select(p=>AssetDatabase.GetAssetDependencyHash(p).ToString()))+AssetDatabase.GetAssetDependencyHash(Root+"/Textures/BaseColor.png").ToString()+NeonRevolverSetup.DependencyHash+"neon-hand-weapons-14";
  if(SessionState.GetString("Asterion.MeshySignature","")==signature&&HasWeapons(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))){BindClass();return;}
   wrapper=new GameObject("Neon Bounty Hunter");var model=UnityEngine.Object.Instantiate(source,wrapper.transform,false);model.name="Character";
   var animator=model.GetComponent<Animator>();
   if(!animator||!animator.avatar||!animator.avatar.isValid||!animator.avatar.isHuman)throw new InvalidOperationException("Meshy Character: Humanoid-Avatar ist noch nicht gültig. Rig-Zuordnung prüfen.");
   foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.name=="Icosphere")UnityEngine.Object.DestroyImmediate(r.gameObject);
   var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new InvalidOperationException("Meshy: kein sichtbares Mesh gefunden.");
   float height=VanguardHeight();
   Bounds bounds=GeometryBounds(model);
   float measuredHeight=bounds.size.y;
   var head=animator.GetBoneTransform(HumanBodyBones.Head);
   var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
   float skeletonHeight=head.position.y-Mathf.Min(leftFoot.position.y,rightFoot.position.y);
   if(measuredHeight<skeletonHeight*.8f||measuredHeight>skeletonHeight*2f)throw new InvalidOperationException("Mesh-/Skelettmaß widersprüchlich; Prefab wird nicht überschrieben.");
   model.transform.localScale*=height/measuredHeight;
   bounds=GeometryBounds(model);model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   string matPath=Root+"/MeshyHunter.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
   mat.shader=shader;mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",color);mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",.8f);mat.EnableKeyword("_NORMALMAP");mat.SetTexture("_MetallicGlossMap",packed);mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);mat.SetFloat("_SmoothnessTextureChannel",0);
   var emission=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Emission.png");if(emission){mat.SetTexture("_EmissionMap",emission);mat.SetColor("_EmissionColor",Color.white*1.5f);mat.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(mat);
   foreach(var r in renderers){r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();if(r is SkinnedMeshRenderer skinned)skinned.updateWhenOffscreen=true;}
   var controller=CreateController(idle,walk,run,kick,death,skill,shooting);
   animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var driver=model.AddComponent<MeshyCharacterAnimator>();driver.visualRoot=wrapper.transform;
   float sole=float.PositiveInfinity;
   foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes}){
    var foot=animator.GetBoneTransform(bone);if(foot)sole=Mathf.Min(sole,foot.position.y);
   }
   driver.soleOffset=Mathf.Max(0,sole-wrapper.transform.position.y);
   var revolver=NeonRevolverSetup.Build();
   CreateWeaponMount(animator,HumanBodyBones.LeftHand,"Left",wrapper.transform,revolver);
   CreateWeaponMount(animator,HumanBodyBones.RightHand,"Right",wrapper.transform,revolver);
   var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
   Socket("JetLeft",chest,wrapper.transform.TransformPoint(new Vector3(-.49f,1.94f,-.3f)));
   Socket("JetRight",chest,wrapper.transform.TransformPoint(new Vector3(.49f,1.94f,-.3f)));
   if(!HasWeapons(wrapper))throw new InvalidOperationException("Beide Revolver samt Mündungen müssen am Charakter vorhanden sein.");
   PrefabUtility.SaveAsPrefabAsset(wrapper,PrefabPath);AssetDatabase.SaveAssets();BindClass();SessionState.SetString("Asterion.MeshySignature",signature);
   Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Meshy-Import.txt","Neon Bounty Hunter integration ready\nAvatar: valid humanoid\nHeight (matched to Vanguard): "+height.ToString("F3")+" m\nSource geometry height: "+measuredHeight.ToString("F3")+" m\nModel scale: "+model.transform.localScale.x.ToString("F4")+"\nWalking: "+walk.length+" s\nRunning: "+run.length+" s\nKick: "+kick.length+" s\nDeath: "+death.length+" s\nSkill: "+skill.length+" s\nShooting: "+shooting.length+" s\nPortrait: supplied Idle loop at normal speed.\nIdle: revised Neon idle (01a09073), "+idle.length+" s; procedural arm IK disabled.\nBody: new Neon model. Two separate Neon revolvers attached to left/right hands; barrel exit sockets on each weapon. Kick/Skill/Death retargeted from previous clips.\n");
   Debug.Log("MESHY_BOUNTY_HUNTER_READY");
  }catch(Exception e){Debug.LogError("Meshy-Integration: "+e.Message);}
  finally{if(wrapper)UnityEngine.Object.DestroyImmediate(wrapper);building=false;}
 }
 static void BindClass(){var cls=AssetDatabase.LoadAssetAtPath<PlayerClassDefinition>("Assets/Resources/Classes/02_BountyHunter.asset");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);if(cls&&prefab&&cls.model!=prefab){cls.model=prefab;EditorUtility.SetDirty(cls);AssetDatabase.SaveAssets();}}
 static float VanguardHeight(){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Vanguard.fbx");
  if(!source)throw new InvalidOperationException("Vanguard-Modell fehlt für den Größenvergleich.");
  var instance=UnityEngine.Object.Instantiate(source);
  try{return GeometryBounds(instance).size.y;}finally{UnityEngine.Object.DestroyImmediate(instance);}
 }
 // Evaluate the same skinning matrices as the renderer. BakeMesh on this FBX's
 // 100x mesh transform gave already-scaled vertices, which TransformPoint scaled again.
 static Vector3[] WorldVertices(SkinnedMeshRenderer renderer){
  var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bindposes=mesh.bindposes;var bones=renderer.bones;
  var matrices=new Matrix4x4[bindposes.Length];
  for(int i=0;i<matrices.Length;i++)matrices[i]=i<bones.Length&&bones[i]?bones[i].localToWorldMatrix*bindposes[i]:renderer.transform.localToWorldMatrix;
  for(int i=0;i<vertices.Length;i++){
   Vector3 vertex=vertices[i];
   if(i>=weights.Length){vertices[i]=renderer.transform.TransformPoint(vertex);continue;}
   var w=weights[i];float sum=w.weight0+w.weight1+w.weight2+w.weight3;
   if(sum<=.0001f){vertices[i]=renderer.transform.TransformPoint(vertex);continue;}
   Vector3 point=Vector3.zero;
   if(w.weight0>0)point+=matrices[w.boneIndex0].MultiplyPoint3x4(vertex)*w.weight0;
   if(w.weight1>0)point+=matrices[w.boneIndex1].MultiplyPoint3x4(vertex)*w.weight1;
   if(w.weight2>0)point+=matrices[w.boneIndex2].MultiplyPoint3x4(vertex)*w.weight2;
   if(w.weight3>0)point+=matrices[w.boneIndex3].MultiplyPoint3x4(vertex)*w.weight3;
   vertices[i]=point/sum;
  }
  return vertices;
 }
 public static Bounds GeometryBounds(GameObject model){
  Bounds bounds=default;bool found=false;
  foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
   if(renderer is SkinnedMeshRenderer skin){
    foreach(var point in WorldVertices(skin)){if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);}
   }else{
    var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
    Bounds local=filter.sharedMesh.bounds;
    for(int i=0;i<8;i++){var point=renderer.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);}
   }
  }
  if(!found)throw new InvalidOperationException("Keine Geometrie für die Größenmessung gefunden.");return bounds;
 }
 public static AnimatorController CreateController(AnimationClip idle,AnimationClip walk,AnimationClip run,AnimationClip kick,AnimationClip death,AnimationClip skill,AnimationClip shooting,string controllerPath=ControllerPath,string maskPath=Root+"/WeaponUpperBody.mask"){
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
  if(!controller){
   controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Move",AnimatorControllerParameterType.Float);
   var locomotion=controller.CreateBlendTreeInController("Locomotion",out BlendTree tree);tree.blendParameter="Move";tree.useAutomaticThresholds=false;tree.AddChild(idle,0);tree.AddChild(walk,.4f);tree.AddChild(run,1);
   var sm=controller.layers[0].stateMachine;sm.defaultState=locomotion;
   foreach(var pair in new[]{("Kick",kick,.8f),("Skill",skill,.65f),("Death",death,death.length)}){
    var state=sm.AddState(pair.Item1);state.motion=pair.Item2;state.speed=pair.Item2.length/Mathf.Max(.1f,pair.Item3);
    if(pair.Item1!="Death"){var transition=state.AddTransition(locomotion);transition.hasExitTime=true;transition.exitTime=1;transition.duration=.10f;}
   }
  }
  var baseMachine=controller.layers[0].stateMachine;
  // Update the existing controller too: it still references the old frozen Idle.anim.
  var movement=baseMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Locomotion");
  if(movement&&movement.motion is BlendTree locomotionTree){
   var motions=locomotionTree.children;
   for(int i=0;i<motions.Length;i++)motions[i].motion=motions[i].threshold<.001f?idle:motions[i].threshold<.8f?walk:run;
   locomotionTree.children=motions;EditorUtility.SetDirty(locomotionTree);
  }
  var preview=baseMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="PreviewIdle"||s.name=="PreviewWalk");
  if(!preview)preview=baseMachine.AddState("PreviewIdle");preview.name="PreviewIdle";preview.motion=idle;preview.speed=1f;preview.iKOnFeet=false;EditorUtility.SetDirty(preview);
  var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
  if(!mask){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,maskPath);}
  for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
  foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
  EditorUtility.SetDirty(mask);
  if(!controller.layers.Any(l=>l.name=="Weapon fire"))controller.AddLayer("Weapon fire");
  var layers=controller.layers;
  for(int i=0;i<layers.Length;i++){
   layers[i].iKPass=false;
   if(layers[i].name!="Weapon fire")continue;
   layers[i].avatarMask=mask;layers[i].defaultWeight=0;layers[i].blendingMode=AnimatorLayerBlendingMode.Override;
   var sm=layers[i].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Shooting");
   if(!state)state=sm.AddState("Shooting");state.motion=shooting;state.speed=1;state.iKOnFeet=false;sm.defaultState=state;EditorUtility.SetDirty(state);
  }
  controller.layers=layers;EditorUtility.SetDirty(controller);return controller;
 }
 static void CreateWeaponMount(Animator animator,HumanBodyBones bone,string side,Transform root,GameObject revolver){
  var hand=animator.GetBoneTransform(bone);
  if(!hand)throw new InvalidOperationException("Neon: Handknochen fehlt: "+side);
  var end=hand.Cast<Transform>().FirstOrDefault(t=>t.name.ToLowerInvariant().Contains("end"));
  Vector3 forward=end?(end.position-hand.position).normalized:hand.up;
  Vector3 up=root.forward;if(Vector3.Cross(forward,up).sqrMagnitude<.01f)up=root.up;
  var mount=new GameObject("PistolSocket"+side).transform;mount.SetParent(hand,false);
  mount.position=hand.position+forward*.07f;mount.rotation=Quaternion.LookRotation(forward,up);
  // Socket units are Unity metres; cancel FBX/bone import scale once.
  Vector3 scale=hand.lossyScale;mount.localScale=new Vector3(1/Mathf.Max(.0001f,Mathf.Abs(scale.x)),1/Mathf.Max(.0001f,Mathf.Abs(scale.y)),1/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
  var weapon=UnityEngine.Object.Instantiate(revolver,mount,false);weapon.name="Revolver"+side;
  var muzzle=weapon.transform.Find("BarrelExit");if(!muzzle)throw new InvalidOperationException("Revolver-Mündung fehlt.");muzzle.name="Muzzle"+side;
 }
 static void Socket(string name,Transform parent,Vector3 world){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=world;}
}

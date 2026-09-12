using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
public sealed class MeshyImportSettings:AssetPostprocessor {
 public const string Revision="mesh-rig-4";
 public const string Root="Assets/Art/MeshyHunter";
 public const string WeaponsRoot=Root+"/Weapons";
 public const string VanguardTextureRevision="sentinel-textures-3";
 public const string VanguardRevision="sentinel-rig-2";
 public const string VanguardRoot="Assets/Art/MeshyVanguard";
 public const string WeaponRevision="revolver-1";
 void OnPreprocessTexture(){
  if(!assetPath.StartsWith(Root+"/Textures/")&&!assetPath.StartsWith(WeaponsRoot+"/Textures/")&&!assetPath.StartsWith(VanguardRoot+"/Textures/")&&!assetPath.StartsWith(VanguardRoot+"/Weapons/Textures/"))return;
  var t=(TextureImporter)assetImporter;if(assetPath.StartsWith(VanguardRoot+"/"))t.userData=VanguardTextureRevision;else if(assetPath.StartsWith(WeaponsRoot+"/Textures/"))t.userData=WeaponRevision;
  t.textureShape=TextureImporterShape.Texture2D;
  if(assetPath.StartsWith(VanguardRoot+"/")){t.textureCompression=TextureImporterCompression.Compressed;t.alphaIsTransparency=false;}
  t.maxTextureSize=2048;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;
  t.sRGBTexture=assetPath.EndsWith("BaseColor.png");t.alphaSource=TextureImporterAlphaSource.FromInput;
  if(assetPath.EndsWith("Normal.png")){t.textureType=TextureImporterType.NormalMap;t.convertToNormalmap=false;}
  else t.textureType=TextureImporterType.Default;
 }
 void OnPreprocessModel(){
  if(assetPath.StartsWith(WeaponsRoot+"/Models/")||assetPath.StartsWith(VanguardRoot+"/Weapons/Models/")){
   var weapon=(ModelImporter)assetImporter;weapon.importCameras=false;weapon.importLights=false;weapon.addCollider=false;weapon.importAnimation=false;weapon.animationType=ModelImporterAnimationType.None;weapon.materialImportMode=ModelImporterMaterialImportMode.None;weapon.isReadable=true;weapon.globalScale=1;weapon.useFileScale=true;weapon.userData=WeaponRevision;return;
  }
  if(!assetPath.StartsWith(Root+"/Models/")&&!assetPath.StartsWith(VanguardRoot+"/Models/"))return;
  var m=(ModelImporter)assetImporter;m.importCameras=false;m.importLights=false;m.addCollider=false;m.importAnimation=true;m.optimizeGameObjects=false;m.isReadable=true;
  m.animationType=ModelImporterAnimationType.Human;m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  m.materialImportMode=ModelImporterMaterialImportMode.None;m.userData=assetPath.StartsWith(VanguardRoot+"/")?VanguardRevision:Revision;
  if(assetPath.StartsWith(VanguardRoot+"/")){m.globalScale=1;m.useFileScale=true;}
  bool original=assetPath.EndsWith("Character.fbx")||assetPath.EndsWith("ExtraAction.fbx")||assetPath.EndsWith("ReadyPose.fbx")||assetPath.EndsWith("Shooting.fbx")||assetPath.EndsWith("Idle.fbx");
  var map=new Dictionary<string,string>{{"Hips","Hips"},{"Head","Head"},{"Neck",original?"neck":"Neck"},{"Spine",original?"Spine02":"Chest"},{"Chest",original?"Spine01":"UpperChest"}};
  if(original)map["UpperChest"]="Spine";
  foreach(string side in new[]{"Left","Right"}){
   map[side+"Shoulder"]=side+"Shoulder";map[side+"UpperArm"]=side+(original?"Arm":"UpperArm");map[side+"LowerArm"]=side+(original?"ForeArm":"LowerArm");map[side+"Hand"]=side+"Hand";
   map[side+"UpperLeg"]=side+(original?"UpLeg":"UpperLeg");map[side+"LowerLeg"]=side+(original?"Leg":"LowerLeg");map[side+"Foot"]=side+"Foot";map[side+"Toes"]=side+(original?"ToeBase":"Toes");
  }
  var bones=new List<HumanBone>();foreach(var pair in map)bones.Add(new HumanBone{humanName=pair.Key,boneName=pair.Value,limit=new HumanLimit{useDefaultValues=true}});
  var desc=m.humanDescription;
  // Regenerate rest transforms for the replacement mesh; do not retain the old body proportions.
  if(assetPath.StartsWith(VanguardRoot+"/Models/")||assetPath.EndsWith("Character.fbx")||assetPath.EndsWith("Running.fbx")||assetPath.EndsWith("Shooting.fbx")||assetPath.EndsWith("Idle.fbx"))desc.skeleton=System.Array.Empty<SkeletonBone>();
  desc.human=bones.ToArray();desc.upperArmTwist=.5f;desc.lowerArmTwist=.5f;desc.upperLegTwist=.5f;desc.lowerLegTwist=.5f;desc.armStretch=0;desc.legStretch=0;desc.feetSpacing=0;m.humanDescription=desc;
 }
 void OnPreprocessAnimation(){
  if(!assetPath.StartsWith(Root+"/Models/")&&!assetPath.StartsWith(VanguardRoot+"/Models/"))return;
  var m=(ModelImporter)assetImporter;
  var clips=m.defaultClipAnimations;
  foreach(var clip in clips){clip.loopTime=assetPath.EndsWith("Walking.fbx")||assetPath.EndsWith("Running.fbx")||assetPath.EndsWith("Shooting.fbx")||assetPath.EndsWith("PortraitWalk.fbx")||assetPath.EndsWith("Idle.fbx")||assetPath.EndsWith("ReadyPose.fbx");clip.loopPose=clip.loopTime;clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;}
  m.clipAnimations=clips;
 }
 void OnPostprocessAnimation(GameObject root,AnimationClip clip){
  if(!assetPath.StartsWith(Root+"/Models/")&&!assetPath.StartsWith(VanguardRoot+"/Models/"))return;
  // The FBXs use different skeleton/unit conventions. Humanoid retargeting owns
  // the bone lengths; imported transform scale tracks must not resize them.
  foreach(var binding in AnimationUtility.GetCurveBindings(clip))
   if(binding.type==typeof(Transform)&&binding.propertyName.StartsWith("m_LocalScale"))AnimationUtility.SetEditorCurve(clip,binding,null);
 }
 static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] oldPaths){
  foreach(string path in imported)if(path.StartsWith(VanguardRoot+"/")){VanguardMeshySetup.Queue();break;}
  foreach(string path in imported)if(path.StartsWith(Root+"/Models/")||path.StartsWith(Root+"/Textures/")||path.StartsWith(WeaponsRoot+"/")){MeshyHunterSetup.Queue();break;}
 }
}

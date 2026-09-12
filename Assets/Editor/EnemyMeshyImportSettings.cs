using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class EnemyMeshyImportSettings : AssetPostprocessor {
 public const string Root="Assets/Art/MeshyEnemies";
 public const string Revision="crucible-enemies-1";
 bool Ours=>assetPath.StartsWith(Root+"/");
 bool Rig=>assetPath.StartsWith(Root+"/Warden/")||assetPath.StartsWith(Root+"/Crimson/");
 void OnPreprocessTexture(){
  if(!Ours)return;
  var t=(TextureImporter)assetImporter;t.userData=Revision;t.textureShape=TextureImporterShape.Texture2D;
  t.textureType=assetPath.EndsWith("Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
  t.convertToNormalmap=false;t.sRGBTexture=assetPath.EndsWith("BaseColor.png");t.maxTextureSize=2048;
  t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;t.textureCompression=TextureImporterCompression.Compressed;
  t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=false;
 }
 void OnPreprocessModel(){
  if(!Ours)return;
  var m=(ModelImporter)assetImporter;m.userData=Revision;m.globalScale=1;m.useFileScale=true;
  m.importCameras=false;m.importLights=false;m.addCollider=false;m.isReadable=true;m.optimizeGameObjects=false;
  m.materialImportMode=ModelImporterMaterialImportMode.None;m.importAnimation=Rig;
  m.animationType=Rig?ModelImporterAnimationType.Human:ModelImporterAnimationType.None;
  if(!Rig)return;
  m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  var map=new Dictionary<string,string>{{"Hips","Hips"},{"Spine","Chest"},{"Chest","UpperChest"},{"Neck","Neck"},{"Head","Head"}};
  foreach(string side in new[]{"Left","Right"})foreach(string bone in new[]{"Shoulder","UpperArm","LowerArm","Hand","UpperLeg","LowerLeg","Foot","Toes"})map[side+bone]=side+bone;
  var bones=new List<HumanBone>();foreach(var pair in map)bones.Add(new HumanBone{humanName=pair.Key,boneName=pair.Value,limit=new HumanLimit{useDefaultValues=true}});
  var desc=m.humanDescription;desc.skeleton=System.Array.Empty<SkeletonBone>();desc.human=bones.ToArray();
  desc.upperArmTwist=.5f;desc.lowerArmTwist=.5f;desc.upperLegTwist=.5f;desc.lowerLegTwist=.5f;desc.armStretch=0;desc.legStretch=0;desc.feetSpacing=0;m.humanDescription=desc;
 }
 void OnPreprocessAnimation(){
  if(!Ours||!Rig)return;
  var m=(ModelImporter)assetImporter;var clips=m.defaultClipAnimations;
  string name=Path.GetFileNameWithoutExtension(assetPath);
  foreach(var clip in clips){clip.loopTime=name=="Walking"||name=="Running"||name=="Axe_Stance";clip.loopPose=clip.loopTime;
   clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
   clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;
  }m.clipAnimations=clips;
 }
 void OnPostprocessAnimation(GameObject root,AnimationClip clip){
  if(!Ours||!Rig)return;
  foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.type==typeof(Transform)&&binding.propertyName.StartsWith("m_LocalScale"))AnimationUtility.SetEditorCurve(clip,binding,null);
 }
 static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] oldPaths){
  foreach(string path in imported)if(path.StartsWith(Root+"/")&&(path.EndsWith(".fbx")||path.EndsWith(".png"))){EnemyMeshySetup.Queue();break;}
 }
}

using UnityEditor;
using UnityEngine;

public sealed class AegisPropImportSettings : AssetPostprocessor {
 public const string Root="Assets/Art/MeshyEnvironment/Props";
 public const string Revision="aegis-props-1";
 bool Ours=>assetPath.StartsWith(Root+"/");

 void OnPreprocessModel(){
  if(!Ours)return;
  var model=(ModelImporter)assetImporter;
  model.userData=Revision;
  model.globalScale=1;model.useFileScale=true;
  model.importCameras=false;model.importLights=false;model.addCollider=false;
  model.importAnimation=false;model.animationType=ModelImporterAnimationType.None;
  model.materialImportMode=ModelImporterMaterialImportMode.None;
  model.importNormals=ModelImporterNormals.Import;
  model.importTangents=ModelImporterTangents.CalculateMikk;
  // Required when the level combines its static architecture at startup.
  model.isReadable=true;
 }

 void OnPreprocessTexture(){
  if(!Ours)return;
  var texture=(TextureImporter)assetImporter;
  texture.userData=Revision;
  texture.textureType=assetPath.EndsWith("/Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
  texture.convertToNormalmap=false;
  texture.sRGBTexture=assetPath.EndsWith("/BaseColor.png")||assetPath.EndsWith("/Emission.png");
  texture.maxTextureSize=2048;texture.mipmapEnabled=true;texture.anisoLevel=4;
  texture.wrapMode=TextureWrapMode.Repeat;
  texture.textureCompression=TextureImporterCompression.Compressed;
  texture.alphaSource=TextureImporterAlphaSource.FromInput;
  texture.alphaIsTransparency=false;
 }

 static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] previous){
  foreach(string path in imported)
   if(path.StartsWith(Root+"/")&&(path.EndsWith(".fbx")||path.EndsWith(".png"))){AegisPropSetup.Queue();break;}
 }
}


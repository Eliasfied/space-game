using UnityEditor;
using UnityEngine;
// Reuse the authored URP materials when the Blender FBX is refreshed.
public sealed class VanguardMaterialImporter : AssetPostprocessor {
 Material OnAssignMaterialModel(Material imported,Renderer renderer){
  if(assetPath!="Assets/Art/Models/Vanguard.fbx" && assetPath!="Assets/Art/Models/BountyHunter.fbx")return null;
  string name=imported.name.Replace(" (Instance)","");
  return AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/"+name+".mat");
 }
}

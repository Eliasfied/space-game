using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
// Builds one reusable revolver with its origin at the rear grip and +Z along the barrel.
public static class NeonRevolverSetup {
 public const string Root=MeshyImportSettings.WeaponsRoot;
 public const string ModelPath=Root+"/Models/Revolver.fbx";
 public const string PrefabPath=Root+"/Revolver.prefab";
 public static bool Ready=>AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)&&Texture("BaseColor")&&Texture("Normal")&&Texture("MetallicSmoothness");
 public static string DependencyHash=>AssetDatabase.GetAssetDependencyHash(ModelPath).ToString()+string.Join(";",new[]{"BaseColor","Normal","MetallicSmoothness","Emission"}.Select(n=>AssetDatabase.GetAssetDependencyHash(Root+"/Textures/"+n+".png").ToString()));
 static Texture2D Texture(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+".png");
 public static GameObject Build(){
  GameObject root=new GameObject("Neon Revolver");
  try{
   var alignment=new GameObject("Grip alignment").transform;alignment.SetParent(root.transform,false);
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),alignment,false);model.name="Revolver model";
   var points=model.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
   if(points.Length==0)throw new InvalidOperationException("Revolver enthält kein sichtbares Mesh.");
   Bounds bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
   // Unity's FBX conversion may reverse the horizontal source axis. Locate the rear grip
   // from the lower geometry rather than assuming that source -X stays Unity -X.
   Vector3 axis=bounds.size.x>bounds.size.z?Vector3.right:Vector3.forward;
   var gripPoints=points.Where(v=>v.y<bounds.min.y+bounds.size.y*.23f).ToArray();
   float rear=gripPoints.Average(v=>Vector3.Dot(v-bounds.center,axis));
   if(Mathf.Abs(rear)<.001f)throw new InvalidOperationException("Revolver-Griffrichtung ist nicht eindeutig.");
   Vector3 forward=axis*(rear>0?-1:1);float length=Mathf.Max(bounds.size.x,bounds.size.z);
   Vector3 grip=bounds.center-forward*(length*.36f)-Vector3.up*(bounds.size.y*.18f);
   Vector3 muzzle=bounds.center+forward*(length*.505f)+Vector3.up*(bounds.size.y*.11f);
   const float weaponLength=.82f;float scale=weaponLength/length;
   Quaternion rotation=Quaternion.Inverse(Quaternion.LookRotation(forward,Vector3.up));
   alignment.localRotation=rotation;alignment.localScale=Vector3.one*scale;alignment.localPosition=-(rotation*grip)*scale;
   var exit=new GameObject("BarrelExit").transform;exit.SetParent(root.transform,false);exit.localPosition=rotation*(muzzle-grip)*scale;
   var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new InvalidOperationException("URP Lit fehlt für den Revolver.");
   string matPath=Root+"/Revolver.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,matPath);}
   material.shader=shader;material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",Texture("BaseColor"));
   material.SetTexture("_BumpMap",Texture("Normal"));material.SetFloat("_BumpScale",.8f);material.EnableKeyword("_NORMALMAP");
   material.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness"));material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_SmoothnessTextureChannel",0);material.EnableKeyword("_METALLICSPECGLOSSMAP");
   material.SetTexture("_EmissionMap",Texture("Emission"));material.SetColor("_EmissionColor",Color.white*1.8f);material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
   foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
   PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
   return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}

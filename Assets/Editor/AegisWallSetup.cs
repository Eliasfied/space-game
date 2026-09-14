using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Replace visuals inside the existing prefabs, preserving their GUIDs, root IDs
// and colliders. Every placed wall, including future level generations, uses them.
[InitializeOnLoad]
public static class AegisWallSetup {
 const string Root=AegisWallImportSettings.Root;
 const string Environment="Assets/Resources/Environment/Aegis";
 const string SignatureKey="Asterion.CrimsonWallSignature";
 const string ModelPath=Root+"/Model.fbx";
 const string LowMeshPath=Root+"/LowWall.asset";
 const string Version="crimson-wall-prefabs-1";
 static bool queued,building;
 static readonly string[] Textures={"BaseColor","Normal","MetallicSmoothness","Emission"};

 static AegisWallSetup(){
  Queue();
  EditorApplication.playModeStateChanged+=state=>{
   if(state==PlayModeStateChange.EnteredEditMode)Queue();
   if(state==PlayModeStateChange.ExitingEditMode)Build(true);
  };
 }
 public static void Queue(){
  if(queued||building)return;
  queued=true;EditorApplication.delayCall+=()=>Build(false);
 }
 [MenuItem("Asterion/Meshy/Rebuild Crimson Walls")]
 public static void Rebuild(){SessionState.EraseString(SignatureKey);Queue();}

 static void Build(bool enteringPlay){
  queued=false;
  if(building||EditorApplication.isPlaying||(!enteringPlay&&EditorApplication.isPlayingOrWillChangePlaymode))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
  if(!File.Exists(ModelPath)||!File.Exists(Environment+"/WallModule.prefab")||!File.Exists(Environment+"/LowWall.prefab"))return;
  if(Textures.Any(name=>!File.Exists(Root+"/Textures/"+name+".png")))return;
  building=true;
  try{
   var sources=new[]{ModelPath}.Concat(Textures.Select(name=>Root+"/Textures/"+name+".png")).ToArray();
   foreach(string path in sources){
    if(!AssetImporter.GetAtPath(path))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    var importer=AssetImporter.GetAtPath(path);
    if(importer&&importer.userData!=AegisWallImportSettings.Revision)importer.SaveAndReimport();
   }
   string signature=Version+AegisWallImportSettings.Revision+string.Join(";",sources.Select(path=>AssetDatabase.GetAssetDependencyHash(path).ToString()));
   if(SessionState.GetString(SignatureKey,"")==signature&&Ready())return;
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
   if(!source)throw new InvalidOperationException("Crimson-Wandmodell konnte nicht importiert werden.");
   var material=Surface();
   ReplaceWall("WallModule",source,material,false);
   ReplaceWall("LowWall",source,material,true);
   AssetDatabase.SaveAssets();SessionState.SetString(SignatureKey,signature);
   Debug.Log("AEGIS_CRIMSON_WALLS_READY: Wandsegmente und niedrige Vordergrundwände aktualisiert.");
  }catch(Exception e){Debug.LogError("Crimson-Wände: "+e.Message+"\n"+e.StackTrace);}
  finally{building=false;}
 }
 static bool Ready(){
  foreach(string name in new[]{"WallModule","LowWall"}){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Environment+"/"+name+".prefab");
   if(!prefab||!prefab.transform.Find("Crimson wall visual"))return false;
  }
  return AssetDatabase.LoadAssetAtPath<Mesh>(LowMeshPath);
 }
 static Material Surface(){
  string path=Root+"/Surface.mat";
  var shader=Shader.Find("Universal Render Pipeline/Lit");
  if(!shader)throw new InvalidOperationException("URP Lit fehlt.");
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
  material.name="Aegis / Crimson vault armor";material.shader=shader;material.enableInstancing=true;
  Texture2D Texture(string name){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+".png");
   if(!texture)throw new InvalidOperationException("Wandtextur fehlt: "+name);
   return texture;
  }
  material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",Texture("BaseColor"));
  material.SetTexture("_BumpMap",Texture("Normal"));material.SetFloat("_BumpScale",.8f);material.EnableKeyword("_NORMALMAP");
  material.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness"));material.SetFloat("_Metallic",1);
  material.SetFloat("_Smoothness",.8f);material.SetFloat("_SmoothnessTextureChannel",0);material.EnableKeyword("_METALLICSPECGLOSSMAP");
  material.SetTexture("_EmissionMap",Texture("Emission"));material.SetColor("_EmissionColor",Color.white*1.2f);material.EnableKeyword("_EMISSION");
  EditorUtility.SetDirty(material);return material;
 }
 static void ReplaceWall(string name,GameObject source,Material material,bool low){
  string path=Environment+"/"+name+".prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try{
   var collider=root.GetComponent<BoxCollider>();
   if(!collider)throw new InvalidOperationException(name+": Wandkollision fehlt.");
   foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   var visual=new GameObject("Crimson wall visual");visual.transform.SetParent(root.transform,false);
   var model=Object.Instantiate(source,visual.transform,false);model.name="Crimson vault panel";
   foreach(var childCollider in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(childCollider);
   foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
   // Measure in the prefab's coordinate system, independent of the FBX's unit/axis transforms.
   Bounds bounds=GeometryBounds(visual.transform);
   if(Mathf.Min(bounds.size.x,bounds.size.y,bounds.size.z)<.00001f)throw new InvalidOperationException("Ungültige Wandabmessungen.");
   visual.transform.localScale=new Vector3(collider.size.x/bounds.size.x,3.15f/bounds.size.y,collider.size.z/bounds.size.z);
   visual.transform.localPosition=-Vector3.Scale(visual.transform.localScale,new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
   if(low){
    Mesh clipped=ClipBelow(visual,root.transform,collider.size.y-.1f);
    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(LowMeshPath);
    if(saved){EditorUtility.CopySerialized(clipped,saved);Object.DestroyImmediate(clipped);EditorUtility.SetDirty(saved);}
    else{saved=clipped;AssetDatabase.CreateAsset(saved,LowMeshPath);}
    Object.DestroyImmediate(visual);
    visual=new GameObject("Crimson wall visual");visual.transform.SetParent(root.transform,false);
    visual.AddComponent<MeshFilter>().sharedMesh=saved;visual.AddComponent<MeshRenderer>().sharedMaterial=material;
    // Close the cut with a simple bronze cap, without compressing the panel's details.
    var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);cap.name="Bronze cut cap";cap.transform.SetParent(visual.transform,false);
    cap.transform.localPosition=Vector3.up*(collider.size.y-.05f);
    cap.transform.localScale=new Vector3(collider.size.x,.1f,collider.size.z);
    Object.DestroyImmediate(cap.GetComponent<Collider>());
    var bronze=AssetDatabase.LoadAssetAtPath<Material>(Environment+"/Materials/Bronze.mat");
    if(!bronze)throw new InvalidOperationException("Bronzematerial fehlt.");
    cap.GetComponent<Renderer>().sharedMaterial=bronze;
   }
   // Root/collider instances loaded above are retained so scene overrides stay valid.
   if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new InvalidOperationException("Wand-Prefab konnte nicht gespeichert werden: "+name);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 static Bounds GeometryBounds(Transform root){
  Bounds bounds=default;bool found=false;
  foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
   if(!filter.sharedMesh)continue;
   Matrix4x4 matrix=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
   foreach(var vertex in filter.sharedMesh.vertices){
    Vector3 point=matrix.MultiplyPoint3x4(vertex);
    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
   }
  }
  if(!found)throw new InvalidOperationException("Wandmodell enthält keine Geometrie.");
  return bounds;
 }
 struct Vertex {
  public Vector3 position,normal;public Vector2 uv;
  public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{position=Vector3.Lerp(a.position,b.position,t),normal=Vector3.Lerp(a.normal,b.normal,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
 }
 static Mesh ClipBelow(GameObject visual,Transform parent,float height){
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
  foreach(var filter in visual.GetComponentsInChildren<MeshFilter>()){
   var mesh=filter.sharedMesh;if(!mesh)continue;
   var positions=mesh.vertices;var sourceNormals=mesh.normals;var sourceUV=mesh.uv;var indices=mesh.triangles;
   if(sourceNormals.Length!=positions.Length||sourceUV.Length!=positions.Length)throw new InvalidOperationException("Wand-Normalen oder UVs fehlen.");
   Matrix4x4 matrix=parent.worldToLocalMatrix*filter.transform.localToWorldMatrix;
   Matrix4x4 normalMatrix=matrix.inverse.transpose;
   Vertex At(int index)=>new Vertex{position=matrix.MultiplyPoint3x4(positions[index]),normal=normalMatrix.MultiplyVector(sourceNormals[index]).normalized,uv=sourceUV[index]};
   for(int i=0;i<indices.Length;i+=3){
    var face=new[]{At(indices[i]),At(indices[i+(matrix.determinant<0?2:1)]),At(indices[i+(matrix.determinant<0?1:2)])};
    var clipped=new List<Vertex>(4);
    for(int j=0;j<3;j++){
     Vertex a=face[j],b=face[(j+1)%3];bool insideA=a.position.y<=height,insideB=b.position.y<=height;
     if(insideA)clipped.Add(a);
     if(insideA!=insideB)clipped.Add(Vertex.Lerp(a,b,(height-a.position.y)/(b.position.y-a.position.y)));
    }
    for(int j=1;j<clipped.Count-1;j++)foreach(var vertex in new[]{clipped[0],clipped[j],clipped[j+1]}){
     triangles.Add(vertices.Count);vertices.Add(vertex.position);normals.Add(vertex.normal);uvs.Add(vertex.uv);
    }
   }
  }
  if(vertices.Count==0)throw new InvalidOperationException("Niedrige Wand enthält keine Geometrie.");
  var result=new Mesh{name="Crimson wall / lower section"};
  result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uvs);result.SetTriangles(triangles,0);
  result.RecalculateBounds();result.RecalculateTangents();return result;
 }
}

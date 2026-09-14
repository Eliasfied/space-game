using System;
using System.IO;
using System.Linq;
using AsterionGame;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class AegisPropSetup {
 const string Root=AegisPropImportSettings.Root;
 const string Environment="Assets/Resources/Environment/Aegis";
 const string SignatureKey="Asterion.AegisPropSignature";
 const string Version="aegis-prop-prefabs-1";
 const string VisualName="Imported Aegis prop";
 static readonly string[] Textures={"BaseColor","Normal","MetallicSmoothness","Emission"};
 static readonly Definition[] Props={
  new Definition("NeonVault","Cover",2.5f,true,.85f),
  new Definition("SideConsole","Terminal",1.9f,false,.9f),
  new Definition("ArcReactor","Pillar",2.5f,false,1.1f)
 };
 readonly struct Definition {
  public readonly string source,module;public readonly float size,emission;public readonly bool byWidth;
  public Definition(string source,string module,float size,bool byWidth,float emission){this.source=source;this.module=module;this.size=size;this.byWidth=byWidth;this.emission=emission;}
 }
 static bool queued,building;
 static AegisPropSetup(){
  Queue();EditorApplication.playModeStateChanged+=state=>{
   if(state==PlayModeStateChange.EnteredEditMode)Queue();
   if(state==PlayModeStateChange.ExitingEditMode)Build(true);
  };
 }
 public static void Queue(){if(queued||building)return;queued=true;EditorApplication.delayCall+=()=>Build(false);}
 [MenuItem("Asterion/Meshy/Rebuild Aegis Props")]
 public static void Rebuild(){SessionState.EraseString(SignatureKey);Queue();}
 static void Build(bool enteringPlay){
  queued=false;
  if(building||EditorApplication.isPlaying||(!enteringPlay&&EditorApplication.isPlayingOrWillChangePlaymode))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
  string[] sources=Props.SelectMany(prop=>new[]{Root+"/"+prop.source+"/Model.fbx"}.Concat(Textures.Select(name=>Root+"/"+prop.source+"/Textures/"+name+".png"))).ToArray();
  if(sources.Any(path=>!File.Exists(path))||Props.Any(prop=>!File.Exists(Environment+"/"+prop.module+".prefab")))return;
  building=true;
  try{
   foreach(string path in sources){
    if(!AssetImporter.GetAtPath(path))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    var importer=AssetImporter.GetAtPath(path);
    if(importer&&importer.userData!=AegisPropImportSettings.Revision)importer.SaveAndReimport();
   }
   string signature=Version+AegisPropImportSettings.Revision+string.Join(";",sources.Select(path=>AssetDatabase.GetAssetDependencyHash(path).ToString()));
   if(SessionState.GetString(SignatureKey,"")==signature&&Ready()){SyncCoverMap();return;}
   foreach(var prop in Props)Replace(prop);
   AssetDatabase.SaveAssets();SyncCoverMap();SessionState.SetString(SignatureKey,signature);
   Debug.Log("AEGIS_PROPS_READY: Neon-Vault-Deckungen, Seitenkonsolen und Reaktorkapseln ersetzen die bisherigen Platzhalter.");
  }catch(Exception e){Debug.LogError("Aegis-Objekte: "+e.Message+"\n"+e.StackTrace);}
  finally{building=false;}
 }
 static bool Ready()=>Props.All(prop=>{
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Environment+"/"+prop.module+".prefab");
  return prefab&&prefab.transform.Find(VisualName);
 });
 static Material Surface(Definition prop){
  string folder=Root+"/"+prop.source,path=folder+"/Surface.mat";
  var shader=Shader.Find("Universal Render Pipeline/Lit");
  if(!shader)throw new InvalidOperationException("URP Lit fehlt.");
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
  Texture2D Texture(string name){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+name+".png");
   if(!texture)throw new InvalidOperationException(prop.source+": Textur fehlt: "+name);
   return texture;
  }
  material.shader=shader;material.enableInstancing=true;material.SetColor("_BaseColor",Color.white);
  material.SetTexture("_BaseMap",Texture("BaseColor"));material.SetTexture("_BumpMap",Texture("Normal"));
  material.SetFloat("_BumpScale",.8f);material.EnableKeyword("_NORMALMAP");
  material.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness"));material.SetFloat("_Metallic",1);
  material.SetFloat("_Smoothness",.8f);material.SetFloat("_SmoothnessTextureChannel",0);material.EnableKeyword("_METALLICSPECGLOSSMAP");
  material.SetTexture("_EmissionMap",Texture("Emission"));material.SetColor("_EmissionColor",Color.white*prop.emission);material.EnableKeyword("_EMISSION");
  EditorUtility.SetDirty(material);return material;
 }
 static void Replace(Definition prop){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+prop.source+"/Model.fbx");
  if(!source)throw new InvalidOperationException(prop.source+": Modell konnte nicht importiert werden.");
  Material material=Surface(prop);
  string path=Environment+"/"+prop.module+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{
   var collider=root.GetComponent<BoxCollider>();
   if(!collider)throw new InvalidOperationException(prop.module+": Kollision fehlt.");
   // Keep the existing prefab/root IDs; remove all of the old blockout visuals.
   foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   var visual=new GameObject(VisualName);visual.transform.SetParent(root.transform,false);
   // Supplied models face +Z; level modules conventionally face -Z.
   visual.transform.localRotation=Quaternion.Euler(0,180,0);
   var model=Object.Instantiate(source,visual.transform,false);model.name=prop.source;
   foreach(var body in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(body);
   foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
   Bounds bounds=GeometryBounds(root.transform);
   float measured=prop.byWidth?Mathf.Max(bounds.size.x,bounds.size.z):bounds.size.y;
   if(measured<.0001f)throw new InvalidOperationException(prop.source+": Ungültige Abmessungen.");
   visual.transform.localScale=Vector3.one*(prop.size/measured);
   bounds=GeometryBounds(root.transform);
   visual.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   bounds=GeometryBounds(root.transform);
   collider.size=bounds.size;collider.center=bounds.center;collider.isTrigger=false;collider.enabled=true;
   if(!PrefabUtility.SaveAsPrefabAsset(root,path))throw new InvalidOperationException(prop.module+": Prefab konnte nicht gespeichert werden.");
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
  if(!found)throw new InvalidOperationException("Objekt enthält keine Geometrie.");
  return bounds;
 }
 static void SyncCoverMap(){
  string path=Environment+"/LevelLayout.json";
  if(!File.Exists(path))return;
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Environment+"/Cover.prefab");
  var collider=prefab?prefab.GetComponent<BoxCollider>():null;if(!collider)return;
  var layout=JsonUtility.FromJson<AegisLevelLayout>(File.ReadAllText(path));
  if(layout?.covers==null)return;
  bool changed=false;
  for(int i=0;i<layout.covers.Length;i++){
   var cover=layout.covers[i];
   if(Mathf.Abs(cover.width-collider.size.x)<.0001f&&Mathf.Abs(cover.depth-collider.size.z)<.0001f)continue;
   cover.x+=(cover.width-collider.size.x)*.5f;cover.z+=(cover.depth-collider.size.z)*.5f;
   cover.width=collider.size.x;cover.depth=collider.size.z;layout.covers[i]=cover;changed=true;
  }
  if(changed){File.WriteAllText(path,JsonUtility.ToJson(layout,true)+"\n");AssetDatabase.ImportAsset(path);}
 }
}

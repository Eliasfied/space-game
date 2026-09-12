using UnityEngine;
using UnityEngine.Rendering;
namespace AsterionGame {
 public static class CombatFx {
  public static readonly Color Cyan=new Color(.12f,.92f,1),Amber=new Color(1,.29f,.07f);
  static Material lineMaterial;
  public static Material Unlit(Color color,bool alpha=false){
   var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",color);
   if(alpha){m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
   return m;
  }
  public static Material LineMat {get{if(!lineMaterial){lineMaterial=Resources.Load<Material>("FxLine");if(!lineMaterial)lineMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));}return lineMaterial;}}
  public static LineRenderer Line(string name,Color color,float width){
   var go=new GameObject(name);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=LineMat;l.startColor=color;l.endColor=color;l.widthMultiplier=width;l.useWorldSpace=true;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.numCapVertices=3;return l;
  }
  public static LineRenderer Ring(string name,Vector3 center,float radius,Color color,float width){var l=Line(name,color,width);l.loop=true;l.positionCount=96;SetRing(l,center,radius);return l;}
  public static void SetRing(LineRenderer line,Vector3 center,float radius){for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/line.positionCount;line.SetPosition(i,center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius);}}
  public static GameObject Disc(Vector3 center,float radius,Color color){
   var go=new GameObject("Telegraph fill");go.transform.position=center;
   var mesh=new Mesh();Vector3[] v=new Vector3[65];int[] triangles=new int[64*3];for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;v[i+1]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;triangles[i*3]=0;triangles[i*3+1]=(i+1)%64+1;triangles[i*3+2]=i+1;}Vector2[] uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++)uv[i]=new Vector2(v[i].x,v[i].z)/(radius*2)+Vector2.one*.5f;mesh.vertices=v;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;
   var r=go.AddComponent<MeshRenderer>();var telegraph=Resources.Load<Material>("GroundTelegraph");r.sharedMaterial=telegraph?new Material(telegraph):Unlit(color,true);r.sharedMaterial.SetColor("_BaseColor",color);r.shadowCastingMode=ShadowCastingMode.Off;var own=go.AddComponent<OwnedResources>();own.mesh=mesh;own.material=r.sharedMaterial;return go;
  }
  public static void Beam(Vector3 from,Vector3 to,Color color,float width,float life){var l=Line("Energy beam",color*1.8f,width);l.positionCount=2;l.SetPosition(0,from);l.SetPosition(1,to);l.gameObject.AddComponent<FadingLine>().Setup(l,life);}
  public static void MuzzleBeam(Transform muzzle,Vector3 origin,Vector3 end,Color color,float width,float life){
   var line=Line("Weapon beam",color*1.8f,width);line.positionCount=2;line.SetPosition(0,origin);line.SetPosition(1,end);
   var fade=line.gameObject.AddComponent<FadingLine>();fade.Setup(line,life);fade.muzzle=muzzle;
  }
  public static void Shockwave(Vector3 at,Color color,float radius){var l=Ring("Shockwave",at+Vector3.up*.12f,.1f,color,.12f);l.gameObject.AddComponent<FadingLine>().Setup(l,.5f,radius,at+Vector3.up*.12f);}
  public static void Burst(Vector3 at,Color color,int count,float radius){for(int i=0;i<count;i++){Vector3 d=Random.onUnitSphere;d.y=Mathf.Abs(d.y)*.6f;var l=Line("Spark",color,.04f);l.positionCount=2;l.gameObject.AddComponent<Spark>().Setup(l,at,d*Random.Range(radius*1.5f,radius*4),Random.Range(.18f,.5f));}}
  public static void PistolFlash(Transform socket,Vector3 origin,Vector3 direction,Color color){
   var fx=new GameObject("Pistol ignition").AddComponent<PistolIgnition>();fx.Setup(socket,origin,direction,color);
  }
  public static void PistolImpact(Vector3 at,Vector3 direction,Color color){
   Vector3 axis=Vector3.Cross(direction,Vector3.up).normalized;if(axis.sqrMagnitude<.1f)axis=Vector3.right;
   Vector3 up=Vector3.Cross(axis,direction).normalized;
   Beam(at-axis*.21f,at+axis*.21f,color,.08f,.1f);Beam(at-up*.21f,at+up*.21f,Color.white,.045f,.08f);
   Burst(at,color*1.6f,7,.55f);
  }
  public static HostileProjectile Projectile(Vector3 at,Vector3 direction,float speed,float damage){
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Plasma bolt";Object.Destroy(go.GetComponent<Collider>());go.transform.localScale=Vector3.one*.33f;var r=go.GetComponent<Renderer>();r.sharedMaterial=LineMat;var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",Amber*2);r.SetPropertyBlock(props);
   var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=LineMat;trail.startColor=Amber;trail.endColor=new Color(.3f,.03f,0);trail.startWidth=.22f;trail.endWidth=0;trail.time=.13f;trail.minVertexDistance=.1f;
   var p=go.AddComponent<HostileProjectile>();p.Setup(at,direction,speed,damage);return p;
  }
 }
 sealed class OwnedResources:MonoBehaviour{public Mesh mesh;public Material material;void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}}
 sealed class FadingLine:MonoBehaviour {
  public Transform muzzle;
  void LateUpdate(){if(muzzle&&line)line.SetPosition(0,muzzle.position);}
  LineRenderer line;float life,born,width,radius;Vector3 center;
  public void Setup(LineRenderer l,float seconds,float endRadius=0,Vector3 at=default){line=l;life=seconds;born=Time.time;width=l.widthMultiplier;radius=endRadius;center=at;}
  void Update(){float t=(Time.time-born)/life;if(t>=1){Destroy(gameObject);return;}line.widthMultiplier=width*(1-t);if(radius>0)CombatFx.SetRing(line,center,Mathf.Lerp(.1f,radius,t));}
 }
 sealed class Spark:MonoBehaviour {
  LineRenderer line;Vector3 at,velocity;float life,born;
  public void Setup(LineRenderer l,Vector3 p,Vector3 v,float duration){line=l;at=p;velocity=v;life=duration;born=Time.time;}
  void Update(){float t=Time.time-born;if(t>=life){Destroy(gameObject);return;}Vector3 p=at+velocity*t+Vector3.down*t*t*2;line.SetPosition(0,p);line.SetPosition(1,p-velocity*.04f);line.widthMultiplier=.04f*(1-t/life);}
 }
}

namespace AsterionGame {
 sealed class PistolIgnition:MonoBehaviour {
  Transform socket;Vector3 origin,direction;Color color;LineRenderer flare,core,halo;float born;
  public void Setup(Transform anchor,Vector3 at,Vector3 forward,Color tint){
   socket=anchor;origin=at;direction=forward;color=tint;born=Time.time;
   flare=CombatFx.Line("Muzzle flare",tint*3,.23f);flare.positionCount=2;
   core=CombatFx.Line("Muzzle white core",new Color(3,2.7f,2),.085f);core.positionCount=2;
   halo=CombatFx.Line("Muzzle pulse",tint*2,.04f);halo.loop=true;halo.positionCount=24;
   flare.transform.SetParent(transform);core.transform.SetParent(transform);halo.transform.SetParent(transform);Draw();
  }
  void LateUpdate(){if(Time.time-born>=.13f){Destroy(gameObject);return;}Draw();}
  void Draw(){
   if(socket)origin=socket.position;float t=Mathf.Clamp01((Time.time-born)/.13f),fade=1-t;
   float length=.65f*fade+.12f;
   flare.SetPosition(0,origin);flare.SetPosition(1,origin+direction*length);flare.startWidth=.24f*fade;flare.endWidth=0;
   core.SetPosition(0,origin);core.SetPosition(1,origin+direction*length*.85f);core.startWidth=.09f*fade;core.endWidth=0;
   Vector3 right=Vector3.Cross(direction,Vector3.up).normalized;if(right.sqrMagnitude<.1f)right=Vector3.right;
   Vector3 up=Vector3.Cross(right,direction).normalized;
   for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;halo.SetPosition(i,origin+direction*.1f+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*Mathf.Lerp(.06f,.28f,t));}
   halo.widthMultiplier=.045f*fade;
  }
 }
}

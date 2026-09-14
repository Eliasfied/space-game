using UnityEngine;
using UnityEngine.Rendering;
namespace AsterionGame {
 // A reusable mesh: aim movement does not allocate meshes or materials each frame.
 public sealed class AbilityAimPreview:MonoBehaviour {
  const int Segments=64;
  Mesh mesh;Material material;LineRenderer edge,rangeRing,guide;MeshRenderer fill;
  readonly Vector3[] vertices=new Vector3[Segments+2];
  readonly int[] triangles=new int[Segments*3];
  void Awake(){
   mesh=new Mesh{name="Skillshot preview"};mesh.MarkDynamic();
   for(int i=0;i<Segments;i++){triangles[i*3]=0;triangles[i*3+1]=i+2;triangles[i*3+2]=i+1;}
   var surface=Resources.Load<Material>("GroundTelegraph");material=surface?new Material(surface):CombatFx.Unlit(Color.cyan,true);
   material.SetFloat("_Cull",0);
   gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;fill=gameObject.AddComponent<MeshRenderer>();fill.sharedMaterial=material;fill.shadowCastingMode=ShadowCastingMode.Off;fill.receiveShadows=false;
   edge=CombatFx.Line("Aim boundary",CombatFx.Cyan,.075f);edge.transform.SetParent(transform);edge.loop=true;
   rangeRing=CombatFx.Ring("Cast range",Vector3.zero,1,new Color(.1f,.65f,.8f,.45f),.025f);rangeRing.transform.SetParent(transform);
   guide=CombatFx.Line("Aim direction",CombatFx.Cyan,.045f);guide.transform.SetParent(transform);guide.positionCount=2;
  }
  public void Draw(AbilityDefinition ability,Vector3 origin,Vector3 point,Vector3 direction,bool available=true){
   origin.y=.09f;point.y=.09f;
   bool circle=ability.AimShape==AbilityAimShape.Circle,dash=ability.AimShape==AbilityAimShape.Dash;
   Vector3 center=circle?point:origin;transform.position=center;transform.rotation=Quaternion.identity;
   Color color=available?CombatFx.Cyan:new Color(1,.35f,.25f);
   edge.startColor=edge.endColor=color;guide.startColor=guide.endColor=color;
   material.SetColor("_BaseColor",new Color(color.r,color.g,color.b,.12f));
   rangeRing.enabled=circle;if(circle)CombatFx.SetRing(rangeRing,origin,ability.range);
   guide.SetPosition(0,origin);guide.SetPosition(1,circle?point:origin+direction*ability.AimRange);
   if(dash){
    // Swept kick lane with the rounded reach of the forward hit sphere.
    Vector3 right=Vector3.Cross(Vector3.up,direction);float r=ability.AimRadius,end=ability.AimRange-r;
    vertices[0]=direction*(end*.5f);
    for(int i=0;i<=Segments;i++){
     float a=i/(float)Segments*Mathf.PI*2;
     // Rounded rectangle, spanning the lunge plus the front of its hit sphere.
     vertices[i+1]=direction*(Mathf.Sin(a)>=0?end:0)+direction*Mathf.Sin(a)*r+right*Mathf.Cos(a)*r;
    }
   }else{
    vertices[0]=Vector3.zero;float sweep=circle?360:ability.AimAngle;
    for(int i=0;i<=Segments;i++)vertices[i+1]=Quaternion.AngleAxis(-sweep*.5f+sweep*i/Segments,Vector3.up)*direction*(circle?ability.AimRadius:ability.AimRange);
   }
   mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();
   edge.positionCount=Segments+(circle||dash?1:3);int offset=circle||dash?0:1;
   if(offset>0){edge.SetPosition(0,center);edge.SetPosition(edge.positionCount-1,center);}
   for(int i=0;i<=Segments;i++)edge.SetPosition(i+offset,center+vertices[i+1]);
  }
  void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
 }
}

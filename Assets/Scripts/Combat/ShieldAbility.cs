using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Shield")]
 public sealed class ShieldAbility:AbilityDefinition {
  public float duration=3;[Range(0,1)]public float reduction=.25f;
  public override void Execute(AbilityContext c){c.caster.GetComponent<Health>().Protect(reduction,duration);var fx=c.caster.GetComponent<ShieldVisual>();if(!fx)fx=c.caster.gameObject.AddComponent<ShieldVisual>();fx.Setup(duration);}
 }
 public sealed class ShieldVisual:MonoBehaviour {
  float until,born,hitUntil,height;GameObject bubble;Material material;Health health;
  readonly LineRenderer[] arcs=new LineRenderer[3];
  public void Setup(float duration){
   born=Time.time;until=born+duration;
   if(!health){health=GetComponent<Health>();health.Damaged+=OnHit;}
   if(!bubble){
    var motor=GetComponent<PlayerMotor>();height=2.513f*(motor&&motor.visual?motor.visual.localScale.y:1)+.35f;
    bubble=GameObject.CreatePrimitive(PrimitiveType.Sphere);bubble.name="Aegis / energy shell";Destroy(bubble.GetComponent<Collider>());bubble.transform.SetParent(transform,false);
    bubble.transform.localPosition=Vector3.up*(height*.5f);
    var shader=Resources.Load<Shader>("ShieldShell");material=shader?new Material(shader):CombatFx.Unlit(new Color(.1f,.65f,1,.16f),true);
    var renderer=bubble.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    for(int i=0;i<3;i++){
     arcs[i]=CombatFx.Line("Aegis / rotating energy arc",CombatFx.Cyan*2,.045f);arcs[i].positionCount=48;arcs[i].useWorldSpace=false;arcs[i].transform.SetParent(bubble.transform,false);
    }
   }
   CombatFx.Shockwave(transform.position,CombatFx.Cyan*1.8f,1.65f);SynthAudio.Play(SoundKind.Charge,transform.position,.28f);Draw();
  }
  void OnHit(float damage){hitUntil=Time.time+.2f;}
  void Update(){if(Time.time>=until||!health||!health.Alive){Destroy(this);return;}Draw();}
  void Draw(){
   float age=Time.time-born,fade=Mathf.Clamp01((until-Time.time)/.28f);
   float growth=Mathf.Lerp(.15f,1,1-Mathf.Pow(1-Mathf.Clamp01(age/.2f),3));
   float hit=Mathf.Clamp01((hitUntil-Time.time)/.2f);
   bubble.transform.localScale=new Vector3(2.65f,height,2.65f)*growth*(.94f+.06f*fade+.035f*hit);
   if(material.HasProperty("_Opacity"))material.SetFloat("_Opacity",fade);
   if(material.HasProperty("_Hit"))material.SetFloat("_Hit",hit);
   for(int n=0;n<arcs.Length;n++){
    Quaternion rotation=Quaternion.Euler(35+n*52,age*(n%2==0?65:-50)+n*120,20);
    for(int i=0;i<48;i++){float a=i/47f*Mathf.PI*1.45f;arcs[n].SetPosition(i,rotation*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.505f);}
    arcs[n].widthMultiplier=(.022f+hit*.018f)*fade;
    arcs[n].startColor=arcs[n].endColor=Color.Lerp(CombatFx.Cyan*2,new Color(3,3.5f,4),hit);
   }
  }
  void OnDestroy(){if(health)health.Damaged-=OnHit;if(bubble)Destroy(bubble);if(material)Destroy(material);}
 }
}

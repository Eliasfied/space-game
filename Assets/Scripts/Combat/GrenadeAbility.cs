using UnityEngine;
namespace AsterionGame {
 [CreateAssetMenu(menuName="Asterion/Abilities/Grenade")]
 public sealed class GrenadeAbility:AbilityDefinition {
  public float radius=2.5f,knockbackDistance;public bool forwardThrow;public Color effectColor=new Color(.2f,1,.72f);
  public float dotDuration,dotDamage,slowFraction;
  public override bool RequiresTarget=>true;
  public override bool RequiresGroundPoint=>false;
  public override void Execute(AbilityContext c){
   Vector3 point=c.point,origin=c.caster.transform.position+Vector3.up*1.3f;
   if(forwardThrow&&!c.target)point=c.caster.transform.position+c.direction*range;
   point.y=.05f;
   Vector3 path=point+Vector3.up*1.25f-origin;
   if(Physics.Raycast(origin,path.normalized,out RaycastHit hit,path.magnitude,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))point=hit.point-path.normalized*.3f;
   point.y=.05f;Vector2 flat=Vector2.ClampMagnitude(new Vector2(point.x,point.z),13);point.x=flat.x;point.z=flat.y;
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Ion grenade";Destroy(go.GetComponent<Collider>());go.transform.localScale=Vector3.one*.2f;
   go.GetComponent<Renderer>().sharedMaterial=CombatFx.LineMat;var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",effectColor*2);go.GetComponent<Renderer>().SetPropertyBlock(props);
   go.AddComponent<GrenadeFlight>().Setup(origin,point,this,c.caster,c.direction,c.target);
  }
 }
 public sealed class GrenadeFlight:MonoBehaviour {
  Vector3 start,end,direction;float born;GrenadeAbility ability;AbilityCaster source;Health target;
  public void Setup(Vector3 from,Vector3 to,GrenadeAbility ability,AbilityCaster source,Vector3 direction,Health target=null){start=from;end=to;this.ability=ability;this.source=source;this.direction=direction;this.target=target;born=Time.time;transform.position=from;}
  void Update(){
   if(Time.deltaTime<=0)return;
   if(target&&target.Alive){end=target.transform.position;end.y=.05f;}
   float t=Mathf.Clamp01((Time.time-born)/.45f);Vector3 position=Vector3.Lerp(start,end,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.5f;
   if(Physics.Linecast(transform.position,position,out RaycastHit hit,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)){end=hit.point;end.y=.05f;t=1;}
   transform.position=position;if(t<1)return;
   var go=new GameObject("Grenade blast");go.AddComponent<GroundHazard>().Setup(end,ability.radius,.15f,ability.damage,Team.Hostile,ability.effectColor,false,source,ability.energyOnHit,ability.knockbackDistance,direction,ability.dotDuration,ability.dotDamage,ability.slowFraction);Destroy(gameObject);
  }
 }
}

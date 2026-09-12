using System.Collections.Generic;
using UnityEngine;
namespace AsterionGame {
 [RequireComponent(typeof(Health))]
 public sealed class EnemyHealthBar : MonoBehaviour {
  public static readonly List<EnemyHealthBar> All=new List<EnemyHealthBar>();
  public string displayName="Gegner";public float verticalOffset=.45f;
  public Health Health {get;private set;}Collider body;
  public Vector3 WorldPosition=>new Vector3(transform.position.x,body?body.bounds.max.y+verticalOffset:transform.position.y+2.5f,transform.position.z);
  void Awake(){Health=GetComponent<Health>();body=GetComponent<Collider>();}
  void OnEnable(){if(!All.Contains(this))All.Add(this);}
  void OnDisable(){All.Remove(this);}
 }
}

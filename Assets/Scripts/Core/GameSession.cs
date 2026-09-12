using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace AsterionGame {
 [DefaultExecutionOrder(-200)]
 public sealed class GameSession:MonoBehaviour {
  public PlayerInputReader input;public Health player,boss;
  public bool Paused{get;private set;}public bool IsSelecting{get;private set;}
  public PlayerClassDefinition ActiveClass{get;private set;}
  public bool Ended=>!player.Alive||!boss.Alive;public bool Won=>!boss.Alive;public float Elapsed{get;private set;}
  ClassSelectionScreen selection;bool launching;
  void Awake(){IsSelecting=true;Time.timeScale=0;Application.targetFrameRate=120;Cursor.visible=true;}
  void Start(){selection=gameObject.AddComponent<ClassSelectionScreen>();selection.Initialize(this);}
  void Update(){
   input.MouseLookAllowed=!IsSelecting&&!Paused&&!Ended;
   if(IsSelecting)return;
   if(input.RestartPressed){Restart();return;}
   if(input.PausePressed&&!Ended)TogglePause();if(!Paused&&!Ended)Elapsed+=Time.deltaTime;if(Ended){Cursor.visible=true;player.GetComponent<AbilityCaster>().Statistics.Finish();}
  }
  public void BeginMission(PlayerClassDefinition definition){if(launching||!definition||!definition.Ready)return;StartCoroutine(Deploy(definition));}
  IEnumerator Deploy(PlayerClassDefinition definition){
   launching=true;
   var loadout=player.GetComponent<PlayerLoadout>();if(!loadout)loadout=player.gameObject.AddComponent<PlayerLoadout>();loadout.Apply(definition);ActiveClass=definition;
   // The menu click must be released before it can become a weapon shot.
   yield return null;
   while(Pointer.current!=null&&Pointer.current.press.isPressed)yield return null;
   if(selection)Destroy(selection);IsSelecting=false;launching=false;Paused=false;Elapsed=0;Time.timeScale=1;Cursor.visible=true;
  }
  public void TogglePause(){if(IsSelecting)return;Paused=!Paused;Time.timeScale=Paused?0:1;Cursor.visible=true;}
  public void Restart(){Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
  void OnApplicationFocus(bool focused){if(!focused&&!IsSelecting&&!Paused&&!Ended)TogglePause();}
  void OnDestroy(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 }
}

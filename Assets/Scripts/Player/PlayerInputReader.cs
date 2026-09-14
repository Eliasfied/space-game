using UnityEngine;
using UnityEngine.InputSystem;
namespace AsterionGame {
 // All device input stays here. A future network command stream can replace this component.
 [DefaultExecutionOrder(-100)]
 public sealed class PlayerInputReader : MonoBehaviour {
  public static readonly string[] SlotKeys={"1","2","3","4","5","F","C","V"};
  InputAction[] extra=new InputAction[5];
  public bool AbilityPressed(int slot)=>slot>=3&&slot<8&&extra[slot-3].WasPressedThisFrame();
  InputAction move, strafe, pointer, fire, dash, orbital, pause, restart,target,select,look,mouseDelta;
  public Vector2 Move => move.ReadValue<Vector2>(); public Vector2 Pointer => pointer.ReadValue<Vector2>();
  public bool MovementKeysHeld {
   get {
    // Opposite keys still override mouse-forward even when their axes cancel.
    foreach(var control in move.controls)
     if(control is UnityEngine.InputSystem.Controls.ButtonControl key&&key.isPressed)return true;
    return false;
   }
  }
  public float Strafe => strafe.ReadValue<float>();
  public bool TargetPressed=>target.WasPressedThisFrame();public bool SelectPressed{get;private set;}
  public bool MouseLookAllowed{get;set;}=true;
  public bool AbilityAiming{get;private set;}
  public bool AimConfirmPressed=>select.WasPressedThisFrame()&&!PointerOverActionBar;
  public bool AimCancelPressed=>look.WasPressedThisFrame()||pause.WasPressedThisFrame();
  public bool PointerOverActionBar {
   get{float scale=Screen.height/900f;return Pointer.y<170*scale&&Mathf.Abs(Pointer.x-Screen.width*.5f)<270*scale;}
  }
  bool waitForLookRelease;
  public void SetAbilityAiming(bool active){
   AbilityAiming=active;suppressClick=true;SelectPressed=false;
   if(active){MouseLookActive=false;ReleaseCursor();}
   else if(look.IsPressed())waitForLookRelease=true;
  }
  public bool MouseLookActive{get;private set;}
  public bool MouseForward=>MouseLookActive&&select.IsPressed();
  public Vector2 LookDelta=>MouseLookActive&&!look.WasPressedThisFrame()?mouseDelta.ReadValue<Vector2>():Vector2.zero;
  Vector2 clickStart,savedPointer;bool suppressClick,ownsCursor;
  void Update(){
   if(!look.IsPressed())waitForLookRelease=false;
   bool canLook=MouseLookAllowed&&Time.timeScale>0&&!AbilityAiming;
   MouseLookActive=canLook&&!waitForLookRelease&&look.IsPressed();
   SelectPressed=false;
   if(select.WasPressedThisFrame()){clickStart=Pointer;suppressClick=look.IsPressed()||!canLook;}
   if(select.IsPressed()&&(look.IsPressed()||(Pointer-clickStart).sqrMagnitude>25))suppressClick=true;
   if(select.WasReleasedThisFrame())SelectPressed=canLook&&!suppressClick&&!look.IsPressed();
   if(MouseLookActive&&!ownsCursor){savedPointer=Pointer;ownsCursor=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
   else if(!MouseLookActive)ReleaseCursor();
  }
  void ReleaseCursor(){if(!ownsCursor)return;ownsCursor=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(Mouse.current!=null)Mouse.current.WarpCursorPosition(savedPointer);}
  void OnApplicationFocus(bool focused){if(!focused){MouseLookActive=false;suppressClick=true;ReleaseCursor();}}
  public bool FireHeld => fire.IsPressed();
  public bool DashPressed => dash.WasPressedThisFrame(); public bool OrbitalPressed => orbital.WasPressedThisFrame();
  public bool PausePressed => pause.WasPressedThisFrame()&&!AbilityAiming; public bool RestartPressed => restart.WasPressedThisFrame();
  void Awake() {
   for(int i=0;i<5;i++)extra[i]=new InputAction("Ability"+(i+4),InputActionType.Button,"<Keyboard>/"+SlotKeys[i+3].ToLowerInvariant());
   // Normalize once in the motor, after combining WASD with Q/E.
   move = new InputAction("Move",InputActionType.Value); move.AddCompositeBinding("2DVector(mode=1)").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
   strafe = new InputAction("Strafe",InputActionType.Value);strafe.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/q").With("Positive","<Keyboard>/e");
   target=new InputAction("Target",InputActionType.Button,"<Keyboard>/tab");
   pointer = new InputAction("Aim",InputActionType.Value,"<Pointer>/position");
   fire = new InputAction("Laser",InputActionType.Button,"<Keyboard>/1");
   select = new InputAction("Select target",InputActionType.Button,"<Mouse>/leftButton");
   look=new InputAction("Steer player",InputActionType.Button,"<Mouse>/rightButton");
   mouseDelta=new InputAction("Mouse delta",InputActionType.Value,"<Mouse>/delta");
   dash = new InputAction("Phase",InputActionType.Button,"<Keyboard>/2");
   orbital = new InputAction("Orbital",InputActionType.Button,"<Keyboard>/3");
   pause = new InputAction("Pause",InputActionType.Button,"<Keyboard>/escape"); restart = new InputAction("Restart",InputActionType.Button,"<Keyboard>/r");
  }
  void OnEnable() { foreach(var a in Actions()) a.Enable(); }
  void OnDisable() { MouseLookActive=false;ReleaseCursor();foreach(var a in Actions()) a.Disable(); }
  void OnDestroy() { foreach(var a in Actions()) a.Dispose(); }
  InputAction[] Actions() => new[]{move,strafe,pointer,fire,dash,orbital,pause,restart,target,select,look,mouseDelta,extra[0],extra[1],extra[2],extra[3],extra[4]};
 }
}

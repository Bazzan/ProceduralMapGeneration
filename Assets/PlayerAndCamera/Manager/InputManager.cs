using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static PlayerInputAction inputActions;

    
    public static Vector2 mouseDirection;
    public static Vector2 WasdInput;

    private PlayerController playerController;

    private void Awake()
    {
        inputActions = new PlayerInputAction();
        playerController = StaticRefrences.PlayerTransform.GetComponent<PlayerController>();
    
    }

    private void OnEnable()
    {
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Fire.performed += OnAttack;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        WasdInput = inputActions.Player.Move.ReadValue<Vector2>();
        mouseDirection = inputActions.Player.Look.ReadValue<Vector2>();
    }


    public void OnJump(InputAction.CallbackContext callbackContext)
    {
        playerController.Jump();
    }
    public void OnAttack(InputAction.CallbackContext callbackContext)
    {

    }

}

using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator playerAnimator;
    
    private int VelocityY;
    private int VelocityX;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        VelocityY = Animator.StringToHash("VelocityY");
        VelocityX = Animator.StringToHash("VelocityX");
    }


    // Update is called once per frame
    void LateUpdate()
    {
        playerAnimator.SetFloat(VelocityY, Mathf.Clamp(InputManager.WasdInput.y, -1, 1));
        playerAnimator.SetFloat(VelocityX, Mathf.Clamp(InputManager.WasdInput.x, -1, 1));
    }
}

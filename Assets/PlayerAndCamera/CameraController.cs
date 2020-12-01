using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 CameraOffset;
    public float SmoothValue;
    public float CameraSpeed;
    public float MaxPitchAngel;
    public float MinPitchAngel;
    public float maxDegreesToRotate;
    public float PitchSpeed;

    [Header("Collision values")]
    public float CameraCollider;
    public float CameraCollisionDistance;
    public LayerMask CameraCollisionMask;

    private float cameraMaxDistance;
    private Vector3 cameraTarget;
    private Transform playerTransform;
    private Vector3 cameraVelocity;
    private RaycastHit rayHit;
    private Vector3 collisonCameraOffset;
    private Transform cameraTransform;
    private float pitchAngel;
    private void Awake()
    {
        playerTransform = StaticRefrences.PlayerTransform;
        cameraTransform = transform.GetChild(0).transform;
        cameraTransform.localPosition = CameraOffset;
        transform.position = playerTransform.position;
        CameraCollider = cameraTransform.GetComponent<SphereCollider>().radius;
        cameraMaxDistance = Mathf.Abs(CameraOffset.z);
        //collisonCameraOffset.z = CameraOffset.z;
        collisonCameraOffset = CameraOffset;

    }

    private void FixedUpdate()
    {


    }
    private void LateUpdate()
    {
        MoveCamera();
        RotateCameraWithPlayer();
        PitchRotation();
        CheckForCollision();
    }


    private void CheckForCollision()
    {
        Vector3 cameraDirection = cameraTransform.position - transform.position;


        if(Physics.SphereCast(transform.position, CameraCollider, cameraDirection, out rayHit, cameraMaxDistance, CameraCollisionMask))
        {
            collisonCameraOffset.z = -rayHit.distance;
            cameraTransform.localPosition = collisonCameraOffset;
        }
        else
        {
            cameraTransform.localPosition = CameraOffset;
        }
        //Debug.Log(rayHit.collider.name);

    }

    private void MoveCamera()
    {
        cameraTarget = playerTransform.position;
        //if(Vector3.Distance(cameraTarget, transform.position) > 0.5f)
            transform.position = Vector3.SmoothDamp(transform.position , cameraTarget + playerTransform.up *2f, ref cameraVelocity, SmoothValue, CameraSpeed);
    }

    public void PitchRotation()
    {
        pitchAngel -= InputManager.mouseDirection.y * PitchSpeed;
        pitchAngel = Mathf.Clamp(pitchAngel, MinPitchAngel, MaxPitchAngel);
        transform.localRotation = Quaternion.Euler(pitchAngel, transform.rotation.eulerAngles.y, 0f);
    }

    public void RotateCameraWithPlayer()
    {
        transform.rotation = Quaternion.Euler(transform.rotation.x, playerTransform.rotation.eulerAngles.y, 0f);
    }
}

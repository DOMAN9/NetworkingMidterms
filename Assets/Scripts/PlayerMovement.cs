using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float maxVelocityChange = 10f;

    [Header("Mouse Look Settings")]
    [SerializeField] float mouseSensitivity = 1f;
    [SerializeField] float verticalLookLimit = 80f;
    public Transform cameraTransform;

    [Header("Stamina Settings")]
    [SerializeField] float runDuration = 3f;
    [SerializeField] float staminaRegenRate = 1f;

    Rigidbody rb;
    Animator anim;

    // input & rotation
    Vector2 input;
    float rotationX = 0f;
    float rotationY = 0f;

    // stamina
    float currentRunTime = 0f;
    bool canRun = true;

    bool crouching = false;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        if (IsOwner)
        {
            // assign main camera if none
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // disable on non?owners
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(false);
            enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // ??? INPUT ?????????????????????????????????????????????????????
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input.Normalize();

        // ??? MOUSE LOOK ???????????????????????????????????????????????
        float mX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // pitch (up/down)
        rotationX = Mathf.Clamp(rotationX - mY, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        // yaw (left/right)
        rotationY += mX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        // ??? ANIMATIONS ??????????????????????????????????????????????
        anim.SetBool("Walk", input.magnitude > 0f);

        if (Input.GetKeyDown(KeyCode.LeftControl))
            crouching = !crouching;
        anim.SetBool("Crouch", crouching);

        // ??? JUMP ????????????????????????????????????????????????????
        if (Input.GetKeyDown(KeyCode.Space))
            rb.AddForce(Vector3.up * 500f);
        anim.SetBool("Jump", Mathf.Abs(rb.linearVelocity.y) > 0.1f);

        // ??? SPRINT & STAMINA ???????????????????????????????????????
        bool isMoving = input.magnitude > 0f;
        bool isTryingRun = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = walkSpeed;

        if (isTryingRun && canRun && isMoving)
        {
            targetSpeed = runSpeed;
            currentRunTime += Time.deltaTime;
            if (currentRunTime >= runDuration)
                canRun = false;
        }
        else
        {
            currentRunTime -= staminaRegenRate * Time.deltaTime;
            if (currentRunTime <= 0f)
            {
                currentRunTime = 0f;
                canRun = true;
            }
        }

        walkSpeed = targetSpeed; // feed into FixedUpdate
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        rb.AddForce(CalculateMovement(walkSpeed), ForceMode.VelocityChange);
    }

    Vector3 CalculateMovement(float speed)
    {
        // desired velocity in world space
        Vector3 desiredVel = transform.TransformDirection(new Vector3(input.x, 0, input.y)) * speed;
        Vector3 currentVel = rb.linearVelocity;
        Vector3 delta = desiredVel - new Vector3(currentVel.x, 0f, currentVel.z);

        delta.x = Mathf.Clamp(delta.x, -maxVelocityChange, maxVelocityChange);
        delta.z = Mathf.Clamp(delta.z, -maxVelocityChange, maxVelocityChange);
        delta.y = 0f;

        return delta;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;
using Photon.Pun;
public class PlayerMovements : MonoBehaviour
{

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float swingSpeed;
    public float dashSpeed;
    public float wallRunSpeed;



    public float counterMovement = 0.175f;
    private float threshold = 0.01f;

    public float maxYSpeed;

    public float dashSpeedChangeFactor;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float speedIncreaceMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float variableJump;
    public float fallForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;
    public int amountOfJump = 2;
    public int ammountOfJumpsLeft;


    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybindings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode readySling = KeyCode.F;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;
    public MovementState state;

    public CameraController cc;


    public enum MovementState
    {
        freeze,
        grappling,
        swinging,
        walking,
        sprinting,
        wallrunning,
        crouch,
        sliding,
        dashing,
        air,

    }
    public bool sliding;
    public bool freeze;
    public bool wallrunning;
    public bool activeGrapple;
    public bool swingings;

    public bool dashing;
    private bool enableMovementOnNextTouch;

    private Vector3 velocityToSet;

    private MovementState lastState;
    private bool keepMomentum;

    private float speedChangeFactor;

    public GameObject PlayerCam;
    #region
    float xRotation;
    float yRotation;

    public float sensX;
    public float sensY;


    #endregion
    public GameObject _player;
    public PhotonView PV;
    private void Awake()
    {
        
        if (!PV.IsMine)
        {

            PlayerCam.SetActive(false);
            Destroy(_player);
        }

    }
    private void Start()
    {
       

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cc = GetComponent<CameraController>();
        PV = GetComponent<PhotonView>();
        readyToJump = true;


            startYScale = transform.localScale.y;
        ammountOfJumpsLeft = amountOfJump;
    }
    private void Update()
    {
        if (!PV.IsMine)
        {
            return;
        }
        
        

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);



        MyInput();
        SpeedControl();
        StateHandler();



        if (state == MovementState.walking || state == MovementState.sprinting || state == MovementState.crouch && grounded)
        {
            rb.drag = groundDrag;
            JumpAmountReset();
        }
        else
            rb.drag = 0;

    }
    private void FixedUpdate()
    {
        if (!PV.IsMine)
        {
            return;
        }
        MovePlayer();



    }
    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");





        if (Input.GetKeyDown(jumpKey) && readyToJump && ammountOfJumpsLeft > 0)
        {
            
            readyToJump = false;
                Jump();
            ammountOfJumpsLeft--;
            ResetJump();
                Invoke(nameof(ResetJump), jumpCooldown);
        }
            
            
            
          

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        else if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }


    }
    private void StateHandler()
    {
        if (wallrunning)
        {
            state = MovementState.wallrunning;
            desiredMoveSpeed = wallRunSpeed;
        }
        if (swingings)
        {

            state = MovementState.swinging;
            desiredMoveSpeed = swingSpeed;
        }
        else if (freeze)
        {
            state = MovementState.freeze;
            desiredMoveSpeed = 0;
            rb.velocity = Vector3.zero;
        }
        else if (dashing)
        {
            state = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }
        else if (activeGrapple)
        {
            state = MovementState.grappling;
            desiredMoveSpeed = sprintSpeed;
        }


        else if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;
            else
                desiredMoveSpeed = sprintSpeed;
        }
        else if (Input.GetKey(crouchKey))
        {

            state = MovementState.crouch;
            moveSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.air;

            if (desiredMoveSpeed < sprintSpeed)
            {
                desiredMoveSpeed = walkSpeed;
            }
            else desiredMoveSpeed = sprintSpeed;
        }
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());

        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        if (lastState == MovementState.dashing) keepMomentum = true;

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = desiredMoveSpeed;
            }
        }
        if (!wallrunning) rb.useGravity = !OnSlope();

        lastDesiredMoveSpeed = desiredMoveSpeed;
        lastState = state;
    }

    #region Movement
    private void MovePlayer()
    {
        if (state == MovementState.dashing) return;
        if (activeGrapple) return;
        if (swingings) return;
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        if (!wallrunning || state != MovementState.air) CounterMovement(horizontalInput, verticalInput, mag);


        if (OnSlope() && !exitingSlope)
        {
            Debug.Log("On slope");
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);
            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        else if (grounded)
        {

            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        }


        else if (!grounded)
        {



            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        }






        rb.useGravity = !OnSlope();
    }
    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * 10f * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * 10f * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
    }
    private void SpeedControl()
    {
        if (activeGrapple) return;


        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;


        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }


        if (maxYSpeed != 0 && rb.velocity.y > maxYSpeed)
            rb.velocity = new Vector3(rb.velocity.x, maxYSpeed, rb.velocity.z);
    }
    private void Jump()
    {
        exitingSlope = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);


        

    }
    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;

        }
        return false;
    }
    #endregion
    #region Calculations
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);

        Invoke(nameof(ResetRestrictions), 3f);
    }
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet;


    }
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float starValue = moveSpeed;
        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(starValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);
                time += Time.deltaTime * speedIncreaceMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * boostFactor;


            yield return null;
        }
        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }
    #endregion
    #region resets
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;

        if (grounded && ammountOfJumpsLeft > 0)
            JumpAmountReset();
    }
    public void JumpAmountReset()
    {
        ammountOfJumpsLeft = amountOfJump;
    }
    public void ResetRestrictions()
    {
        activeGrapple = false;

    }
    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            GetComponent<Grappling>().StopGrappleRPC();
        }
    }
    #endregion

}




























/* private void CheckJumpMultiplier()
    {
        if (isJumping)
        {
            Debug.Log("is Jumping");
            if (Input.GetKeyUp(jumpKey))
            {
                Debug.Log("realesed space");
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * variableJump, rb.velocity.z);
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                isJumping = false;
            }
            else if (rb.velocity.y <= 0.01f)
            {
                Debug.Log("is falling");
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                isJumping = false;
            }
        }
    }
 
 
 
 * */
















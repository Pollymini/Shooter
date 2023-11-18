using UnityEngine;
using Photon.Pun;
using System;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.UI;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviourPunCallbacks, IDamagable, IPunObservable
{
    [SerializeField] Image healthbarImage;
    [SerializeField] GameObject ui;


    //Items
    [SerializeField] Item[] items;
    int itemIndex;
    int previousItemIndex = -1;


    //Player Scale
    public float playerHeight;
    public float crouchYScale;
    private float startYScale;

    //KeyCodes
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode swingKey = KeyCode.Mouse0;
    public KeyCode dashKey = KeyCode.E;
    public KeyCode enableSwing = KeyCode.Alpha3;

    //Movement
    private float currentSpeed;
    [SerializeField] float moveSpeed;
    [SerializeField] float sprintSpeed;
    [SerializeField] float crouchSpeed;
    [SerializeField] float slideSpeed;
    [SerializeField] float freezeSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float airMultiplier;
    [SerializeField] float dashSpeed;
    [SerializeField] float wallRunSpeed;
    [SerializeField] float swingSpeed;
    public float groundDrag;

    private float maxYSpeed;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;

    float InputH;
    float InputV;

    Vector3 moveDirection;

    public LayerMask whatIsGround;
    Rigidbody rb;

    private bool grounded;
    private bool sliding;
    private bool readyToJump;


    //Camera Controlls 
    public float sensX;
    public float sensY;

    float xRotation;
    float yRotation;

    public Transform orientation;
    public Transform Cube;
    public Transform camHolder;

    //Timers
    public float slideTimer;
    public float maxSlideTime;
    public float jumpCooldown;

    //Multiplayer
    PhotonView PV;
    PlayerManager playerManager;

    //StateMashine
    public MovementState state;

    //Health
    const float maxHealth = 100f;
    float currentHealth = maxHealth;

    //Climbing
    [Header("Climbing")]
    public float climbSpeed;
    public float maxClimbTime;
    private float climbTimer;

    private bool climbing;

    [Header("Detection")]
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;

    public LayerMask whatIsWall;
    private RaycastHit frontWallHit;
    private bool wallFront;

    //Dashing
    [Header("Dashing")]
    public float dashForce;
    public float dashUpwardForce;
    public float maxDashYSpeed;
    public float dashDuration;

    [Header("CameraEffets")]
    public Transform cam;
    public float dashFov;

    [Header("Settings")]
    public bool useCameraForward = true;
    public bool allowAllDirections = true;
    public bool disableGravity = false;
    public bool resetVel = false;

    [Header("Cooldown")]
    public float dashCd;
    private float dashCdTimer;

  
    

    public bool dashing;
    // WallRun
    [Header("Exiting")]
    private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;


    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Inpur")]
    private float horizontalInput;
    private float verticalInput;


    [Header("WallRunning")]
   
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float wallRunTimer;
    public float maxWallRunTime;
    private float Timer;

    bool wallrunning;

    [Header("Gravity")]
    private bool useGravity;
    public float gravityCounter;

    // Swing
    [Header("References")]
    public LineRenderer lr;
    public Transform gunTip, player;
    public LayerMask whatIsGrappable;
    
    

    [Header("Prediction")]
    public RaycastHit predictionHit;
    public float predictionSCR;
    public Transform predictionPoint;

    [Header("Swinging")]
    public float maxSwingDist = 80f;

    private Vector3 swingPoint;
    private SpringJoint joint;


    private bool canSwing;



    [Header("OdmGear")]
 
    public float horizontalTrustForce;
    public float forwardTrustForce;
    public float extendCableSpeed;

    private bool swingings;
    public enum MovementState
    {
        freeze,
        grappling,
        swinging,
        walking,
        sprinting,
        wallrunning,
        crouching,
        sliding,
        dashing,
        air,
    }
   






    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();
        canSwing = false;
        readyToJump = true;

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
    }
    void Start()
    {
        if (PV.IsMine)
        {
            EquipItem(0);
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
            return;
        }

        sliding = false;
        startYScale = transform.localScale.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void Update()
    {
        if (!PV.IsMine)
            return;

        WallCheck();
        StateMashine();

        StateHandler();
        Inputs();
        LookAround();
        MovementController();
        Crouching();
        CheckForWall();

        if (wallrunning)
        {
            
            WallRunMovement();
        }

        if (Input.GetKey(enableSwing)&& !canSwing)
        {
            canSwing = true;
        }
        else if(Input.GetKey(enableSwing) && canSwing)
        {
            canSwing = false;
        }

        if (Input.GetKeyDown(swingKey) && canSwing) PV.RPC("RPC_StartSwing", RpcTarget.All);
        if (Input.GetKeyUp(swingKey)) PV.RPC("RPC_StopSwing", RpcTarget.All); 
        PV.RPC("RPC_CheckForSwingPoints", RpcTarget.All);
        if (joint != null) OdmGearMovement();

        if (Input.GetKeyDown(dashKey)) { Dash(); }
        if (dashCdTimer > 0)
            dashCdTimer -= Time.deltaTime;

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        if (state == MovementState.walking || state == MovementState.sprinting || state == MovementState.crouching && grounded)
        {
            rb.drag = groundDrag;
        }
        else
            rb.drag = 0;
        


        if (sliding)
        {
            SlidingMovement();
        }
        for(int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if(Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            if(itemIndex >= items.Length - 1)
            {
                EquipItem(0);
            }
            else
            {

              EquipItem(itemIndex + 1);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            if (itemIndex <= 0)
            {
                EquipItem(items.Length - 1);
            }
            else
            {
                 EquipItem(itemIndex - 1);
            }

        }
        if(Input.GetMouseButtonDown(0) && !canSwing)
        {
            items[itemIndex].Use(); 
        }
        if(transform.position.y < -255f)
        {
            Die();
        }


        

        if (climbing) ClimbingMovement();
    }

    #region Swing
    private void LateUpdate()
    {
        PV.RPC("RPC_DrawRope", RpcTarget.All);
    }
   public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    { /*
        PV.RPC("RPC_DrawRope", RpcTarget.All);
        if (stream.IsWriting)
        {
           stream.SendNext(gunTip.transform.position);
           stream.SendNext(swingPoint);
           
        }
        if (stream.IsReading)
        {
            gunTip.transform.position = (Vector3)stream.ReceiveNext();
            swingPoint = (Vector3)stream.ReceiveNext();
           
        }*/
    }
        [PunRPC]
    private void RPC_StartSwing()
    {

        if (predictionHit.point == Vector3.zero) return;
        Debug.Log("Start Swing");

        swingings = true;




        swingPoint = predictionHit.point;
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;

        lr.positionCount = 2;
        currentGrappleposition = gunTip.position;

    }
    [PunRPC]
    public void RPC_StopSwing()
    {
        swingings = false;

        lr.positionCount = 0;
        Destroy(joint);
    }
    private Vector3 currentGrappleposition;

    [PunRPC]
    public void RPC_DrawRope()
    {
        if (!joint) return;
        
        currentGrappleposition = Vector3.Lerp(currentGrappleposition, swingPoint, Time.deltaTime * 8f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, swingPoint);
    }
    private void OdmGearMovement()
    {
        if (Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * horizontalTrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * horizontalTrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.W)) rb.AddForce(orientation.forward * forwardTrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardTrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, swingPoint) + extendCableSpeed;

            joint.maxDistance = extendedDistanceFromPoint * 0.8f;
            joint.minDistance = extendedDistanceFromPoint * 0.25f;
        }

    }
    [PunRPC]
    private void RPC_CheckForSwingPoints()
    {
        if (joint != null ) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSCR, cam.forward, out sphereCastHit, maxSwingDist, whatIsGrappable);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward, out raycastHit, maxSwingDist, whatIsGrappable);

        Vector3 realHitPoint;

        if (raycastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;
        else if (sphereCastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;
        else realHitPoint = Vector3.zero;

        if (realHitPoint != Vector3.zero)
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = realHitPoint;
        }
        else if(!canSwing)
        {
            predictionPoint.gameObject.SetActive(false);
        }
        else
        {
            predictionPoint.gameObject.SetActive(false);
        }


        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;

    }
    public void ResetRestrictions()
    {
        //activeGrapple = false;

    }
    #endregion
    #region WallRun
    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
        if(wallRight == null)
            Debug.Log("wallRight null");
        if (wallLeft == null)
            Debug.Log("wallLeft null");
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StartWallRun()
    {

        wallrunning = true;
        wallRunTimer = maxWallRunTime;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
       

    }
    private void WallRunMovement()
    {

        rb.useGravity = useGravity;



        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallforward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallforward).magnitude > (orientation.forward - -wallforward).magnitude)
            wallforward = -wallforward;


        rb.AddForce(wallforward * wallRunForce, ForceMode.Force);
        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput > 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);

        if (useGravity)
            rb.AddForce(transform.up * gravityCounter, ForceMode.Force);
    }

    private void StopWallRun()
    {
        wallrunning = false;
       
    }
    private void WallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
    #endregion
    #region WallClimb
    private void StateMashine()
    {
        if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle)
        {

            if (!climbing && climbTimer > 0) StartClimbing();


            if (climbTimer > 0) climbTimer -= Time.deltaTime;
            if (climbTimer < 0) StopClimbing();

        }
        else 
        {
            if (climbing) StopClimbing();
        }

        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            StartWallRun();
            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if (wallRunTimer <= 0 && wallrunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKeyDown(jumpKey))
                WallJump();
        }
        else if (exitingWall)
        {
            if (wallrunning)
                StopWallRun();
            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer < 0)
                exitingWall = false;
        }

        else
        {
            StopWallRun();
        }
    }
    private void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        if (grounded)
        {
            climbTimer = maxClimbTime;
        }
    }
    private void StartClimbing()
    {
        climbing = true;
    }
    private void ClimbingMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
    }
    private void StopClimbing()
    {
        climbing = false;
    }
    #endregion
    #region Dashing
    private void Dash()
    {
        if (dashCdTimer > 0) return;
        else dashCdTimer = dashCd;

        dashing = true;
        maxYSpeed = maxDashYSpeed;

       

        Transform forwardT;

        if (useCameraForward)
            forwardT = camHolder;
        else
            forwardT = orientation;

        Vector3 direction = GetDirection(forwardT);


        Vector3 forceToApply = direction * dashForce + orientation.up * dashUpwardForce;

        if (disableGravity)
            rb.useGravity = false;

        delayedForceToApply = forceToApply;

        rb.AddForce(forceToApply, ForceMode.Impulse);
        Invoke(nameof(DelayedDashForce), 0.025f);

        Invoke(nameof(ResetDash), dashDuration);
    }
    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        if (resetVel)
            rb.velocity = Vector3.zero;
        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }
    private void ResetDash()
    {
        dashing = false;
        maxYSpeed = 0;

        

        if (disableGravity)
            rb.useGravity = true;

    }
    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticallInput = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3();

        if (allowAllDirections)
            direction = forwardT.forward * verticallInput + forwardT.right * horizontalInput;
        else
            direction = forwardT.forward;

        if (verticallInput == 0 && horizontalInput == 0)
            direction = forwardT.forward;

        return direction.normalized;
    }
    #endregion
    #region  StateHandler


    private void StateHandler()
    {
        if (swingings)
        {

            state = MovementState.swinging;
            currentSpeed = swingSpeed;
        }
        else if (wallrunning)
        {
            state = MovementState.wallrunning;
            currentSpeed = wallRunSpeed;
        }

        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            currentSpeed = sprintSpeed;
        }
        else if (dashing)
        {
            state = MovementState.dashing;
            currentSpeed = dashSpeed;
            
        }
        else if (grounded && Input.GetKeyDown(crouchKey) && !Speed())
        {
            state = MovementState.crouching;
            currentSpeed = crouchSpeed;
            Crouching();
        }
        else if (Input.GetKeyDown(crouchKey) && (InputH != 0 || InputV != 0))
        {
            state = MovementState.sliding;
            StartSlide();
        }
        else if (grounded)
        {
            state = MovementState.walking;
            currentSpeed = moveSpeed;
        }
        else if(!grounded)
        {
            state = MovementState.air;
        }
    }
    #endregion
    #region Inputs
    private void Inputs()
    {
        
        //Movement Inputs
        InputH = Input.GetAxisRaw("Horizontal");
        InputV = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey) && readyToJump)
        {
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    #endregion
    #region Camera
    private void LookAround()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -75f, 75f);

        camHolder.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        Cube.transform.rotation = Quaternion.Euler(0, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
    #endregion
    #region Movement
    
    
    private void MovementController()
    {
            Vector2 mag = FindVelRelativeToLook();
            float xMag = mag.x, yMag = mag.y;

            moveDirection = orientation.right * InputH + orientation.forward * InputV;

            if (grounded)
            {
                rb.AddForce(moveDirection.normalized * currentSpeed * 10f, ForceMode.Force);
               
            }
            else if (!grounded)
            {
                rb.AddForce(moveDirection.normalized * currentSpeed * 10f * airMultiplier, ForceMode.Force);
               
             }
        CounterMovement(InputH, InputV, mag);
    }







            
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
    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (MathF.Abs(mag.x) > threshold && MathF.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) 
        {
            rb.AddForce(moveSpeed * 10f * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (MathF.Abs(mag.y) > threshold && MathF.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * 10f * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
    }
    public bool Speed()
    {
        if (InputH != 0 || InputV != 0) return true;
        else return false;
    }
    #endregion
    #region  Sliding/Crouching
    private void Crouching()
    {
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
    private void StartSlide()
    {
        sliding = true;
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        slideTimer = maxSlideTime;
        Debug.Log("S");
    }
    private void SlidingMovement()
    {
        Debug.Log("SM");
            rb.AddForce(moveDirection.normalized * slideSpeed, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        
        if (slideTimer <= 0 || Input.GetKeyUp(crouchKey))
        {

            StopSlide();
        }

    }
    private void StopSlide()
    {
        sliding = false;
        if (Input.GetKey(crouchKey))
        state = MovementState.crouching;
        else
        state = MovementState.walking;
    }
    #endregion
    #region Jump
    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
    #endregion
    #region Inventory
    void EquipItem(int _index)
    {
        if(_index == previousItemIndex)
        {
            return;
        }
        itemIndex = _index;
        items[itemIndex].itemGameObject.SetActive(true);
        if(previousItemIndex != -1) 
        {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }
        previousItemIndex = itemIndex;

        if (PV.IsMine)
        {
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if(!PV.IsMine && targetPlayer == PV.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }
    }
    #endregion
    #region Damage
    public void TakeDamage(float damage)
    {
        PV.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage) 
    {
        if (!PV.IsMine)
        {
            return;
        }
        currentHealth -= damage;
        healthbarImage.fillAmount = currentHealth / maxHealth;
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        playerManager.Die();
    }

   
    #endregion
}


















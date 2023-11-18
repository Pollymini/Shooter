using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Slidings : MonoBehaviour
{
    [Header("References")]
    public Transform orintation;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovements pm;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    private float slideTimer;

    public float slideYscale;
    private float startYscale;

    [Header("Input")]
    public KeyCode slidekey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;

   

    private void Start()
    {
        rb = GetComponent<Rigidbody>(); 
        pm = GetComponent<PlayerMovements>();

        startYscale = playerObj.localScale.y;
    }
    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(slidekey) && (horizontalInput !=0 || verticalInput !=0))
        {
            StartSlide();
        }
        if(Input.GetKeyUp(slidekey) && pm.sliding)
        {
            StopSlide();
        }
    }

    private void FixedUpdate()
    {
        if(pm.sliding)
        {
            SlidingMovement();
        }
    }
    private void StartSlide()
    {
        pm.sliding = true;
        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYscale, playerObj.localScale.z);

        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        slideTimer = maxSlideTime;
    }
    private void SlidingMovement()
    {
        Vector3 inputDirection = orintation.forward * verticalInput + orintation.right * horizontalInput;

        if(!pm.OnSlope() || rb.velocity.y > 0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        }
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }


        if (slideTimer<= 0 ) 
        {

            StopSlide();
        }

    }
    private void StopSlide()
    {
        pm.sliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYscale, playerObj.localScale.z);
    }
}

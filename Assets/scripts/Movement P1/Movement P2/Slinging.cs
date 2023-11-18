using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Photon.Pun;

public class Slinging : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lr;
    public Transform gunTip, cam, player;
    public LayerMask whatIsGrappable;
    public PlayerMovements pm;
    private Grappling gr;

    [Header("Prediction")]
    public RaycastHit predictionHit;
    public float predictionSCR;
    public Transform predictionPoint;

    [Header("Swinging")]
    public float maxSwingDist = 80f;

    private Vector3 swingPoint;
    private SpringJoint joint;

    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse0;


    public PhotonView PV;

    [Header("OdmGear")]
    public Transform orientation;
    public Rigidbody rb;
    public float horizontalTrustForce;
    public float forwardTrustForce;
    public float extendCableSpeed;

   ///public PauseMenuGame PMG;

    private void Update()
    {
         if (!PV.IsMine) { return; }
        /// if (PMG.Paused == false)
        /// {

        /// }
        /// else return;
        /// 
        if (Input.GetKeyDown(swingKey)) StartSwing();
        if (Input.GetKeyUp(swingKey)) StopSwing();
        CheckForSwingPoints();
        if (joint != null) OdmGearMovement();
    }
    private void LateUpdate()
    {
        DrawRope();
    }
    private void StartSwing()
    {

        if (predictionHit.point == Vector3.zero) return;

        if (GetComponent<Grappling>() != null)
            GetComponent<Grappling>().StopGrappleRPC();
        pm.ResetRestrictions();
        pm.swingings = true;

         

      
       
        
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
    public void StopSwing()
    {
      pm.swingings = false;

        lr.positionCount = 0;
        Destroy(joint);
    }
    private Vector3 currentGrappleposition;
    void DrawRope()
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
    private void CheckForSwingPoints()
    {
        if (joint != null) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSCR, cam.forward, out sphereCastHit, maxSwingDist, whatIsGrappable);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward, out raycastHit, maxSwingDist, whatIsGrappable);

        Vector3 realHitPoint;

        if(raycastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;
        else if (sphereCastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;
        else realHitPoint = Vector3.zero;

        if(realHitPoint != Vector3.zero)
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = realHitPoint;
        }
        else
        {
            predictionPoint.gameObject.SetActive(false);
        }

        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;

    }
        
        




}

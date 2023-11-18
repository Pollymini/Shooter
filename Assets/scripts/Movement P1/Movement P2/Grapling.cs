using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Grappling : MonoBehaviour
{
   ///public PauseMenuGame PMG;
    [Header("References")]
    private PlayerMovements pm;
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;

    private Slinging sl;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    private Vector3 grapplePoint;

    [Header("Cooldown")]
    public float grapplingCd;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;


    public PhotonView PV;

    private bool grappling;

    private void Start()
    {
        pm = GetComponent<PlayerMovements>();
    }

    private void Update()
    {
        if (!PV.IsMine) { return; }
        if (Input.GetKey(grappleKey)) StartGrappleRPC();
       


        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (grappling)
           lr.SetPosition(0, gunTip.position);
    }
    [PunRPC]
    private void StartGrappleRPC()
    {
        if (grapplingCdTimer > 0) return;

        if (GetComponent<Slinging>() != null)
            GetComponent<Slinging>().StopSwing();
       

        grappling = true;

        pm.freeze = true;

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            Invoke(nameof(ExecuteGrappleRPC), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;

            Invoke(nameof(StopGrappleRPC), grappleDelayTime);
        }

        lr.enabled = true;
        lr.SetPosition(1, grapplePoint);
    }
    [PunRPC]
    private void ExecuteGrappleRPC()
    {
        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrappleRPC), 1f);
    }
    [PunRPC]
    public void StopGrappleRPC()
    {
        pm.freeze = false;

        grappling = false;

        grapplingCdTimer = grapplingCd;

        lr.enabled = false;
    }

    public bool IsGrappling()
    {
        return grappling;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
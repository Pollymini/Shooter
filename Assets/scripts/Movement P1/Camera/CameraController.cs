using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Photon.Pun;

public class CameraController : MonoBehaviour
{
    public float sensX;
    public float sensY;


    public PhotonView PV;
    /// private PauseMenuGame PMG;


    public Transform orientation;
    public Transform camHolder;
     public Camera cam;
    float xRotation;
    float yRotation;
    

    private void Start()
    {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
    }

        
        /// if(PMG.Paused == true) return;



        
        
    private void Update()
    {
        
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camHolder.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);   
    }
   

    public void DoFov(float endValue)
    {
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }
    public void DoTilt(float zTilt)
    {
        transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
    }
    
}

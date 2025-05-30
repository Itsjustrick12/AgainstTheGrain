using UnityEngine;
//code referenced from: https://www.youtube.com/watch?v=R6scxu1BHhs&t=56s&ab_channel=ShackMan
public class CameraController : MonoBehaviour
{
    public Camera cam;
    private Vector3 dragOrigin;
    private Vector3 targetPos;

    //needed to prevent camera from jerking around
    public float smoothing = 0.125f;

    public void Update()
    {
        MoveCamera();   
    }

    public void MoveCamera()
    {
        //move camera based on diffrences

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //origin for the drag
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);

        }
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 diff = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            targetPos = cam.transform.position + diff;
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, smoothing * Time.deltaTime);
        }
    }
}

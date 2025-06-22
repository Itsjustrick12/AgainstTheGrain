using UnityEngine;
//code referenced from: https://www.youtube.com/watch?v=R6scxu1BHhs&t=56s&ab_channel=ShackMan
public class CameraController : MonoBehaviour
{
    public Camera cam;
    private Vector3 dragOrigin;
    private Vector3 targetPos;

    public int minX = -1;
    public int maxX = 1;
    public int minY = -1;
    public int maxY = 1;

    //needed to prevent camera from jerking around
    public float smoothing = 0.125f;

    //needed for determining whether or not the camera can be moved due to UI freezing
    CursorToTilemap inputManager;
    private void Awake()
    {
        inputManager = FindAnyObjectByType<CursorToTilemap>();
    }

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
        if (Input.GetKey(KeyCode.Space) && !inputManager.makingDecision)
        {
            Vector3 diff = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            targetPos = cam.transform.position + diff;
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, smoothing * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        //updown
        Gizmos.DrawLine(new Vector3(minX, minY), new Vector3(minX, maxY));
        Gizmos.DrawLine(new Vector3(maxX, minY), new Vector3(maxX, maxY));
        //left right
        Gizmos.DrawLine(new Vector3(minX, minY), new Vector3(maxX, minY));
        Gizmos.DrawLine(new Vector3(minX, maxY), new Vector3(maxX, maxY));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    new Camera camera;

    private Vector3? lastMousePos;

    public float zoomSpeed = 0.1f;

    public float minSize = 0.1f;
    public float maxSize = 10f;

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        // Zoom
        if(Input.mouseScrollDelta.y != 0)
        {
            var screenPos = Input.mousePosition;
            var worldPos = camera.ScreenToWorldPoint(screenPos);
            var zoomFactor = Mathf.Exp(-zoomSpeed * Input.mouseScrollDelta.y);
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize * zoomFactor, minSize, maxSize);
            // Translate camera so mouse cursor remains fixed point
            var worldPos2 = camera.ScreenToWorldPoint(screenPos);
            camera.transform.position -= worldPos2 - worldPos;
        }
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
            var r = currentMagnitude / prevMagnitude; ;
            if (float.IsFinite(r))
            {
                camera.orthographicSize *= r;
            }
        }

        // Pan
        if (Input.GetMouseButton(2))
        {
            var worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
            if (lastMousePos != null)
            {
                camera.transform.position -= worldPos - lastMousePos.Value;
            }

            lastMousePos = camera.ScreenToWorldPoint(Input.mousePosition);
        }
        else
        {
            lastMousePos = null;
        }
    }
}

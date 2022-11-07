using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public float sensitivity = 1;

    public float zoomSpeed = 1;

    public float minZoom = 0.1f;
    public float maxZoom = 10f;
    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            var x = Input.GetAxis("Mouse X") * sensitivity;
            var y = Input.GetAxis("Mouse Y") * sensitivity;

            var r = transform.localRotation;
            r = r * Quaternion.AngleAxis(x, Vector3.up);
            r = r * Quaternion.AngleAxis(y, Vector3.left);
            transform.localRotation = r;
        }

        var scale = transform.localScale.x;
        scale *= Mathf.Exp(-Input.mouseScrollDelta.y * zoomSpeed);
        scale = Mathf.Clamp(scale, minZoom, maxZoom);
        transform.localScale = new Vector3(scale, scale, scale);
    }
}

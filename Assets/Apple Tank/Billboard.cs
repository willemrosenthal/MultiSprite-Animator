using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour {

    Vector3 relativeCamPos;
    Vector2 offset;
    Vector2 WorldUnitsInCamera;
    Vector2 WorldToPixelAmount;

    public bool cameraInParent = true;

    void Start() {
        //Finding Pixel To World Unit Conversion Based On Orthographic Size Of Camera
        WorldUnitsInCamera.y = Camera.main.orthographicSize * 2;
        WorldUnitsInCamera.x = WorldUnitsInCamera.y * Screen.width / Screen.height;

        WorldToPixelAmount.x = Screen.width / WorldUnitsInCamera.x;
        WorldToPixelAmount.y = Screen.height / WorldUnitsInCamera.y;

        offset = new Vector2();
    }

    void Update() {
        relativeCamPos = Camera.main.transform.position;

        // get position relative to camera on screen.
        relativeCamPos += (Vector3)RelativeCamOffset();

        transform.LookAt(2 * transform.position - relativeCamPos, Vector3.back);
    }

    Vector2 RelativeCamOffset() {
        offset.x = (Camera.main.WorldToScreenPoint(transform.position).x / WorldToPixelAmount.x) - WorldUnitsInCamera.x * 0.5f;
        offset.y = (Camera.main.WorldToScreenPoint(transform.position).y / WorldToPixelAmount.y) - WorldUnitsInCamera.y * 0.5f;
        
        if (Camera.main.transform.parent)
            offset = Quaternion.AngleAxis(Camera.main.transform.parent.eulerAngles.z, Vector3.forward) * offset;

        return offset * 1;
    }
}
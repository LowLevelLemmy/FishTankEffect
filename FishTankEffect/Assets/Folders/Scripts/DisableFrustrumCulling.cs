using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableFrustrumCulling : MonoBehaviour
{
    Camera cam;

    void OnEnable()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null)
            return;
        // boundsTarget is the center of the camera's frustum, in world coordinates:
        Vector3 camPosition = cam.transform.position;
        Vector3 normCamForward = Vector3.Normalize(cam.transform.forward);
        float boundsDistance = (cam.farClipPlane - cam.nearClipPlane) / 2 + cam.nearClipPlane;
        Vector3 boundsTarget = camPosition + (normCamForward * boundsDistance);

        // The game object's transform will be applied to the mesh's bounds for frustum culling checking.
        // We need to "undo" this transform by making the boundsTarget relative to the game object's transform:
        Vector3 realtiveBoundsTarget = this.transform.InverseTransformPoint(boundsTarget);

        // Set the bounds of the mesh to be a 1x1x1 cube (actually doesn't matter what the size is)
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.bounds = new Bounds(realtiveBoundsTarget, Vector3.one);

    }

    void OnPreCull()
    {
        cam.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                            Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                            cam.worldToCameraMatrix;
    }

    void OnDisable()
    {
        cam.ResetCullingMatrix();
    }
}
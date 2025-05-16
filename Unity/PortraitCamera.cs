using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PortraitCameraController : MonoBehaviour
{
    public RenderTexture targetTexture;

    void Start()
    {
        var cam = GetComponent<Camera>();
        if (targetTexture != null)
            cam.targetTexture = targetTexture;
    }
}
using UnityEngine;

public class PhysicsRotator : MonoBehaviour
{
    private RectTransform rectTransform;
    public float rotateSpeed = 2.5f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (rectTransform == null) return;

        Vector3 currentEuler = rectTransform.localEulerAngles;
        rectTransform.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y + rotateSpeed, currentEuler.z);
    }
}

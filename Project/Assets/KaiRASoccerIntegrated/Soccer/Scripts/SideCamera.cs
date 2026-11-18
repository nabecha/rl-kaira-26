using UnityEngine;

public class SideCamera : MonoBehaviour
{
    public Transform fieldCenter;

    public Vector3 offset = new Vector3(0f, 15f, -22f);

    void LateUpdate()
    {
        if (fieldCenter == null) return;

        transform.position = fieldCenter.position + offset;
        transform.LookAt(fieldCenter);
    }
}

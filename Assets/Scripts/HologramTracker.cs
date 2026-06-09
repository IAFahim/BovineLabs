using UnityEngine;

[ExecuteAlways]
public class HologramTracker : MonoBehaviour
{
    void LateUpdate()
    {
        // Camera.current gracefully handles both the Scene View camera in Editor Mode
        // and the active Game View camera in Play Mode.
        var cam = Camera.current;
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
    }
}

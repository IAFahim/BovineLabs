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
        {
            // By copying the camera's rotation exactly, the text's negative Z-axis 
            // (the front of the TMPro) will directly face the camera, preventing flipping.
            transform.rotation = cam.transform.rotation;
        }
    }
}

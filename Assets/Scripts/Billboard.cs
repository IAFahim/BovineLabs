using UnityEngine;

[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    private void LateUpdate()
    {
        var cam = Camera.current;
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }
    }
}

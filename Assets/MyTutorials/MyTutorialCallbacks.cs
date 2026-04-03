using UnityEngine;
using Unity.Tutorials.Core.Editor;

[CreateAssetMenu(fileName = "MyTutorialCallbacks", menuName = "Tutorials/MyTutorialCallbacks Instance")]
public class MyTutorialCallbacks : ScriptableObject
{
    public bool DoesCubeExist()
    {
        return GameObject.Find("Cube") != null;
    }
}
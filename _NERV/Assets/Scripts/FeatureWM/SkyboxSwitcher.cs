using UnityEngine;

public class SkyboxSwitcher : MonoBehaviour
{
    [Header("Assign Your Skybox Materials")]
    public Material Skybox1;
    public Material Skybox2;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && Skybox1 != null)
        {
            RenderSettings.skybox = Skybox1;
            Debug.Log("Skybox switched to 1");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && Skybox2 != null)
        {
            RenderSettings.skybox = Skybox2;
            Debug.Log("Skybox switched to 2");
        }
    }
}

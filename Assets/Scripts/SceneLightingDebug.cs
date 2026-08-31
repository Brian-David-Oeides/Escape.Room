#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// Editor/development-build-only convenience to force the atmospheric
// Directional Light on or off, overriding its scene-authored state.
// Compiled out entirely in release builds - see class guard above.
public class SceneLightingDebug : MonoBehaviour
{
    [SerializeField] private bool forceDirectionalLightOn = false;
    [SerializeField] private Light directionalLight;

    void Awake()
    {
        if (directionalLight == null)
        {
            directionalLight = GetComponent<Light>();
        }
    }

    void Update()
    {
        if (directionalLight != null)
        {
            directionalLight.enabled = forceDirectionalLightOn;
        }
    }
}
#endif

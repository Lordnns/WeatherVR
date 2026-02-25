using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VRSeamlessLoader : MonoBehaviour
{
    public string sceneToLoad = "MainWorld";
    public GameObject loadingSceneCamera;
    
    public static bool IsMainSceneReady = false;

    private IEnumerator Start()
    {
        IsMainSceneReady = false;
        
        // 2. Load the main scene ADDITIVELY
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        // 5. Wait for the WeatherManager in the NEW scene to finish its Start()
        while (!IsMainSceneReady) yield return null;

        // 6. SWAP: Disable loading camera so the MainWorld camera takes over
        if (loadingSceneCamera != null)
            loadingSceneCamera.SetActive(false);

        // Clean up the loading scene
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
}
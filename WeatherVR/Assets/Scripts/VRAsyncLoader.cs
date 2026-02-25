using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VRAsyncLoader : MonoBehaviour
{
    public string sceneToLoad = "MainWorld";
    public static bool SceneSetupComplete = false; // The flag for the next scene

    private IEnumerator Start()
    {
        // 2. Start loading the main world
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        // 3. Wait for Unity to finish loading the heavy assets (90%)
        while (op.progress < 0.9f)
            yield return null;

        // 4. Wait for the new scene's WeatherManager to flip the flag
        // (This ensures SnowSystem.cs has finished its Start() loop)
        while (!SceneSetupComplete)
            yield return null;

        // 5. Everything is ready, open the world!
        op.allowSceneActivation = true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events; 

public class SceneManagerScript : MonoBehaviour
{
    public static SceneManagerScript Instance;

    private string sceneIdentifier; 
   
    public void SceneInvoke(string sceneName) // invoke scene without delay
    {
        sceneIdentifier = sceneName;
        Invoke(nameof(GoToScene), 0f);
    }

    public void DelayedSceneInvoke(string sceneName)//invoke scene with delay
    {
        sceneIdentifier = sceneName;
        Invoke(nameof(GoToScene), 0.1f);
    }
    public void GoToScene() {
        SceneManager.LoadScene(sceneIdentifier); 
    }
    private void Awake()
    {
        StartSingleton();
    }
    private void StartSingleton()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
 
  
}

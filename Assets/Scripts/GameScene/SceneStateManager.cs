using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using DG.Tweening;

public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance;

    [SerializeField]
    private AudioSource[] audioSources = null;

    [SerializeField]
    private GameObject[] instructionObjects = null;

    [SerializeField]
    private GameObject[] countdownObjects = null;

    [SerializeField]
    private GameObject[] gameplayObjects = null;

    [SerializeField]
    private GameObject pauseButton = null;

    [SerializeField]
    private GameObject overlay = null;

    [SerializeField]
    private AudioMixer audioMixer = null;

    private GameObject comboIndicator;
    private GameObject titleButton;
    private GameObject countButton;
    private TMPro.TextMeshProUGUI title;
    private TMPro.TextMeshProUGUI countdown;

    SceneState sceneState = SceneState.Countdown;

    float delay = 1;
    bool setPause = false;

    void Start()
    {
        Instance = this;
        Initialize();
        CheckSceneState();
    }

    void Update()
    {
        SetPause();
    }

    void Initialize()
    {
        // Get component named Combo, we need it to hide/show the object based on the screen state
        comboIndicator = gameplayObjects[0].transform.Find("Combo").gameObject;

        // Initialize title & countdown object
        // Title is static, countdown is dynamic
        titleButton = countdownObjects[1];
        countButton = countdownObjects[0];
        title = countdownObjects[1].GetComponentInChildren<TMPro.TextMeshProUGUI>();
        countdown = countdownObjects[0].GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    public SceneState GetSceneState()
    {
        return sceneState;
    }

    public void ChangeSceneState(SceneState scene)
    {
        sceneState = scene;
        CheckSceneState();
    }

    void CheckSceneState()
    {
        // Loop through the animated object list, and inactive them
        // Later, the inactived objects will show up based on the current scene state
        var array = instructionObjects.Concat(countdownObjects).Concat(gameplayObjects).ToArray();
        SetInactives(array);

        switch (sceneState)
        {
            case SceneState.Instruction:
                InstructionStart();
                break;
            case SceneState.Countdown:
                StartCoroutine(CountdownStart());
                break;
        }
    }

    // Set all objects to inactive.
    // This is needed, to reset all objects on the scene before transition to the next scene state begin.
    void SetInactives(GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            obj.SetActive(false);
        }
    }

    void SetPause()
    {
        if (Lane.Instance != null)
            setPause = Lane.Instance.SetPause();

        pauseButton.SetActive(setPause);
    }

    // At the moment, we use dspTime to control spawned notes and notes position.
    // Hence, all we need to do to pause/resume the game is just pausing/resuming the song.
    // Since we use audio playback reference to spawn our game object.
    public void PauseGame()
    {
        sceneState = SceneState.Pause;
        overlay.SetActive(true);

        foreach (var audio in audioSources)
        {
            audio.Pause();
        }
    }

    public void ResumeGame()
    {
        sceneState = SceneState.Countdown;
        overlay.SetActive(false);
        var pauseDelay = 3;
        StartCoroutine(FadeMixerGroup.StartFade(audioMixer, "backsoundVolume", pauseDelay, FadeMixerGroup.Fade.In));

        foreach (var audio in audioSources)
        {
            var currentTime = audio.time;
            audio.time = currentTime - pauseDelay;
            audio.PlayScheduled(0);
        }
    }

    public void SetAudioTime(float time)
    {
        foreach (var audio in audioSources)
        {
            audio.time = time;
        }
    }

    void InstructionStart()
    {
        comboIndicator.SetActive(false);
        StartCoroutine(AnimateObjects(instructionObjects, 0.1f, AnimationType.MoveY));
    }

    // Countdown to Gameplay transition.
    // The code is pretty self explanatory since it's a hardcoded & sequential one.
    // Basically telling the sequence of animation that need to be played in transition.
    IEnumerator CountdownStart()
    {
        StartCoroutine(AnimateObjects(countdownObjects, 0.1f, AnimationType.MoveY));

        int count = 3;
        while (count > 0)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(AnimateObjects(countdownObjects, 0.2f, AnimationType.PunchScale));
            countdown.SetText(count.ToString());
            count --;
        }

        yield return new WaitForSeconds(delay);

        titleButton.SetActive(false);
        PunchScale(countButton);
        countdown.SetText("Go!");

        yield return new WaitForSeconds(delay);

        StartCoroutine(AnimateObjects(gameplayObjects, 0.1f, AnimationType.MoveY));
        comboIndicator.SetActive(true);
        countButton.SetActive(false);
        countdown.SetText("3");

        yield return new WaitForSeconds(2);

        SongManager.Instance.StartSong();
    }

    // Animate group of objects based on the given parameter (duration & animationType)
    IEnumerator AnimateObjects(GameObject[] objects, float duration, AnimationType type)
    {
        foreach(var obj in objects)
        {
            // Set parent to active if it's inactive
            if (obj.transform.parent != null && !obj.transform.parent.gameObject.activeSelf)
            {
                var parentObj = obj.transform.parent.gameObject;
                parentObj.SetActive(true);
            }

            obj.SetActive(true);

            switch (type) {
                case AnimationType.MoveY:
                    MoveY(obj);
                    break;
                case AnimationType.PunchScale:
                    PunchScale(obj);
                    break;
            }
            yield return new WaitForSeconds(duration);
        }
    }

    // TODO - Move this to animation utilities
    // DoTween Animation
    void PunchScale(GameObject obj)
    {
        obj.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0.25f), 0.2f, 1, 1);
    }

    void MoveY(GameObject obj)
    {
        var post = obj.transform.position;
        obj.transform.DOMoveY(post.y, 0.75f).SetEase(Ease.InOutQuad).From(post.y + 5f);
    }

    // Enumeration
    enum AnimationType
    {
        PunchScale,
        MoveY
    }

    public enum SceneState
    {
        Instruction,
        Countdown,
        Pause,
    }
}


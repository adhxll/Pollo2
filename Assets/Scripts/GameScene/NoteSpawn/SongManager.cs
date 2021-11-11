using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance;
    public PitchDetector detectedPitch;

    [SerializeField]
    TextAsset midiJSON = null;

    [SerializeField]
    private Lane lanes = null;

    [SerializeField]
    private BarTimeline barTimeline = null;

    private AudioSource audioSource;
    public static MIDI.MidiFile midiFile;

    public float songDelayInSeconds;
    public double marginOfError;    // In seconds
    public int inputDelayInMilliseconds;

    public float noteTime;  // Time needed for the note spawn location to the tap location
    public float noteSpawnX;    // Note spawn position in world space
    public float noteTapX;  // Note tap position in world space

    public float noteDespawnX
    {
        get
        {
            return noteTapX - (noteSpawnX - noteTapX);
        }
    }

    static float midiBPM
    {
        get
        {
            return (float)60 / (float)midiFile.header.tempos[0].bpm;
        }
    }

    // Position Tracking
    double dspTimeSong;
    public bool songPlayed = false;
    bool endOfSong = false;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        midiFile = MIDI.CreateFromJSON(midiJSON.text);
        GetDataFromMidi();
    }

    void GetDataFromMidi()
    {
        var tracks = midiFile.tracks;
        int i = 0;
        foreach(var track in tracks)
        {
            var notes = track.notes;

            if (i == 0)
                lanes.SetTimeStamps(notes);

            if (i == 1)
                barTimeline.SetTimeStamps(notes);

            i++;
        }

        barTimeline.PlaceTimestamp();

    }

    public void StartSong()
    {
        songPlayed = true;
        dspTimeSong = AudioSettings.dspTime;

        switch (SceneStateManager.Instance.GetSceneState())
        {
            case SceneStateManager.SceneState.Instruction:
                detectedPitch.GetComponent<PitchDetectionSystem>().StartPlaying(); // Play audio source if the current scene state is 'Instruction'
                break;
            case SceneStateManager.SceneState.Countdown:
                detectedPitch.GetComponent<PitchDetectionSystem>().StartRecording(); // Play through microphone if the current scene state is 'Countdown/Gameplay'
                break;
        }

        PolloController.Instance.SetActive(true);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        barTimeline.ConfigureSection();
        audioSource.PlayScheduled(0);

    }
    
    void Update()
    {
        var sceneManager = GetComponent<SceneManagerScript>();

        // Check if the song has ended
        // If the condition is true, it'll change current scene state 'Instruction' to 'Countdown/Gameplay'
        if (GetAudioSourceTime() == 0
            && songPlayed
            && (AudioSettings.dspTime - dspTimeSong) > songDelayInSeconds
            && SceneStateManager.Instance.GetSceneState() != SceneStateManager.SceneState.Pause)
        {
            switch (SceneStateManager.Instance.GetSceneState())
            {
                case SceneStateManager.SceneState.Instruction:
                    songPlayed = false;
                    ResetScene();
                    SceneStateManager.Instance.ChangeSceneState(SceneStateManager.SceneState.Countdown);
                    break;
                case SceneStateManager.SceneState.EndOfSong:
                    sceneManager.SceneInvoke("ResultPage");
                    break;
            }
        }

        if(GetAudioSourceTime() >= GetAudioSourceLength() - 3 && endOfSong == false){
            endOfSong = true;
            SceneStateManager.Instance.ChangeSceneState(SceneStateManager.SceneState.EndOfSong);
        }
    }

    // Get current playback position in metric times
    public static double GetAudioSourceTime()
    {
        return (double)Instance.audioSource.timeSamples / Instance.audioSource.clip.frequency;
        //return AudioSettings.dspTime - Instance.dspTimeSong;
    }

    // Get current beat in float
    public static float GetCurrentBeat()
    {
        return (float)GetAudioSourceTime() / midiBPM;
    }

    // Get current audio progress in percentage, current position relatives to the clip length
    public float GetCurrentAudioProgress()
    {
        return (float)(GetAudioSourceTime() / audioSource.clip.length);
    }

    public float GetAudioSourceLength()
    {
        return audioSource.clip.length;
    }

    // Reset all instance to its default state
    public void ResetScene()
    {
        ScoreManager.Instace.Reset();
        Lane.Instance.Reset();
    }
}

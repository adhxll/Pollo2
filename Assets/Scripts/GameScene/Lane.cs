using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class Lane : MonoBehaviour
{
    //public TMPro.TextMeshProUGUI accuracyScore;
    //public TMPro.TextMeshProUGUI accuracyPercentage;

    public static Lane Instance;

    [SerializeField]
    private GameObject notePrefab = null;

    [SerializeField]
    private GameObject barPrefab = null;

    List<Note> notes = new List<Note>();
    public List<double> timeStamps = new List<double>();    // A list that store each notes timestamp (telling the exact time when its need to be spawned)
    public List<float> noteDurations = new List<float>();   // A list that store each notes ticks duration
    public List<int> midiNotes = new List<int>();   // A list that store what MIDI notes need to be played at the given note

    int spawnIndex = 0;
    int inputIndex = 0;
    int barIndex = 0;
    int correctNotes = 0;

    //Variable to count in exact midi note to prevent Hit() function called accidentally
    int averageCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Iterate through the given MIDI (or JSON in this case)
    // and set timeStamps, noteDurations, and midiNotes to the list.
    public void SetTimeStamps(MIDI.Notes[] array)
    {
        foreach (var note in array)
        {
            timeStamps.Add(note.time);
            noteDurations.Add((float)note.durationTicks / SongManager.midiFile.header.ppq);
            midiNotes.Add(note.midi);
            //Debug.Log(midiNotes.Count);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (SongManager.Instance.songPlayed)
        {
            if (SongManager.GetCurrentBeat() >= barIndex)
            {
                //Debug.Log($"Current Time = {SongManager.GetAudioSourceTime()}, Current Beat = {SongManager.GetCurrentBeat()}, Current Index {barIndex}");
                SpawnMusicBar();
            }

            if (spawnIndex < timeStamps.Count)
            {
                if (SongManager.GetAudioSourceTime() >= timeStamps[spawnIndex] - SongManager.Instance.noteTime)
                {
                    SpawnMusicNote();
                }
            }

            if (inputIndex < timeStamps.Count)
            {
                double timeStamp = timeStamps[inputIndex];
                double marginOfError = SongManager.Instance.marginOfError;
                double audioTime = SongManager.GetAudioSourceTime() - (SongManager.Instance.inputDelayInMilliseconds / 1000.0);

                if ((SongManager.Instance.detectedPitch.midiNote == midiNotes[inputIndex] ||
                   SongManager.Instance.detectedPitch.midiNote + 12 == midiNotes[inputIndex] ||
                   SongManager.Instance.detectedPitch.midiNote - 12 == midiNotes[inputIndex]))
                {
                    if (Math.Abs(audioTime - timeStamp) < marginOfError)
                    {
                        //Algo to count if the note really is played & not accidentally detected
                        averageCount++;
                        if(averageCount >= 5){
                            Hit();
                            //Reset count after Hit
                            averageCount = 0;
                        }
                        
                    }
                    else
                    {
                        //print($"Hit inaccurate on {inputIndex} note with {Math.Abs(audioTime - timeStamp)} delay");
                    }
                }

                if (timeStamp + marginOfError <= audioTime)
                {
                    Miss();
                    //Reset count after miss
                    averageCount = 0;
                }
                if(averageCount>0){
                    Debug.Log($"Average Count : {averageCount}");
                }
                
            }
            Debug.Log(spawnIndex);
            Debug.Log(midiNotes.Count);


            //accuracyScore.text = $"{correctNotes} / {inputIndex}";
            //accuracyPercentage.text = ((float)correctNotes / inputIndex * 100).ToString("0.00") + " %";
            //Debug.Log($"ACCURACY {(float)correctNotes / inputIndex * 100}%");
        }
    }

    private void SpawnMusicNote()
    {
        var note = Instantiate(notePrefab, transform);
        note.GetComponent<Note>().assignedTime = timeStamps[spawnIndex];
        note.GetComponent<Note>().noteLength = noteDurations[spawnIndex];
        notes.Add(note.GetComponent<Note>());
        spawnIndex++;
    }

    private void SpawnMusicBar()
    {
        var bar = Instantiate(barPrefab, transform);
        bar.GetComponent<Bar>().assignedTime = SongManager.GetAudioSourceTime();
        barIndex++;
    }

    private void Hit()
    {
        var note = notes[inputIndex];

        ScoreManager.Hit();
        note.GetComponent<SpriteRenderer>().sprite = note.noteRight;
        AnimationManager.Instace.AnimateHit(note.gameObject, 0.1f);

        correctNotes++;
        inputIndex++;
    }

    private void Miss()
    {
        var note = notes[inputIndex];

        ScoreManager.Miss();
        notes[inputIndex].GetComponent<SpriteRenderer>().sprite = note.noteWrong;
        AnimationManager.Instace.AnimateHit(note.gameObject, -0.1f);
        inputIndex++;
    }

    // Reset Lane to the initial state
    public void Reset()
    {
        spawnIndex = 0;
        inputIndex = 0;
        barIndex = 0;
        correctNotes = 0;
        notes.Clear();
    }

    // temporary function to pass the session's data
    public void OnDestroy()
    {
        PlayerPrefs.SetInt("SessionTotalNotes", inputIndex);
        PlayerPrefs.SetInt("SessionCorrectNotes", correctNotes);
    }
}

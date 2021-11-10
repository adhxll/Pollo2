using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BarTimeline : MonoBehaviour
{
    [SerializeField]
    private GameObject barBackground = null;

    [SerializeField]
    private GameObject barIndicator = null;

    [SerializeField]
    private GameObject barPrefab = null;

    private List<GameObject> barList = new List<GameObject>();
    private List<double> timestamp = new List<double>();

    int currentSection = 0;

    RectTransform barSprite
    {
        get
        {
            return barIndicator.GetComponent<RectTransform>();
        }
    }

    float height
    {
        get
        {
            return barSprite.rect.height;
        }
    }

    float width
    {
        get
        {
            var rect = barBackground.GetComponent<RectTransform>();
            return rect.rect.width;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        var relativePost = SongManager.Instance.GetCurrentAudioProgress();
        var progressWidth = width * relativePost;

        // Update sprite width based on current audio progress
        barSprite.sizeDelta = new Vector2(progressWidth, height);

        SectionManager();
    }

    // Populate section with 'C1' note from midi as separator
    public void SetTimeStamps(MIDI.Notes[] array)
    {
        foreach (var note in array)
        {
            timestamp.Add(note.time);
        }
    }

    // Place separator/divider according to their position in audioSourceLength
    public void PlaceTimestamp()
    {
        foreach (var time in timestamp)
        {
            var left = new Vector2(- width / 2, 0);
            var right = new Vector2(width / 2, 0);
            var bar = Instantiate(barPrefab, transform);
            var relativePost = time / SongManager.Instance.GetAudioSourceLength();

            // Add instantiated bar to list
            barList.Add(bar);

            // Set bar width and height
            bar.transform.localScale = new Vector2(1f, 16f);

            // Place bar based on linear interpolation between start point, end point, and audio progress
            bar.transform.localPosition = Vector3.Lerp(left, right, (float)relativePost);
        }
    }

    // Configuring inputIndex and spawnIndex for each section
    void ConfigureSection()
    {
        var musicNotes = Lane.Instance.timeStamps;
        var noteTime = SongManager.Instance.noteTime;
        var i = 0;
        var j = 0;

        foreach (var section in timestamp)
        {
            var barObj = barList[j].GetComponent<Timestamp>();

            for (; i < musicNotes.Count; i++)
            {
                if (section == musicNotes[i] || section < musicNotes[i])
                {
                    barObj.inputIndex = i;
                }

                if (section + noteTime < musicNotes[i])
                {
                    barObj.spawnIndex = i;
                    j++;
                    Debug.Log($"[GUESS] Section {j}, inputIndex is {barObj.inputIndex}, spawnIndex is {barObj.spawnIndex}");
                    break;
                }
            }
        }
    }

    void SectionManager()
    {
        // Determine current section that the player's at
        if (currentSection < timestamp.Count - 1)
        {
            if (SongManager.GetAudioSourceTime() >= timestamp[currentSection + 1])
            {
                currentSection++;
                barList[currentSection].GetComponent<Image>().color = new Color32(255, 200, 113, 255);
                ConfigureSection();
                SaveLane();
            }
        }

        // Restart current section
        if (Input.GetKeyUp(KeyCode.R))
        {
            ResetLane();
            Lane.Instance.ClearRest();
        }

        // Go to previous section
        if (Input.GetKeyUp(KeyCode.E))
        {
            if (currentSection > 0)
                currentSection--;

            ResetLane();
            Lane.Instance.ClearRest();
        }

        // Go to next section
        if (Input.GetKeyUp(KeyCode.T))
        {
            if (currentSection < timestamp.Count)
                currentSection++;

            ResetLane();
            Lane.Instance.FillRest();
        }
    }

    void SaveLane()
    {
        var barObj = barList[currentSection].GetComponent<Timestamp>();
        barObj.inputIndex = Lane.Instance.inputIndex;
        barObj.spawnIndex = Lane.Instance.spawnIndex;
        barObj.barIndex = Lane.Instance.barIndex;

        Debug.Log($"[CORRECT] Section {currentSection}, inputIndex is {barObj.inputIndex}, spawnIndex is {barObj.spawnIndex}");
    }

    // Assign some lane input index variables to the saved index
    void ResetLane()
    {
        var barObj = barList[currentSection].GetComponent<Timestamp>();
        SceneStateManager.Instance.SetAudioTime((float)timestamp[currentSection]);
        Lane.Instance.inputIndex = barObj.inputIndex;
        Lane.Instance.spawnIndex = barObj.spawnIndex;
        Lane.Instance.barIndex = barObj.barIndex;
        Lane.Instance.DestroyChild();
    }
}

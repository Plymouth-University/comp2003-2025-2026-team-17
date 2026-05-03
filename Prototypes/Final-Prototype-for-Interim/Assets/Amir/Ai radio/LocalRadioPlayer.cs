using System.Collections.Generic;
using UnityEngine;

public class LocalRadioPlayer : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource musicSource;

    [Header("Playlist")]
    public List<AudioClip> tracks = new List<AudioClip>();
    public bool playOnStart = false;
    public bool loopPlaylist = true;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.T;
    public KeyCode nextKey = KeyCode.Y;
    public KeyCode previousKey = KeyCode.U;

    private int currentTrackIndex = 0;
    private bool radioOn = false;

    void Start()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
        }

        if (playOnStart && tracks.Count > 0)
        {
            radioOn = true;
            PlayTrack(currentTrackIndex);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleRadio();

        if (Input.GetKeyDown(nextKey))
            NextTrack();

        if (Input.GetKeyDown(previousKey))
            PreviousTrack();

        if (radioOn && musicSource != null && !musicSource.isPlaying && tracks.Count > 0)
        {
            PlayNextAutomatically();
        }
    }

    public void ToggleRadio()
    {
        if (musicSource == null || tracks.Count == 0)
            return;

        radioOn = !radioOn;

        if (radioOn)
        {
            PlayTrack(currentTrackIndex);
        }
        else
        {
            musicSource.Pause();
        }
    }

    public void NextTrack()
    {
        if (tracks.Count == 0 || musicSource == null)
            return;

        currentTrackIndex++;

        if (currentTrackIndex >= tracks.Count)
        {
            currentTrackIndex = loopPlaylist ? 0 : tracks.Count - 1;
        }

        if (radioOn)
            PlayTrack(currentTrackIndex);
    }

    public void PreviousTrack()
    {
        if (tracks.Count == 0 || musicSource == null)
            return;

        currentTrackIndex--;

        if (currentTrackIndex < 0)
        {
            currentTrackIndex = loopPlaylist ? tracks.Count - 1 : 0;
        }

        if (radioOn)
            PlayTrack(currentTrackIndex);
    }

    private void PlayNextAutomatically()
    {
        currentTrackIndex++;

        if (currentTrackIndex >= tracks.Count)
        {
            if (loopPlaylist)
                currentTrackIndex = 0;
            else
            {
                radioOn = false;
                return;
            }
        }

        PlayTrack(currentTrackIndex);
    }

    private void PlayTrack(int index)
    {
        if (musicSource == null || tracks.Count == 0 || index < 0 || index >= tracks.Count)
            return;

        musicSource.clip = tracks[index];
        musicSource.Play();

        Debug.Log("Now playing: " + tracks[index].name);
    }
}
//using System.Collections;
//using System.Collections.Generic;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.Networking;


//public class MultiSearchRadioPlayer : MonoBehaviour
//{
//    public string[] searchTerms = new string[]
//   {
//        "coldplay",
//        "ed sheeran",
//        "dua lipa",
//        "the weeknd"
//   };



//    [Header("Search Settings")]
//    public int limitPerSearch = 25;
//    public string country = "gb";

//    [Header("Audio")]
//    public AudioSource musicSource;

//    [Header("Playback")]
//    public bool autoStart = true;
//    public bool loopPlaylist = true;

//    private List<ITunesTrack> playlist = new List<ITunesTrack>();
//    private HashSet<string> usedPreviewUrls = new HashSet<string>();

//    private int currentTrackIndex = 0;
//    private bool isLoadingTrack = false;
//    private bool playlistLoaded = false;

//    void Start()
//    {
//        StartCoroutine(LoadAllSearches());
//    }

//    void Update()
//    {
//        if (playlistLoaded && !isLoadingTrack && playlist.Count > 0 && !musicSource.isPlaying)
//        {
//            StartCoroutine(PlayNextTrack());
//        }




//        if (Input.GetKeyDown(KeyCode.R))
//            ToggleRadio();

//        if (Input.GetKeyDown(KeyCode.RightBracket))
//            NextTrack();

//        if (Input.GetKeyDown(KeyCode.LeftBracket))
//            PreviousTrack();
//    }



//    IEnumerator LoadAllSearches()
//    {
//        playlist.Clear();
//        usedPreviewUrls.Clear();

//        for (int i = 0; i < searchTerms.Length; i++)
//        {
//            string term = searchTerms[i];

//            if (string.IsNullOrWhiteSpace(term))
//                continue;

//            yield return StartCoroutine(LoadSearchResults(term));
//        }

//        playlistLoaded = true;
//        Debug.Log("Final playlist size: " + playlist.Count);

//        if (autoStart && playlist.Count > 0)
//        {
//            StartCoroutine(PlayTrack(currentTrackIndex));
//        }


//    }


//    IEnumerator LoadSearchResults(string term)
//    {
//        string encodedTerm = UnityWebRequest.EscapeURL(term);
//        string url = $"https://itunes.apple.com/search?term={encodedTerm}&limit={limitPerSearch}&entity=song&country={country}";

//        using (UnityWebRequest request = UnityWebRequest.Get(url))
//        {
//            yield return request.SendWebRequest();


//            if (request.result != UnityWebRequest.Result.Success)

//            if (request.isNetworkError || request.isHttpError)

//            {
//                Debug.LogError("Search failed for " + term + ": " + request.error);
//                yield break;
//            }

//            string json = request.downloadHandler.text;
//            ITunesSearchResponse response = JsonUtility.FromJson<ITunesSearchResponse>(json);

//            if (response != null && response.results != null)
//            {
//                for (int i = 0; i < response.results.Length; i++)
//                {
//                    ITunesTrack track = response.results[i];

//                    if (track == null || string.IsNullOrEmpty(track.previewUrl))
//                        continue;

//                    if (!usedPreviewUrls.Contains(track.previewUrl))
//                    {
//                        usedPreviewUrls.Add(track.previewUrl);
//                        playlist.Add(track);
//                    }
//                }

//                Debug.Log("Loaded from " + term + ": " + response.results.Length);
//            }
//            else
//            {
//                Debug.LogWarning("No results found for " + term);
//            }
//        }
//    }




//    IEnumerator PlayTrack(int index)
//    {
//        if (playlist.Count == 0 || index < 0 || index >= playlist.Count)
//            yield break;

//        isLoadingTrack = true;

//        string previewUrl = playlist[index].previewUrl;

//        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(previewUrl, AudioType.MPEG))
//        {
//            yield return audioRequest.SendWebRequest();


//            if (audioRequest.result != UnityWebRequest.Result.Success)

//            if (audioRequest.isNetworkError || audioRequest.isHttpError)
//            {
//                Debug.LogError("Audio load failed: " + audioRequest.error);
//                isLoadingTrack = false;
//                yield break;
//            }

//            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
//            musicSource.clip = clip;
//            musicSource.Play();

//            Debug.Log("Now playing: " + playlist[index].artistName + " - " + playlist[index].trackName);
//        }

//        isLoadingTrack = false;
//    }




//    IEnumerator PlayNextTrack()
//    {
//        if (playlist.Count == 0)
//            yield break;

//        currentTrackIndex++;

//        if (currentTrackIndex >= playlist.Count)
//        {
//            if (loopPlaylist)
//                currentTrackIndex = 0;
//            else
//                yield break;
//        }

//        yield return StartCoroutine(PlayTrack(currentTrackIndex));
//    }


//    public void ToggleRadio()
//        {
//        if (musicSource == null || playlist.Count == 0)
//            return;

//        if (musicSource.isPlaying) 
//        {
//            musicSource.Stop();
//        }

//        else
//        {
//            StartCoroutine(PlayTrack(currentTrackIndex));
//        }
//    }

//    public void NextTrack()
//    {
//        if (playlist.Count == 0 || isLoadingTrack)
//            return;

//        musicSource.Stop();
//        StartCoroutine(PlayNextTrack());
//    }

//    public void PreviousTrack()
//    {
//        if (playlist.Count == 0 || isLoadingTrack)
//            return;

//        currentTrackIndex--;

//        if (currentTrackIndex < 0)
//            currentTrackIndex = playlist.Count - 1;

//        musicSource.Stop();
//        StartCoroutine(PlayTrack(currentTrackIndex));
//    }




//}
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RealAIRadio : MonoBehaviour
{
    [Header("Radio Settings")]
    public AudioSource carSpeakers;

    // Paste your free Hugging Face token here inside the quotes!
    private string hfToken = "";

    // The endpoint for Meta's free MusicGen model
    private string apiUrl = "https://api-inference.huggingface.co/models/facebook/musicgen-small";

    // -- UPDATE: Removed Update() method so that the radio is only called by the EzrealCarController script when the player presses the button to turn on the radio.

    public void TurnOnRadio()
    {
        // You can change this prompt to get different styles of music!
        string prompt = "80s synthwave driving down the highway, upbeat";
        StartCoroutine(GenerateFreeAIMusic(prompt));
    }

    IEnumerator GenerateFreeAIMusic(string prompt)
    {
        Debug.Log("Tuning the AI Radio... Please wait.");

        // 1. Package our prompt into JSON
        string json = $"{{\"inputs\": \"{prompt}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // 2. Setup the web request to the AI server
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // We tell Unity to expect an audio file back
            request.downloadHandler = new DownloadHandlerAudioClip(apiUrl, AudioType.WAV);

            // Set our security headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + hfToken);

            // 3. Send the request and wait for the AI to compose the song
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Song generated successfully!");

                // 4. Extract the audio and play it
                AudioClip generatedClip = DownloadHandlerAudioClip.GetContent(request);
                carSpeakers.clip = generatedClip;
                carSpeakers.Play();
            }
            else
            {
                Debug.LogError("Radio static! The AI server might be busy: " + request.error);
            }
        }
    }
}
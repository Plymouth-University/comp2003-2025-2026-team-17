using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RealAIRadio : MonoBehaviour
{
    [Header("Radio Settings")]
    public AudioSource carSpeakers;

    [Header("API Config")]
    [Tooltip("Seconds to wait before retrying when the model is still loading (HTTP 503)")]
    public float modelLoadRetryDelay = 20f;

    [Tooltip("Maximum number of retry attempts when the model is warming up")]
    public int maxRetries = 3;

    private string hfToken = "";

    private const string ApiUrl = "https://api-inference.huggingface.co/models/cvssp/audioldm-s-full-v2";

    // -------------------------------------------------------------------------
    // .env loader — reads Assets/StreamingAssets/.env at runtime
    // -------------------------------------------------------------------------
    private static readonly Dictionary<string, string> EnvVars = new();

    private static void LoadEnv()
    {
        string path = Path.Combine(Application.streamingAssetsPath, ".env");

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                "[RealAIRadio] .env file not found at: " + path + "\n" +
                "Create Assets/StreamingAssets/.env and add: HF_TOKEN=hf_yourToken");
            return;
        }

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            int sep = line.IndexOf('=');
            if (sep <= 0) continue;

            string key = line.Substring(0, sep).Trim();
            string value = line.Substring(sep + 1).Trim();

            // Strip optional surrounding quotes: HF_TOKEN="abc" → abc
            if (value.Length >= 2 &&
                ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                 (value.StartsWith("'") && value.EndsWith("'"))))
            {
                value = value.Substring(1, value.Length - 2);
            }

            EnvVars[key] = value;
        }

        Debug.Log($"[RealAIRadio] Loaded {EnvVars.Count} variable(s) from .env");
    }

    private static string GetEnv(string key)
    {
        return EnvVars.TryGetValue(key, out string value) ? value : "";
    }

    // -------------------------------------------------------------------------

    private void Awake()
    {
        LoadEnv();

        // Token comes exclusively from .env — never hardcoded in source
        hfToken = GetEnv("HF_TOKEN").Trim();

        if (string.IsNullOrEmpty(hfToken))
        {
            Debug.LogError(
                "[RealAIRadio] HF_TOKEN not found. " +
                "Add HF_TOKEN=hf_yourToken to Assets/StreamingAssets/.env");
        }
    }

    // Called externally by EzrealCarController when the player presses the radio button.
    public void TurnOnRadio()
    {
        if (string.IsNullOrEmpty(hfToken))
        {
            Debug.LogError("[RealAIRadio] Cannot start radio — HF_TOKEN is missing.");
            return;
        }

        string prompt = "80s synthwave driving down the highway, upbeat";
        StartCoroutine(GenerateAIMusic(prompt, maxRetries));
    }

    // -------------------------------------------------------------------------

    private IEnumerator GenerateAIMusic(string prompt, int retriesLeft)
    {
        Debug.Log($"[RealAIRadio] Requesting AI music... (attempts remaining: {retriesLeft})");

        string json = JsonUtility.ToJson(new MusicGenRequest { inputs = prompt });
        byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyBytes);
        request.downloadHandler = new DownloadHandlerAudioClip(ApiUrl, AudioType.WAV);
        request.timeout = 120;

        request.SetRequestHeader("Authorization", "Bearer " + hfToken);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "audio/wav");
        request.SetRequestHeader("X-Wait-For-Model", "false");

        yield return request.SendWebRequest();

        long statusCode = request.responseCode;

        switch (request.result)
        {
            case UnityWebRequest.Result.Success when statusCode == 200:
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

                    if (clip == null)
                    {
                        Debug.LogError("[RealAIRadio] Received 200 but could not decode WAV data.");
                        yield break;
                    }

                    Debug.Log("[RealAIRadio] Song generated — tuning in!");
                    carSpeakers.clip = clip;
                    carSpeakers.Play();
                    break;
                }

            case UnityWebRequest.Result.Success when statusCode == 503:
            case UnityWebRequest.Result.ProtocolError when statusCode == 503:
                {
                    if (retriesLeft > 0)
                    {
                        Debug.Log($"[RealAIRadio] Model is loading — retrying in {modelLoadRetryDelay}s " +
                                  $"({retriesLeft} attempt(s) left).");
                        yield return new WaitForSeconds(modelLoadRetryDelay);
                        yield return GenerateAIMusic(prompt, retriesLeft - 1);
                    }
                    else
                    {
                        Debug.LogError("[RealAIRadio] Model failed to load after all retries. Try again later.");
                    }
                    break;
                }

            case UnityWebRequest.Result.ProtocolError when statusCode == 401:
                Debug.LogError(
                    "[RealAIRadio] 401 Unauthorised — check HF_TOKEN in StreamingAssets/.env.\n" +
                    "Make sure there are no extra spaces or newlines around the token value.");
                break;

            case UnityWebRequest.Result.ProtocolError when statusCode == 429:
                Debug.LogError("[RealAIRadio] 429 Rate limited — you've hit the free-tier quota. " +
                               "Wait a few minutes and try again.");
                break;

            default:
                // 1. Set a default message just in case the server sent absolutely nothing
                string hiddenServerReason = "No extra data provided by server.";

                // 2. If the server DID send data back (the error message), translate the raw bytes into text!
                if (request.downloadHandler.data != null)
                {
                    hiddenServerReason = Encoding.UTF8.GetString(request.downloadHandler.data);
                }

                // 3. Print it to the console so we can finally see what Hugging Face is complaining about
                Debug.LogError($"[RealAIRadio] Request failed. HTTP {statusCode} | Unity result: {request.result} | {request.error}\n" +
                               $"Exact Server Reason: {hiddenServerReason}");
                break;
        }
    }

    // -------------------------------------------------------------------------

    [System.Serializable]
    private class MusicGenRequest
    {
        public string inputs;
    }
}

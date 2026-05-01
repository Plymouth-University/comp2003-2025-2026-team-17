using System;

[Serializable]
public class ITunesSearchResponse
{
    public int resultCount;
    public ITunesTrack[] results;
}

[Serializable]
public class ITunesTrack
{
    public string trackName;
    public string artistName;
    public string previewUrl;
    public string artworkUrl100;
}
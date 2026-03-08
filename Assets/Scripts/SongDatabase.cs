using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Song data models and auto-discovery of songs from Resources folders.
/// Each song folder in Resources/Audio/GuitarFlash/ should have:
///   - audio.ogg (or audio.mp3) — the music file
///   - info.txt — "Song Title|Artist Name" (optional, defaults to folder name)
///
/// Beatmaps are generated either via FFT analysis (if audio is present)
/// or algorithmically via BPM (as fallback).
/// </summary>
public static class SongDatabase
{
    // Note data: when and where
    [System.Serializable]
    public struct NoteData
    {
        public float time;  // seconds from start
        public int lane;    // 0-3 (4 lanes)

        public NoteData(float time, int lane)
        {
            this.time = time;
            this.lane = lane;
        }
    }

    // Song info
    [System.Serializable]
    public struct SongData
    {
        public string id;
        public string title;
        public string artist;
        public int bpm;
        public float duration;       // seconds
        public string audioResource; // Resources path (without extension)
        public string folderPath;    // Resources folder path
        public Color accentColor;
        public bool hasAudio;
    }

    // Accent colors for songs (cycle through)
    private static readonly Color[] accentColors = new Color[]
    {
        new Color(1f, 0.42f, 0.42f, 1f),     // Red
        new Color(0.306f, 0.804f, 0.769f, 1f), // Teal
        new Color(1f, 0.671f, 0.298f, 1f),    // Orange
        new Color(0.29f, 0.565f, 0.886f, 1f), // Blue
        new Color(0.863f, 0.078f, 0.235f, 1f), // Crimson
        new Color(1f, 0.902f, 0.427f, 1f),    // Yellow
    };

    // ========== AUTO-SCAN SONGS ==========

    /// <summary>
    /// Scan Resources/Audio/GuitarFlash/ for song folders.
    /// Each subfolder should contain audio.ogg (or audio.mp3) and optionally info.txt.
    /// </summary>
    public static SongData[] DiscoverSongs()
    {
        List<SongData> songs = new List<SongData>();
        string basePath = "Audio/GuitarFlash";

        // Load all AudioClips from subfolders
        // Unity Resources.LoadAll loads from a path recursively
        AudioClip[] clips = Resources.LoadAll<AudioClip>(basePath);

        // Also try to find info.txt files
        TextAsset[] infos = Resources.LoadAll<TextAsset>(basePath);

        // Build a map of folder → assets
        Dictionary<string, AudioClip> clipMap = new Dictionary<string, AudioClip>();
        Dictionary<string, TextAsset> infoMap = new Dictionary<string, TextAsset>();

        foreach (var clip in clips)
        {
            // clip.name will be the filename without extension (e.g., "audio")
            // We need to figure out which folder it's in.
            // Resources.LoadAll doesn't give us path info, so we use a naming convention:
            // The audio file should be named "audio" and the info file "info"
            // We'll match them by trying to load from known subfolder names.
            clipMap[clip.name] = clip;
        }

        foreach (var info in infos)
        {
            infoMap[info.name] = info;
        }

        // Strategy: try to discover folders by loading a "manifest"
        // Since Unity Resources doesn't have a directory listing API,
        // we use the loaded assets to infer folders.

        // If we found clips named "audio", there's at least one song.
        // But we can't distinguish folders this way. So we use a different approach:
        // Load everything and try known folder names, OR accept any AudioClip found.

        // Better approach: Accept any AudioClip found (not just "audio" named ones)
        // and pair them with info.txt files that share a naming prefix.

        // Simplest working approach: each song folder has files like:
        //   insonia/audio.ogg  and  insonia/info.txt
        // When loaded via Resources.LoadAll, Unity gives us the clip named "audio"
        // But we can't distinguish between folders!

        // BEST APPROACH: Use a single folder level where:
        //   Resources/Audio/GuitarFlash/insonia.ogg
        //   Resources/Audio/GuitarFlash/insonia_info.txt (TextAsset)
        // OR the subfolder approach with unique audio filenames:
        //   Resources/Audio/GuitarFlash/insonia/insonia.ogg
        //   Resources/Audio/GuitarFlash/insonia/info.txt

        // Let's support BOTH approaches:

        // Approach 1: Flat files (songname.ogg + songname_info.txt)
        HashSet<string> processed = new HashSet<string>();

        foreach (var kvp in clipMap)
        {
            string clipName = kvp.Key;
            AudioClip clip = kvp.Value;

            if (clipName == "README") continue; // skip readme

            string songId = clipName.ToLower().Replace(" ", "-");
            if (processed.Contains(songId)) continue;
            processed.Add(songId);

            string title = clipName;
            string artist = "Artista Desconhecido";

            // Try to find matching info
            string infoKey = clipName + "_info";
            if (infoMap.ContainsKey(infoKey))
            {
                ParseInfo(infoMap[infoKey].text, ref title, ref artist);
            }
            else if (infoMap.ContainsKey("info"))
            {
                // Subfolder approach: info.txt in same folder
                ParseInfo(infoMap["info"].text, ref title, ref artist);
            }

            SongData song = new SongData
            {
                id = songId,
                title = title,
                artist = artist,
                bpm = 0, // Unknown — will use FFT
                duration = clip.length,
                audioResource = basePath + "/" + clipName,
                folderPath = basePath,
                accentColor = accentColors[songs.Count % accentColors.Length],
                hasAudio = true
            };

            songs.Add(song);
        }

        // If no songs found, add a demo entry
        if (songs.Count == 0)
        {
            songs.Add(new SongData
            {
                id = "demo",
                title = "Sem Músicas",
                artist = "Adicione músicas na pasta Resources/Audio/GuitarFlash/",
                bpm = 120,
                duration = 30f,
                audioResource = "",
                folderPath = "",
                accentColor = new Color(0.5f, 0.5f, 0.5f, 1f),
                hasAudio = false
            });
        }

        return songs;
    }

    private static void ParseInfo(string text, ref string title, ref string artist)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Format: "Title|Artist" (one line)
        string[] lines = text.Trim().Split('\n');
        if (lines.Length > 0)
        {
            string[] parts = lines[0].Split('|');
            if (parts.Length >= 1) title = parts[0].Trim();
            if (parts.Length >= 2) artist = parts[1].Trim();
        }
    }

    // ========== BEATMAP GENERATION ==========

    /// <summary>
    /// Generate a beatmap for a song. Uses FFT if AudioClip is available, otherwise BPM-based.
    /// </summary>
    public static List<NoteData> GenerateBeatmap(SongData song, int difficulty, AudioClip clip = null)
    {
        // If we have an audio clip, use FFT beat detection
        if (clip != null)
        {
            return BeatDetector.DetectBeats(clip, difficulty);
        }

        // Fallback: BPM-based generation
        if (song.bpm > 0)
        {
            return GenerateBPMBeatmap(song, difficulty);
        }

        // No audio, no BPM — return empty
        return new List<NoteData>();
    }

    /// <summary>
    /// Fallback: Generate beatmap from BPM pattern.
    /// </summary>
    private static List<NoteData> GenerateBPMBeatmap(SongData song, int difficulty)
    {
        List<NoteData> notes = new List<NoteData>();
        float beatInterval = 60f / song.bpm;
        float startDelay = 3f;

        System.Random rng = new System.Random(song.id.GetHashCode() + difficulty * 1000);

        float time = startDelay;
        float endTime = song.duration - 5f;

        int patternIndex = 0;

        while (time < endTime)
        {
            switch (difficulty)
            {
                case 0:
                    notes.Add(new NoteData(time, rng.Next(0, 4)));
                    time += beatInterval;
                    if (rng.Next(100) < 20) time += beatInterval;
                    break;

                case 1:
                    GenerateMediumPattern(notes, rng, ref time, beatInterval, ref patternIndex);
                    break;

                case 2:
                    GenerateHardPattern(notes, rng, ref time, beatInterval, ref patternIndex);
                    break;
            }
        }

        notes.Sort((a, b) => a.time.CompareTo(b.time));
        return notes;
    }

    private static void GenerateMediumPattern(List<NoteData> notes, System.Random rng,
        ref float time, float beat, ref int patternIndex)
    {
        int pattern = patternIndex % 6;
        patternIndex++;

        switch (pattern)
        {
            case 0:
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat;
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat;
                break;
            case 1:
                int lane = rng.Next(0, 4);
                notes.Add(new NoteData(time, lane));
                time += beat * 0.5f;
                notes.Add(new NoteData(time, (lane + 1) % 4));
                time += beat * 0.5f;
                break;
            case 2:
                int l1 = rng.Next(0, 3);
                notes.Add(new NoteData(time, l1));
                notes.Add(new NoteData(time, l1 + 1));
                time += beat;
                break;
            case 3:
                for (int i = 0; i < 4; i++)
                {
                    notes.Add(new NoteData(time, i));
                    time += beat * 0.5f;
                }
                break;
            case 4:
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat * 1.5f;
                break;
            case 5:
                for (int i = 3; i >= 0; i--)
                {
                    notes.Add(new NoteData(time, i));
                    time += beat * 0.5f;
                }
                break;
        }
    }

    private static void GenerateHardPattern(List<NoteData> notes, System.Random rng,
        ref float time, float beat, ref int patternIndex)
    {
        int pattern = patternIndex % 8;
        patternIndex++;

        switch (pattern)
        {
            case 0:
                for (int i = 0; i < 4; i++)
                {
                    notes.Add(new NoteData(time, rng.Next(0, 4)));
                    time += beat * 0.25f;
                }
                break;
            case 1:
                int l1 = rng.Next(0, 3);
                notes.Add(new NoteData(time, l1));
                notes.Add(new NoteData(time, l1 + 1));
                time += beat * 0.5f;
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat * 0.5f;
                break;
            case 2:
                notes.Add(new NoteData(time, 0));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, 3));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, 1));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, 2));
                time += beat * 0.25f;
                break;
            case 3:
                int bl = rng.Next(0, 2);
                notes.Add(new NoteData(time, bl));
                notes.Add(new NoteData(time, bl + 1));
                notes.Add(new NoteData(time, bl + 2));
                time += beat;
                break;
            case 4:
                for (int i = 0; i < 4; i++)
                {
                    notes.Add(new NoteData(time, i));
                    time += beat * 0.25f;
                }
                break;
            case 5:
                int la = rng.Next(0, 2);
                int lb = la + 2;
                notes.Add(new NoteData(time, la));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, lb));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, la));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, lb));
                time += beat * 0.25f;
                break;
            case 6:
                int dl = rng.Next(0, 3);
                notes.Add(new NoteData(time, dl));
                notes.Add(new NoteData(time, dl + 1));
                time += beat * 0.75f;
                break;
            case 7:
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat * 0.5f;
                int d = rng.Next(0, 3);
                notes.Add(new NoteData(time, d));
                notes.Add(new NoteData(time, d + 1));
                time += beat * 0.5f;
                break;
        }
    }
}

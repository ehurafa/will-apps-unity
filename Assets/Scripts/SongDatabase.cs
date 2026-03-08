using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Song data models and beatmap generation based on BPM.
/// Each song has pre-generated beatmaps for 3 difficulty levels.
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
        public Color accentColor;
    }

    // ========== SONG LIST ==========

    public static SongData[] AllSongs => new[]
    {
        new SongData
        {
            id = "insonia",
            title = "Insônia",
            artist = "Hungria ft Tribo da Periferia",
            bpm = 136,
            duration = 260f, // ~4:20
            audioResource = "Audio/GuitarFlash/insonia",
            accentColor = new Color(1f, 0.42f, 0.42f, 1f) // red vibe
        }
    };

    // ========== BEATMAP GENERATION ==========

    public static List<NoteData> GenerateBeatmap(SongData song, int difficulty)
    {
        // difficulty: 0=Easy, 1=Medium, 2=Hard
        List<NoteData> notes = new List<NoteData>();
        float beatInterval = 60f / song.bpm;
        float startDelay = 3f; // Seconds before first note

        // Seed with song name hash so beatmap is always the same
        System.Random rng = new System.Random(song.id.GetHashCode() + difficulty * 1000);

        float time = startDelay;
        float endTime = song.duration - 5f; // Stop 5s before end

        // Pattern templates for variety
        int patternIndex = 0;

        while (time < endTime)
        {
            switch (difficulty)
            {
                case 0: // Easy — notes on every beat, single lane
                    notes.Add(new NoteData(time, rng.Next(0, 4)));
                    time += beatInterval;
                    // Occasional rest (skip a beat)
                    if (rng.Next(100) < 20) time += beatInterval;
                    break;

                case 1: // Medium — half-beat notes, occasional doubles
                    GenerateMediumPattern(notes, rng, ref time, beatInterval, ref patternIndex);
                    break;

                case 2: // Hard — quarter-beat notes, doubles, runs
                    GenerateHardPattern(notes, rng, ref time, beatInterval, ref patternIndex);
                    break;
            }
        }

        // Sort by time
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
            case 0: // Single notes on beat
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat;
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat;
                break;

            case 1: // Half-beat pair
                int lane = rng.Next(0, 4);
                notes.Add(new NoteData(time, lane));
                time += beat * 0.5f;
                notes.Add(new NoteData(time, (lane + 1) % 4));
                time += beat * 0.5f;
                break;

            case 2: // Double (2 lanes at once)
                int l1 = rng.Next(0, 3);
                notes.Add(new NoteData(time, l1));
                notes.Add(new NoteData(time, l1 + 1));
                time += beat;
                break;

            case 3: // Staircase up
                for (int i = 0; i < 4; i++)
                {
                    notes.Add(new NoteData(time, i));
                    time += beat * 0.5f;
                }
                break;

            case 4: // Single with rest
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat * 1.5f;
                break;

            case 5: // Staircase down
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
            case 0: // Fast run
                for (int i = 0; i < 4; i++)
                {
                    notes.Add(new NoteData(time, rng.Next(0, 4)));
                    time += beat * 0.25f;
                }
                break;

            case 1: // Double notes
                int l1 = rng.Next(0, 3);
                notes.Add(new NoteData(time, l1));
                notes.Add(new NoteData(time, l1 + 1));
                time += beat * 0.5f;
                notes.Add(new NoteData(time, rng.Next(0, 4)));
                time += beat * 0.5f;
                break;

            case 2: // Zig-zag
                notes.Add(new NoteData(time, 0));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, 3));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, 1));
                time += beat * 0.25f;
                notes.Add(new NoteData(time, 2));
                time += beat * 0.25f;
                break;

            case 3: // Triple
                int base_lane = rng.Next(0, 2);
                notes.Add(new NoteData(time, base_lane));
                notes.Add(new NoteData(time, base_lane + 1));
                notes.Add(new NoteData(time, base_lane + 2));
                time += beat;
                break;

            case 4: // Fast staircase
                for (int i = 0; i < 4; i++)
                {
                    notes.Add(new NoteData(time, i));
                    time += beat * 0.25f;
                }
                break;

            case 5: // Alternating fast
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

            case 6: // Double with rest
                int dl = rng.Next(0, 3);
                notes.Add(new NoteData(time, dl));
                notes.Add(new NoteData(time, dl + 1));
                time += beat * 0.75f;
                break;

            case 7: // Mixed
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

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fast offline beat detection using energy-based onset detection.
/// Splits audio into 4 frequency bands using simple filtering,
/// then detects energy spikes (onsets) in each band.
/// Designed to run in < 1 second even for long songs.
/// </summary>
public static class BeatDetector
{
    // Analysis parameters
    private const int CHUNK_SIZE = 2048;         // Samples per energy chunk
    private const int HISTORY_SIZE = 20;         // Chunks of history for average
    private const float ONSET_THRESHOLD = 1.5f;  // Energy spike threshold
    private const float MIN_ONSET_GAP = 0.12f;   // Min seconds between onsets

    /// <summary>
    /// Analyze an AudioClip and return a list of detected notes.
    /// Uses fast energy-based analysis (no FFT/DFT).
    /// </summary>
    public static List<SongDatabase.NoteData> DetectBeats(AudioClip clip, int difficulty)
    {
        if (clip == null) return new List<SongDatabase.NoteData>();

        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int totalSamples = clip.samples;

        // Get all samples
        float[] rawSamples = new float[totalSamples * channels];
        clip.GetData(rawSamples, 0);

        // Mix to mono (take every Nth sample for speed)
        float[] mono = MixToMono(rawSamples, totalSamples, channels);

        // Split into 4 frequency bands using simple filters
        float[] low = new float[totalSamples];    // Bass (lane 0)
        float[] midLow = new float[totalSamples]; // Mid-low (lane 1)
        float[] midHigh = new float[totalSamples]; // Mid-high (lane 2)
        float[] high = new float[totalSamples];   // Treble (lane 3)

        SplitBands(mono, low, midLow, midHigh, high, totalSamples);

        // Detect onsets in each band
        float[][] bands = { low, midLow, midHigh, high };
        List<float>[] bandOnsets = new List<float>[4];
        for (int b = 0; b < 4; b++)
        {
            bandOnsets[b] = DetectOnsets(bands[b], totalSamples, sampleRate);
        }

        // Build filtered beatmap
        List<SongDatabase.NoteData> notes = BuildBeatmap(bandOnsets, difficulty);

        Debug.Log($"BeatDetector: {clip.name} -> {notes.Count} notes (difficulty {difficulty})");
        return notes;
    }

    private static float[] MixToMono(float[] raw, int totalSamples, int channels)
    {
        float[] mono = new float[totalSamples];
        if (channels == 1)
        {
            System.Array.Copy(raw, mono, totalSamples);
        }
        else
        {
            for (int i = 0; i < totalSamples; i++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += raw[i * channels + c];
                mono[i] = sum / channels;
            }
        }
        return mono;
    }

    /// <summary>
    /// Split signal into 4 frequency bands using cascaded moving averages.
    /// This is much faster than FFT while still separating bass/mid/treble.
    /// </summary>
    private static void SplitBands(float[] input, float[] low, float[] midLow,
        float[] midHigh, float[] high, int length)
    {
        // Moving average sizes (larger = lower cutoff frequency)
        // At 44100 Hz: size 200 ≈ 220 Hz cutoff, size 50 ≈ 882 Hz, size 10 ≈ 4410 Hz
        int lowSize = 200;
        int midSize = 50;
        int highSize = 10;

        float[] smooth1 = MovingAverage(input, length, lowSize);   // < ~220 Hz
        float[] smooth2 = MovingAverage(input, length, midSize);   // < ~880 Hz
        float[] smooth3 = MovingAverage(input, length, highSize);  // < ~4400 Hz

        for (int i = 0; i < length; i++)
        {
            low[i] = smooth1[i];                         // Bass
            midLow[i] = smooth2[i] - smooth1[i];         // Mid-low
            midHigh[i] = smooth3[i] - smooth2[i];        // Mid-high
            high[i] = input[i] - smooth3[i];              // Treble
        }
    }

    /// <summary>
    /// Fast moving average using running sum.
    /// </summary>
    private static float[] MovingAverage(float[] input, int length, int windowSize)
    {
        float[] output = new float[length];
        float sum = 0;
        int half = windowSize / 2;

        // Initialize sum for first window
        int initEnd = Mathf.Min(windowSize, length);
        for (int i = 0; i < initEnd; i++) sum += input[i];

        for (int i = 0; i < length; i++)
        {
            // Add new sample entering window
            int addIdx = i + half;
            if (addIdx < length) sum += input[addIdx];

            // Remove sample leaving window
            int removeIdx = i - half - 1;
            if (removeIdx >= 0) sum -= input[removeIdx];

            int count = Mathf.Min(addIdx + 1, length) - Mathf.Max(removeIdx + 1, 0);
            output[i] = sum / Mathf.Max(count, 1);
        }

        return output;
    }

    /// <summary>
    /// Detect energy onsets in a band signal.
    /// </summary>
    private static List<float> DetectOnsets(float[] band, int totalSamples, int sampleRate)
    {
        List<float> onsets = new List<float>();
        int numChunks = totalSamples / CHUNK_SIZE;

        float[] energyHistory = new float[HISTORY_SIZE];
        int histIdx = 0;
        float lastOnsetTime = -1f;

        for (int c = 0; c < numChunks; c++)
        {
            int offset = c * CHUNK_SIZE;
            float time = (float)offset / sampleRate;

            // Calculate RMS energy of this chunk
            float energy = 0;
            for (int i = 0; i < CHUNK_SIZE; i++)
            {
                float s = band[offset + i];
                energy += s * s;
            }
            energy /= CHUNK_SIZE;

            // Calculate average energy from history
            int histCount = Mathf.Min(c, HISTORY_SIZE);
            float avgEnergy = 0;
            for (int h = 0; h < histCount; h++)
                avgEnergy += energyHistory[h];
            if (histCount > 0) avgEnergy /= histCount;

            // Onset detection: current energy significantly above average
            if (energy > avgEnergy * ONSET_THRESHOLD && energy > 0.00001f)
            {
                if (time - lastOnsetTime >= MIN_ONSET_GAP)
                {
                    onsets.Add(time);
                    lastOnsetTime = time;
                }
            }

            // Update history (circular buffer)
            energyHistory[histIdx % HISTORY_SIZE] = energy;
            histIdx++;
        }

        return onsets;
    }

    /// <summary>
    /// Build beatmap from band onsets, filtered by difficulty.
    /// </summary>
    private static List<SongDatabase.NoteData> BuildBeatmap(List<float>[] bandOnsets, int difficulty)
    {
        List<SongDatabase.NoteData> notes = new List<SongDatabase.NoteData>();

        // Merge all onsets
        List<(float time, int lane)> all = new List<(float, int)>();
        for (int b = 0; b < 4; b++)
            foreach (float t in bandOnsets[b])
                all.Add((t, b));

        all.Sort((a, b) => a.time.CompareTo(b.time));

        // Difficulty filtering
        float minGap;
        float keepRatio;
        bool allowDoubles;

        switch (difficulty)
        {
            case 0: // Easy
                minGap = 0.4f;
                keepRatio = 0.3f;
                allowDoubles = false;
                break;
            case 1: // Medium
                minGap = 0.2f;
                keepRatio = 0.6f;
                allowDoubles = true;
                break;
            default: // Hard
                minGap = 0.1f;
                keepRatio = 0.9f;
                allowDoubles = true;
                break;
        }

        System.Random rng = new System.Random(42);
        float lastTime = -1f;

        foreach (var onset in all)
        {
            if (rng.NextDouble() > keepRatio) continue;
            if (!allowDoubles && Mathf.Abs(onset.time - lastTime) < 0.01f) continue;
            if (onset.time - lastTime < minGap && Mathf.Abs(onset.time - lastTime) > 0.01f) continue;
            if (onset.time < 2f) continue;

            notes.Add(new SongDatabase.NoteData(onset.time, onset.lane));
            lastTime = onset.time;
        }

        return notes;
    }
}

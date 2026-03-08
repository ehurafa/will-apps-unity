using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Offline FFT-based beat detection.
/// Analyzes an AudioClip's raw samples to detect onsets (beats) and maps them to lanes.
/// Uses energy-based onset detection with 4 frequency bands for lane assignment.
/// </summary>
public static class BeatDetector
{
    // Analysis parameters
    private const int WINDOW_SIZE = 1024;        // FFT window (samples)
    private const int HOP_SIZE = 512;            // Overlap (half window)
    private const int HISTORY_SIZE = 43;         // ~1 second of history at 44100Hz / HOP_SIZE
    private const float ONSET_THRESHOLD = 1.4f;  // Energy must be 1.4x above average
    private const float MIN_ONSET_GAP = 0.12f;   // Minimum seconds between onsets

    // Frequency band boundaries (for 4 lanes)
    // Band 0 (low bass): 0-150 Hz       → Lane 0
    // Band 1 (mid bass): 150-400 Hz     → Lane 1
    // Band 2 (mid):      400-2000 Hz    → Lane 2
    // Band 3 (high):     2000-8000 Hz   → Lane 3

    /// <summary>
    /// Analyze an AudioClip and return a list of NoteData representing detected beats.
    /// </summary>
    public static List<SongDatabase.NoteData> DetectBeats(AudioClip clip, int difficulty)
    {
        if (clip == null) return new List<SongDatabase.NoteData>();

        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int totalSamples = clip.samples;

        // Get all samples (mono mix)
        float[] rawSamples = new float[totalSamples * channels];
        clip.GetData(rawSamples, 0);

        // Mix to mono
        float[] samples = MixToMono(rawSamples, totalSamples, channels);

        // Detect onsets in 4 frequency bands
        List<float>[] bandOnsets = new List<float>[4];
        for (int b = 0; b < 4; b++) bandOnsets[b] = new List<float>();

        DetectBandOnsets(samples, sampleRate, bandOnsets);

        // Build beatmap from onsets based on difficulty
        List<SongDatabase.NoteData> notes = BuildBeatmap(bandOnsets, difficulty);

        Debug.Log($"BeatDetector: {clip.name} → {notes.Count} notes detected (difficulty {difficulty})");
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
                {
                    sum += raw[i * channels + c];
                }
                mono[i] = sum / channels;
            }
        }
        return mono;
    }

    private static void DetectBandOnsets(float[] samples, int sampleRate, List<float>[] bandOnsets)
    {
        int totalSamples = samples.Length;
        int numWindows = (totalSamples - WINDOW_SIZE) / HOP_SIZE;

        // Frequency band bin ranges
        float binResolution = (float)sampleRate / WINDOW_SIZE;
        int[] bandLimits = new int[]
        {
            (int)(150f / binResolution),    // End of band 0
            (int)(400f / binResolution),    // End of band 1
            (int)(2000f / binResolution),   // End of band 2
            (int)(8000f / binResolution)    // End of band 3
        };

        // Energy history per band
        float[][] energyHistory = new float[4][];
        int[] historyIdx = new int[4];
        for (int b = 0; b < 4; b++)
        {
            energyHistory[b] = new float[HISTORY_SIZE];
            historyIdx[b] = 0;
        }

        float[] lastOnsetTime = new float[4];
        for (int b = 0; b < 4; b++) lastOnsetTime[b] = -1f;

        // FFT buffer
        float[] window = new float[WINDOW_SIZE];
        float[] spectrum = new float[WINDOW_SIZE / 2]; // Magnitude spectrum

        for (int w = 0; w < numWindows; w++)
        {
            int offset = w * HOP_SIZE;
            float time = (float)offset / sampleRate;

            // Copy window with Hann windowing
            for (int i = 0; i < WINDOW_SIZE; i++)
            {
                float hannCoeff = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (WINDOW_SIZE - 1)));
                window[i] = samples[offset + i] * hannCoeff;
            }

            // Compute magnitude spectrum (simplified DFT for key bins)
            ComputeSpectrum(window, spectrum);

            // Calculate energy per band
            float[] bandEnergy = new float[4];
            int prevLimit = 0;
            for (int b = 0; b < 4; b++)
            {
                int endBin = Mathf.Min(bandLimits[b], spectrum.Length);
                float energy = 0;
                int count = 0;
                for (int bin = prevLimit; bin < endBin; bin++)
                {
                    energy += spectrum[bin] * spectrum[bin];
                    count++;
                }
                bandEnergy[b] = count > 0 ? energy / count : 0;
                prevLimit = endBin;
            }

            // Onset detection per band
            for (int b = 0; b < 4; b++)
            {
                // Calculate average energy from history
                float avgEnergy = 0;
                int histCount = Mathf.Min(w, HISTORY_SIZE);
                for (int h = 0; h < histCount; h++)
                {
                    avgEnergy += energyHistory[b][h];
                }
                if (histCount > 0) avgEnergy /= histCount;

                // Check for onset
                if (bandEnergy[b] > avgEnergy * ONSET_THRESHOLD && bandEnergy[b] > 0.0001f)
                {
                    if (time - lastOnsetTime[b] >= MIN_ONSET_GAP)
                    {
                        bandOnsets[b].Add(time);
                        lastOnsetTime[b] = time;
                    }
                }

                // Update history
                energyHistory[b][historyIdx[b] % HISTORY_SIZE] = bandEnergy[b];
                historyIdx[b]++;
            }
        }
    }

    /// <summary>
    /// Simplified spectrum computation using energy in frequency bins.
    /// Not a full FFT but good enough for onset detection.
    /// Uses Goertzel's algorithm for targeted frequency bins.
    /// </summary>
    private static void ComputeSpectrum(float[] window, float[] spectrum)
    {
        int N = window.Length;
        int halfN = spectrum.Length;

        for (int k = 0; k < halfN; k++)
        {
            // Only compute every 4th bin for performance (still enough resolution)
            if (k % 4 != 0 && k > 20)
            {
                spectrum[k] = spectrum[k - 1]; // Interpolate from previous
                continue;
            }

            float real = 0, imag = 0;
            float angle = 2f * Mathf.PI * k / N;
            for (int n = 0; n < N; n++)
            {
                real += window[n] * Mathf.Cos(angle * n);
                imag -= window[n] * Mathf.Sin(angle * n);
            }
            spectrum[k] = Mathf.Sqrt(real * real + imag * imag) / N;
        }
    }

    /// <summary>
    /// Build a beatmap from band onsets, filtered by difficulty.
    /// </summary>
    private static List<SongDatabase.NoteData> BuildBeatmap(List<float>[] bandOnsets, int difficulty)
    {
        List<SongDatabase.NoteData> notes = new List<SongDatabase.NoteData>();

        // Merge all onsets with lane info
        List<(float time, int lane)> allOnsets = new List<(float, int)>();
        for (int b = 0; b < 4; b++)
        {
            foreach (float t in bandOnsets[b])
            {
                allOnsets.Add((t, b));
            }
        }

        // Sort by time
        allOnsets.Sort((a, b) => a.time.CompareTo(b.time));

        // Filter based on difficulty
        float minGap;
        float keepRatio;
        bool allowDoubles;

        switch (difficulty)
        {
            case 0: // Easy — keep ~30% of onsets, wider gap, no doubles
                minGap = 0.4f;
                keepRatio = 0.3f;
                allowDoubles = false;
                break;
            case 1: // Medium — keep ~60%, moderate gap, occasional doubles
                minGap = 0.2f;
                keepRatio = 0.6f;
                allowDoubles = true;
                break;
            default: // Hard — keep ~90%, tight gap, doubles allowed
                minGap = 0.1f;
                keepRatio = 0.9f;
                allowDoubles = true;
                break;
        }

        // Use deterministic selection
        System.Random rng = new System.Random(42);
        float lastTime = -1f;

        foreach (var onset in allOnsets)
        {
            // Skip based on keepRatio
            if (rng.NextDouble() > keepRatio) continue;

            // Check gap (allow doubles at same time)
            if (!allowDoubles && Mathf.Abs(onset.time - lastTime) < 0.01f) continue;
            if (onset.time - lastTime < minGap && Mathf.Abs(onset.time - lastTime) > 0.01f) continue;

            // Skip notes in the first 2 seconds (let the song start)
            if (onset.time < 2f) continue;

            notes.Add(new SongDatabase.NoteData(onset.time, onset.lane));
            lastTime = onset.time;
        }

        return notes;
    }
}

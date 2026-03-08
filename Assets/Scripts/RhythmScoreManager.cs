using UnityEngine;

/// <summary>
/// Manages rhythm game scoring: timing detection, combo, multiplier, accuracy, rank.
/// </summary>
public class RhythmScoreManager
{
    // Timing windows (in seconds)
    public const float PERFECT_WINDOW = 0.05f;  // ±50ms
    public const float GREAT_WINDOW = 0.10f;    // ±100ms
    public const float GOOD_WINDOW = 0.15f;     // ±150ms

    public enum HitQuality { Perfect, Great, Good, Miss }
    public enum Rank { S, A, B, C, D }

    // Score values
    private const int PERFECT_SCORE = 300;
    private const int GREAT_SCORE = 200;
    private const int GOOD_SCORE = 100;

    // State
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int Multiplier { get; private set; }

    public int PerfectCount { get; private set; }
    public int GreatCount { get; private set; }
    public int GoodCount { get; private set; }
    public int MissCount { get; private set; }

    public int TotalNotes { get; private set; }

    public RhythmScoreManager()
    {
        Reset();
    }

    public void Reset()
    {
        Score = 0;
        Combo = 0;
        MaxCombo = 0;
        Multiplier = 1;
        PerfectCount = 0;
        GreatCount = 0;
        GoodCount = 0;
        MissCount = 0;
        TotalNotes = 0;
    }

    /// <summary>
    /// Evaluate a note hit based on timing difference.
    /// Returns the quality and updates score/combo.
    /// </summary>
    public HitQuality EvaluateHit(float timeDiff)
    {
        float absDiff = Mathf.Abs(timeDiff);
        TotalNotes++;

        HitQuality quality;
        int baseScore;

        if (absDiff <= PERFECT_WINDOW)
        {
            quality = HitQuality.Perfect;
            baseScore = PERFECT_SCORE;
            PerfectCount++;
        }
        else if (absDiff <= GREAT_WINDOW)
        {
            quality = HitQuality.Great;
            baseScore = GREAT_SCORE;
            GreatCount++;
        }
        else if (absDiff <= GOOD_WINDOW)
        {
            quality = HitQuality.Good;
            baseScore = GOOD_SCORE;
            GoodCount++;
        }
        else
        {
            return RegisterMiss();
        }

        // Hit! Update combo
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;

        // Update multiplier based on combo
        if (Combo >= 40) Multiplier = 8;
        else if (Combo >= 20) Multiplier = 4;
        else if (Combo >= 10) Multiplier = 2;
        else Multiplier = 1;

        Score += baseScore * Multiplier;
        return quality;
    }

    /// <summary>
    /// Register a missed note (not hit in time).
    /// </summary>
    public HitQuality RegisterMiss()
    {
        TotalNotes++;
        MissCount++;
        Combo = 0;
        Multiplier = 1;
        return HitQuality.Miss;
    }

    /// <summary>
    /// Calculate accuracy percentage (0-100).
    /// </summary>
    public float GetAccuracy()
    {
        if (TotalNotes == 0) return 100f;
        float weighted = (PerfectCount * 1f + GreatCount * 0.75f + GoodCount * 0.5f);
        return (weighted / TotalNotes) * 100f;
    }

    /// <summary>
    /// Get final rank based on accuracy.
    /// </summary>
    public Rank GetRank()
    {
        float acc = GetAccuracy();
        if (acc >= 95f) return Rank.S;
        if (acc >= 85f) return Rank.A;
        if (acc >= 70f) return Rank.B;
        if (acc >= 50f) return Rank.C;
        return Rank.D;
    }

    public string GetRankString()
    {
        return GetRank().ToString();
    }

    public Color GetRankColor()
    {
        switch (GetRank())
        {
            case Rank.S: return new Color(1f, 0.843f, 0f, 1f);      // Gold
            case Rank.A: return new Color(0.306f, 0.804f, 0.769f, 1f); // Teal
            case Rank.B: return new Color(0.29f, 0.565f, 0.886f, 1f);  // Blue
            case Rank.C: return new Color(1f, 0.42f, 0.42f, 1f);     // Red
            default: return new Color(0.5f, 0.5f, 0.5f, 1f);          // Gray
        }
    }
}

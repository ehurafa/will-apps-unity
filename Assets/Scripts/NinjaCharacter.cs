using UnityEngine;

/// <summary>
/// Data model for Flappy Ninja characters.
/// Each character has unique physics properties and visual color.
/// </summary>
[System.Serializable]
public struct NinjaCharacter
{
    public string id;
    public string name;
    public Color color;
    public float jumpForce;
    public float gravityScale;

    public static NinjaCharacter Naruto => new NinjaCharacter
    {
        id = "naruto",
        name = "Naruto",
        color = new Color(1f, 0.42f, 0.208f, 1f), // #FF6B35
        jumpForce = 6.5f,
        gravityScale = 2.3f
    };

    public static NinjaCharacter Sasuke => new NinjaCharacter
    {
        id = "sasuke",
        name = "Sasuke",
        color = new Color(0.29f, 0.565f, 0.886f, 1f), // #4A90E2
        jumpForce = 6.0f,
        gravityScale = 2.2f
    };

    public static NinjaCharacter Sakura => new NinjaCharacter
    {
        id = "sakura",
        name = "Sakura",
        color = new Color(0.863f, 0.078f, 0.235f, 1f), // #DC143C
        jumpForce = 6.8f,
        gravityScale = 2.5f
    };

    public static NinjaCharacter[] All => new[] { Naruto, Sasuke, Sakura };
}

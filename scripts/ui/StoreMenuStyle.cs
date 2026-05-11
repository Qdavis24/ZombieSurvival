using Godot;

public static class StoreMenuStyle
{
    public const int OptionButtonFontSize = 34;
    public static readonly Vector2 OptionButtonMinimumSize = new(560f, 60f);

    private const string StoreFontPath = "res://scenes/ui/BoldsPixels.ttf";
    private static FontFile _storeFont;

    public static void ApplyOptionButtonStyle(Button button)
    {
        if (button == null)
            return;

        _storeFont ??= GD.Load<FontFile>(StoreFontPath);
        button.CustomMinimumSize = OptionButtonMinimumSize;
        button.AddThemeFontOverride("font", _storeFont);
        button.AddThemeFontSizeOverride("font_size", OptionButtonFontSize);
    }
}

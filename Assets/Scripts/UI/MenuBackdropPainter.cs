using UnityEngine;

/// <summary>
/// Ana menünün hareketli arka planı (IMGUI): çapraz yavaşça kayan iki renkli karo deseni ve üstünde süzülen soluk harfler.
/// Harflerin yeri <see cref="FloatingLetters"/>'tan gelir; renk ve hızlar <see cref="GameConfig"/>'te.
/// </summary>
public class MenuBackdropPainter
{
    readonly GameConfig config;
    GUIStyle letterStyle;
    readonly GUIContent content = new GUIContent();

    public MenuBackdropPainter(GameConfig config)
    {
        this.config = config;
    }

    /// <summary>Tüm ekrana çizer (yalnızca Repaint olayında; ekran pikseli, matris birim olmalı).</summary>
    public void Draw(float time)
    {
        if (Event.current.type != EventType.Repaint) return;

        float width = Screen.width, height = Screen.height;
        float unit = height / 1080f;
        UiDraw.Fill(new Rect(0, 0, width, height), config.uiBackdropColorA);
        DrawTiles(time, width, height, unit);
        DrawLetters(time, width, height, unit);
    }

    void DrawTiles(float time, float width, float height, float unit)
    {
        float tile = Mathf.Max(8f, config.uiBackdropTileSize * unit);
        float shift = (time * config.uiBackdropScrollSpeed * unit) % (tile * 2f);
        int columns = Mathf.CeilToInt(width / tile) + 2;
        int rows = Mathf.CeilToInt(height / tile) + 2;

        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                if (((x + y) & 1) != 0) continue; // Dama: yalnızca bir renk çizilir, öteki zemin
                UiDraw.Fill(new Rect((x - 2) * tile + shift, (y - 2) * tile + shift, tile, tile), config.uiBackdropColorB);
            }
    }

    void DrawLetters(float time, float width, float height, float unit)
    {
        if (letterStyle == null)
            letterStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Get(), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };

        Color baseColor = config.uiBackdropLetterColor;
        Matrix4x4 old = GUI.matrix;
        for (int i = 0; i < config.uiBackdropLetterCount; i++)
        {
            FloatingLetter letter = FloatingLetters.Get(i, time, width, height, config.uiBackdropLetterSpeed * unit,
                config.uiBackdropLetterMinSize * unit, config.uiBackdropLetterMaxSize * unit, config.uiBackdropLetterAlpha);

            letterStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(letter.Size));
            letterStyle.normal.textColor = new Color(baseColor.r, baseColor.g, baseColor.b, letter.Alpha);
            content.text = letter.Letter.ToString();
            Rect rect = new Rect(letter.X - letter.Size, letter.Y - letter.Size, letter.Size * 2f, letter.Size * 2f);

            GUI.matrix = old;
            GUIUtility.RotateAroundPivot(letter.Angle, rect.center);
            GUI.Label(rect, content, letterStyle);
        }
        GUI.matrix = old;
    }
}

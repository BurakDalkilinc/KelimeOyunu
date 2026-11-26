using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Koleksiyon defterinin tam ekran görünümü (IMGUI): sekmeler, kart ızgarası, sayfa düğmeleri, sağda seçili kartın ayrıntısı.
/// Kurallar ve yazılar düz sınıflardadır (<see cref="CollectionBook"/>, <see cref="CollectionText"/>); burası yalnızca çizim ve tıklama.
/// Resimler Resources/Sprites'tan gelir; bulunamazsa düz renk kutular çizilir.
/// </summary>
public class CollectionBookScreen : MonoBehaviour
{
    const string PageSprite = "ui_book_page";
    const string FrameSprite = "ui_card_frame";
    const string FrameSeenSprite = "ui_card_frame_seen";
    const string FrameLockedSprite = "ui_card_frame_locked_plain";
    const string LockIconSprite = "ui_icon_lock";
    const string BookIconSprite = "ui_icon_book";
    const string ScreenTitle = "Koleksiyon Defteri";

    static CollectionBookScreen instance;

    /// <summary>Defter açık mı? Açıkken menüler kendilerini çizmez.</summary>
    public static bool IsOpen => instance != null && instance.isActiveAndEnabled;

    /// <summary>Defteri açar (yoksa oluşturur).</summary>
    public static void Open()
    {
        if (instance == null) instance = new GameObject("CollectionBookScreen").AddComponent<CollectionBookScreen>();
        instance.gameObject.SetActive(true);
        instance.Refresh();
    }

    /// <summary>Defteri kapatır.</summary>
    public static void Close()
    {
        if (instance != null) instance.gameObject.SetActive(false);
    }

    GameConfig config;
    CollectionBook book;
    TagNames tags;
    CollectionTab tab = CollectionTab.All;
    List<WordEntry> filtered = new List<WordEntry>();
    int page;
    WordEntry selected;
    readonly int[] tabCounts = new int[CollectionTabs.All.Length];

    GUIStyle pageStyle, frameStyle, frameSeenStyle, frameLockedStyle;
    GUIStyle titleStyle, textStyle, rightStyle, smallStyle, centerStyle, smallCenterStyle, lockedCenterStyle, buttonStyle;
    bool stylesReady;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Refresh()
    {
        config = CollectionService.Config;
        book = CollectionService.Book;
        tags = CollectionService.Tags;
        filtered = book.Filter(tab);
        for (int i = 0; i < CollectionTabs.All.Length; i++) tabCounts[i] = book.Filter(CollectionTabs.All[i]).Count;
        page = CollectionPaging.ClampPage(page, filtered.Count, config.collectionCardsPerPage);
    }

    void SelectTab(CollectionTab next)
    {
        tab = next;
        page = 0;
        Refresh();
    }

    void ChangePage(int delta)
    {
        page = CollectionPaging.ClampPage(page + delta, filtered.Count, config.collectionCardsPerPage);
    }

    // ---- Çizim ----

    void OnGUI()
    {
        if (book == null) Refresh();
        EnsureStyles();
        HandleKeys();

        GUI.depth = -100; // Diğer IMGUI menülerinin üstünde
        float scale = Screen.height / config.collectionReferenceHeight;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = config.collectionReferenceHeight;

        DrawColor(new Rect(0, 0, width, height), config.collectionBackdropColor);

        float m = config.collectionMargin;
        Rect pageRect = new Rect(m, m, width - 2 * m, height - 2 * m);
        DrawFrame(pageRect, pageStyle, config.collectionPageColor);

        float p = config.collectionPadding;
        Rect inner = new Rect(pageRect.x + p, pageRect.y + p, pageRect.width - 2 * p, pageRect.height - 2 * p);
        float detailWidth = Mathf.Min(config.collectionDetailWidth, inner.width * 0.5f);
        float gap = config.collectionCardSpacing;

        Rect header = new Rect(inner.x, inner.y, inner.width, config.collectionHeaderHeight);
        Rect footer = new Rect(inner.x, inner.yMax - config.collectionFooterHeight, inner.width - detailWidth - gap, config.collectionFooterHeight);
        Rect grid = new Rect(inner.x, header.yMax + gap, inner.width - detailWidth - gap, footer.y - header.yMax - 2 * gap);
        Rect detail = new Rect(grid.xMax + gap, header.yMax + gap, detailWidth, inner.yMax - header.yMax - gap);

        DrawHeader(header);
        DrawGrid(grid);
        DrawFooter(footer);
        DrawDetail(detail);

        GUI.matrix = oldMatrix;
    }

    void HandleKeys()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { Close(); e.Use(); }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftArrow) { ChangePage(-1); e.Use(); }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.RightArrow) { ChangePage(1); e.Use(); }
        else if (e.type == EventType.ScrollWheel) { ChangePage(e.delta.y > 0 ? 1 : -1); e.Use(); }
    }

    void DrawHeader(Rect area)
    {
        float icon = config.collectionIconSize;
        Sprite bookIcon = SpriteLibrary.Get(BookIconSprite);
        float x = area.x;
        if (bookIcon != null)
        {
            DrawSprite(new Rect(x, area.y, icon, icon), bookIcon, 1f);
            x += icon + config.collectionCardSpacing;
        }

        float lineHeight = area.height * 0.5f;
        GUI.Label(new Rect(x, area.y, area.width * 0.5f, lineHeight), ScreenTitle, titleStyle);

        GUI.Label(new Rect(area.x, area.y, area.width, lineHeight), CollectionText.Progress(book), rightStyle);

        // Sekmeler
        float tabWidth = (area.width - config.collectionCardSpacing * (CollectionTabs.All.Length - 1)) / CollectionTabs.All.Length;
        for (int i = 0; i < CollectionTabs.All.Length; i++)
        {
            CollectionTab each = CollectionTabs.All[i];
            Rect r = new Rect(area.x + i * (tabWidth + config.collectionCardSpacing), area.y + lineHeight, tabWidth, lineHeight - config.collectionCardSpacing * 0.5f);
            bool on = GUI.Toggle(r, tab == each, $"{CollectionTabs.Title(each)} ({tabCounts[i]})", buttonStyle);
            if (on && tab != each) SelectTab(each);
        }
    }

    void DrawGrid(Rect area)
    {
        int perPage = config.collectionCardsPerPage;
        int columns = Mathf.Max(1, config.collectionColumns);
        int rows = Mathf.Max(1, (perPage + columns - 1) / columns);
        float gap = config.collectionCardSpacing;
        float cellWidth = (area.width - gap * (columns - 1)) / columns;
        float cellHeight = (area.height - gap * (rows - 1)) / rows;

        // Kart en/boy oranı korunur; hücreye sığan en büyük kart ortalanır.
        float cardHeight = Mathf.Min(cellHeight, cellWidth / config.collectionCardAspect);
        float cardWidth = cardHeight * config.collectionCardAspect;

        List<WordEntry> cards = CollectionPaging.Slice(filtered, page, perPage);
        for (int i = 0; i < cards.Count; i++)
        {
            int col = i % columns, row = i / columns;
            float cx = area.x + col * (cellWidth + gap) + (cellWidth - cardWidth) * 0.5f;
            float cy = area.y + row * (cellHeight + gap) + (cellHeight - cardHeight) * 0.5f;
            DrawCard(new Rect(cx, cy, cardWidth, cardHeight), cards[i]);
        }

        if (cards.Count == 0) GUI.Label(area, "Bu sekmede kelime yok.", centerStyle);
    }

    void DrawCard(Rect rect, WordEntry word)
    {
        CollectionStatus status = book.GetStatus(word.id);

        if (word == selected) DrawOutline(rect, config.collectionSelectedColor, config.collectionSelectedOutline);

        switch (status)
        {
            case CollectionStatus.Discovered: DrawFrame(rect, frameStyle, config.collectionDiscoveredColor); break;
            case CollectionStatus.Seen: DrawFrame(rect, frameSeenStyle, config.collectionSeenColor); break;
            default: DrawFrame(rect, frameLockedStyle, config.collectionLockedColor); break;
        }

        // Çerçevenin iç alanı (kenar kalınlığı kadar içeride)
        float inset = config.collectionFrameBorder * config.collectionFramePixelScale;
        Rect content = new Rect(rect.x + inset, rect.y + inset, rect.width - 2 * inset, rect.height - 2 * inset);
        Rect imageRect = new Rect(content.x, content.y, content.width, content.height * config.collectionCardImageRatio);
        Rect textRect = new Rect(content.x, imageRect.yMax, content.width, content.height - imageRect.height);

        if (status == CollectionStatus.Locked)
        {
            Sprite lockIcon = SpriteLibrary.Get(LockIconSprite);
            if (lockIcon != null) DrawSprite(Shrink(imageRect, 0.5f), lockIcon, 1f);
            else GUI.Label(imageRect, "?", titleStyle);

            Rect nameRect = new Rect(textRect.x, textRect.y, textRect.width, textRect.height * 0.5f);
            Rect hintRect = new Rect(textRect.x, nameRect.yMax, textRect.width, textRect.height * 0.5f);
            GUI.Label(nameRect, CollectionText.LockedName, lockedCenterStyle);
            GUI.Label(hintRect, CollectionText.LetterHint(word), lockedCenterStyle);
        }
        else
        {
            float alpha = status == CollectionStatus.Seen ? config.collectionSeenAlpha : 1f;
            Sprite art = SpriteLibrary.Get(SpriteLibrary.NameFor(word));
            if (art != null) DrawSprite(imageRect, art, alpha);
            GUI.Label(textRect, word.display, smallCenterStyle);
        }

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) selected = word;
    }

    void DrawFooter(Rect area)
    {
        int pageCount = CollectionPaging.PageCount(filtered.Count, config.collectionCardsPerPage);
        float gap = config.collectionCardSpacing;
        float buttonWidth = (area.width - 3 * gap) / 4f;

        GUI.enabled = page > 0;
        if (GUI.Button(new Rect(area.x, area.y, buttonWidth, area.height), "< Önceki", buttonStyle)) ChangePage(-1);
        GUI.enabled = true;

        GUI.Label(new Rect(area.x + buttonWidth + gap, area.y, buttonWidth, area.height), CollectionText.PageLabel(page, pageCount), centerStyle);

        GUI.enabled = page < pageCount - 1;
        if (GUI.Button(new Rect(area.x + 2 * (buttonWidth + gap), area.y, buttonWidth, area.height), "Sonraki >", buttonStyle)) ChangePage(1);
        GUI.enabled = true;

        if (GUI.Button(new Rect(area.x + 3 * (buttonWidth + gap), area.y, buttonWidth, area.height), "Kapat (Esc)", buttonStyle)) Close();
    }

    void DrawDetail(Rect area)
    {
        DrawFrame(area, frameStyle, config.collectionDiscoveredColor);
        float inset = config.collectionFrameBorder * config.collectionFramePixelScale + config.collectionCardSpacing;
        Rect inner = new Rect(area.x + inset, area.y + inset, area.width - 2 * inset, area.height - 2 * inset);

        if (selected == null)
        {
            GUI.Label(inner, "Ayrıntı için bir karta tıkla.", centerStyle);
            return;
        }

        CollectionStatus status = book.GetStatus(selected.id);
        bool discovered = status == CollectionStatus.Discovered;
        float imageSize = Mathf.Min(inner.width, inner.height * 0.4f);
        Rect imageRect = new Rect(inner.x + (inner.width - imageSize) * 0.5f, inner.y, imageSize, imageSize);

        // Önce resim (kilitliyse kilit simgesi), sonra altında yazılar.
        if (status == CollectionStatus.Locked)
        {
            Sprite lockIcon = SpriteLibrary.Get(LockIconSprite);
            if (lockIcon != null) DrawSprite(Shrink(imageRect, 0.5f), lockIcon, 1f);
        }
        else
        {
            Sprite art = SpriteLibrary.Get(SpriteLibrary.NameFor(selected));
            if (art != null) DrawSprite(imageRect, art, discovered ? 1f : config.collectionSeenAlpha);
        }

        float textTop = imageRect.yMax + config.collectionCardSpacing;
        GUILayout.BeginArea(new Rect(inner.x, textTop, inner.width, inner.yMax - textTop));
        if (status == CollectionStatus.Locked)
        {
            GUILayout.Label(CollectionText.LockedName, titleStyle);
            GUILayout.Label(CollectionText.LetterHint(selected), textStyle);
            GUILayout.Label(CollectionText.KindAndLength(selected), textStyle);
            GUILayout.Label("Bir maçta bu kelimeyi yaz ya da yazılırken gör.", smallStyle);
        }
        else
        {
            GUILayout.Label(selected.display, titleStyle);
            GUILayout.Label(CollectionTabs.KindName(selected), smallStyle);
            if (discovered)
            {
                GUILayout.Label(selected.description, textStyle);
                string tagText = tags.Describe(selected);
                if (tagText.Length > 0) GUILayout.Label("Özellikler: " + tagText, smallStyle);
            }
            else
            {
                GUILayout.Label(CollectionText.SeenOnlyText, textStyle);
            }
            GUILayout.Label(CollectionText.Counts(book.TimesWritten(selected.id), book.TimesSeen(selected.id)), smallStyle);
        }
        GUILayout.EndArea();
    }

    // ---- Yardımcılar ----

    /// <summary>Çerçeve resmini 9 dilim olarak çizer (kenarlar gerilmez); resim yoksa düz renk kutu.</summary>
    void DrawFrame(Rect rect, GUIStyle style, Color fallback)
    {
        if (style == null) { DrawColor(rect, fallback); return; }
        if (Event.current.type != EventType.Repaint) return;

        // Resmin bir pikseli ekranda FramePixelScale kadar olsun diye çizimi o ölçekte yaparız.
        float s = config.collectionFramePixelScale;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.Scale(new Vector3(s, s, 1f));
        style.Draw(new Rect(rect.x / s, rect.y / s, rect.width / s, rect.height / s), false, false, false, false);
        GUI.matrix = old;
    }

    static void DrawColor(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void DrawOutline(Rect rect, Color color, float thickness)
    {
        DrawColor(new Rect(rect.x - thickness, rect.y - thickness, rect.width + 2 * thickness, rect.height + 2 * thickness), color);
    }

    /// <summary>Sprite'ı kutuya en/boy oranını koruyarak sığdırır.</summary>
    static void DrawSprite(Rect box, Sprite sprite, float alpha)
    {
        Texture2D texture = sprite.texture;
        Rect tr = sprite.textureRect;
        float aspect = tr.width / tr.height;
        float w = box.width, h = box.width / aspect;
        if (h > box.height) { h = box.height; w = h * aspect; }
        Rect fitted = new Rect(box.x + (box.width - w) * 0.5f, box.y + (box.height - h) * 0.5f, w, h);
        Rect uv = new Rect(tr.x / texture.width, tr.y / texture.height, tr.width / texture.width, tr.height / texture.height);

        Color old = GUI.color;
        GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
        GUI.DrawTextureWithTexCoords(fitted, texture, uv);
        GUI.color = old;
    }

    static Rect Shrink(Rect rect, float factor)
    {
        float w = rect.width * factor, h = rect.height * factor;
        return new Rect(rect.x + (rect.width - w) * 0.5f, rect.y + (rect.height - h) * 0.5f, w, h);
    }

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        pageStyle = NineSlice(PageSprite, config.collectionPageBorder);
        frameStyle = NineSlice(FrameSprite, config.collectionFrameBorder);
        frameSeenStyle = NineSlice(FrameSeenSprite, config.collectionFrameBorder);
        frameLockedStyle = NineSlice(FrameLockedSprite, config.collectionFrameBorder);

        Font font = GameFont.Get();
        Color ink = config.collectionTextColor;
        textStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = config.collectionFontSize, wordWrap = true };
        textStyle.normal.textColor = ink;
        rightStyle = new GUIStyle(textStyle) { alignment = TextAnchor.UpperRight };
        titleStyle = new GUIStyle(textStyle) { fontSize = config.collectionTitleFontSize, wordWrap = false };
        smallStyle = new GUIStyle(textStyle) { fontSize = config.collectionSmallFontSize };
        centerStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
        smallCenterStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
        lockedCenterStyle = new GUIStyle(smallCenterStyle);
        lockedCenterStyle.normal.textColor = config.collectionLockedTextColor;
        buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = config.collectionFontSize };
    }

    /// <summary>Resources/Sprites içindeki resimden 9 dilimli stil; resim yoksa null (düz renge düşülür).</summary>
    static GUIStyle NineSlice(string spriteName, int border)
    {
        Sprite sprite = SpriteLibrary.Get(spriteName);
        if (sprite == null) return null;
        var style = new GUIStyle { border = new RectOffset(border, border, border, border) };
        style.normal.background = sprite.texture;
        return style;
    }
}

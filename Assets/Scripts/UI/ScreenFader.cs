using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahne geçişi: ekran piksel kareler hâlinde kararır, sahne yüklenir, sonra kareler açılır (<see cref="PixelDissolve"/>).
/// Tek örnek <see cref="GameServices"/> tarafından kurulur ve sahneler arasında kalır. Oyun açılırken de karanlıktan açılır.
/// Örnek yoksa (ör. testte) sahne beklemeden yüklenir. Süre ve renk <see cref="GameConfig"/>'te.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    enum State { Idle, Covering, Revealing }

    static ScreenFader instance;

    GameConfig config;
    State state;
    float progress; // 0 = ekran açık, 1 = tamamen kapalı
    Action pending;
    bool skipFrame;

    /// <summary>Geçiş sürüyor mu (bu sırada yeni geçiş istenmez)?</summary>
    public static bool IsBusy => instance != null && instance.state == State.Covering;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    /// <summary>Ekranı karartıp verilen sahneyi (yol ya da ad) yükler, sonra açar.</summary>
    public static void LoadScene(string scene) => Run(() => SceneManager.LoadScene(scene));

    /// <summary>Ekranı karartıp verilen sıradaki sahneyi yükler, sonra açar.</summary>
    public static void LoadScene(int buildIndex) => Run(() => SceneManager.LoadScene(buildIndex));

    /// <summary>Ekranı karartır, işi yapar (ör. sahne yükleme), sonra açar. Geçiş zaten sürüyorsa istek yok sayılır.</summary>
    public static void Run(Action action)
    {
        if (action == null) return;
        if (instance == null) { action(); return; }
        if (instance.state == State.Covering) return;

        instance.pending = action;
        instance.state = State.Covering;
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        config = GameConfigLoader.Get();

        // Oyun açılışı: karanlıktan açıl.
        progress = 1f;
        state = State.Revealing;
        skipFrame = true;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        float duration = Mathf.Max(0.0001f, config.uiFadeSeconds);
        float step = Time.unscaledDeltaTime / duration;

        switch (state)
        {
            case State.Covering:
                progress = Mathf.Min(1f, progress + step);
                if (progress < 1f) return;
                Action action = pending;
                pending = null;
                state = State.Revealing;
                skipFrame = true; // Yükleme karesinin uzun süresi açılmayı atlatmasın
                action?.Invoke();
                return;

            case State.Revealing:
                if (skipFrame) { skipFrame = false; return; }
                progress = Mathf.Max(0f, progress - step);
                if (progress <= 0f) state = State.Idle;
                return;
        }
    }

    void OnGUI()
    {
        if (progress <= 0f || Event.current.type != EventType.Repaint) return;
        GUI.depth = -1000; // Her şeyin üstünde

        if (progress >= 1f)
        {
            UiDraw.Fill(new Rect(0, 0, Screen.width, Screen.height), config.uiFadeColor);
            return;
        }

        float cell = Mathf.Max(4f, config.uiFadeCellPixels * Screen.height / 1080f);
        int columns = Mathf.CeilToInt(Screen.width / cell);
        int rows = Mathf.CeilToInt(Screen.height / cell);
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                if (PixelDissolve.IsCovered(x, y, progress))
                    UiDraw.Fill(new Rect(x * cell, y * cell, cell + 1f, cell + 1f), config.uiFadeColor);
    }
}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Deneme paneli (yalnızca Unity Editor'da ve geliştirme sürümünde; oyuncuya giden sürümde yoktur).
/// F1 ile açılır/kapanır. Tek kişilik Arena sahnesinde kendiliğinden eklenir, sahneye elle bir şey koymak gerekmez.
/// - Kelime listesi: aranır, tıklanan kelime sırayı beklemeden "ben yazmış gibi" ya da "bot yazmış gibi" oynatılır (kilitlenmez).
/// - Ölümsüzlük, zaman hızı, botları dondurma, botları yanına toplama, herkesi diriltme, yaratıkları ve kanı temizleme.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    const float Width = 360f;

    MatchRunner runner;
    MatchBootstrap bootstrap;
    bool open;
    string filter = string.Empty;
    Vector2 scroll;
    bool godMode;
    bool botsFrozen;
    float timeScale = 1f;
    string lastMessage = "F1: paneli aç/kapat";
    GUIStyle box, label, button, smallButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => EnsureExists();
        EnsureExists();
    }

    /// <summary>Sahnede maç varsa ve panel yoksa paneli ekler (MatchRunner da başlarken çağırır).</summary>
    public static void EnsureExists()
    {
        if (FindAnyObjectByType<MatchRunner>() == null || FindAnyObjectByType<DebugPanel>() != null) return;
        new GameObject("Deneme paneli (F1)").AddComponent<DebugPanel>();
    }

    void Update()
    {
        if (runner == null) runner = FindAnyObjectByType<MatchRunner>();
        if (bootstrap == null) bootstrap = FindAnyObjectByType<MatchBootstrap>();

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) open = !open;

        PlayerState me = runner != null ? runner.LocalPlayer : null;
        if (godMode && me != null && me.IsAlive)
        {
            // Ölümsüzlük: her kare can dolar ve kalkan tazelenir (lav/uçurum ve araç patlaması yine öldürür).
            me.Health.Heal(me.Health.Max);
            if (!me.Status.HasShield || me.Status.ShieldTimeLeft < 2f) me.Status.ApplyShield(99, 5f);
        }
    }

    void OnGUI()
    {
        if (runner == null || runner.Turns == null) return;
        BuildStyles();

        if (!open)
        {
            GUI.Label(new Rect(Screen.width - 190f, Screen.height - 28f, 180f, 24f), "F1: Deneme paneli", label);
            return;
        }

        var area = new Rect(Screen.width - Width - 10f, 10f, Width, Screen.height - 20f);
        GUI.Box(area, GUIContent.none, box);
        GUILayout.BeginArea(new Rect(area.x + 10f, area.y + 8f, area.width - 20f, area.height - 16f));

        GUILayout.Label("<b>DENEME PANELİ</b>  (F1 kapat)", label);
        GUILayout.Label(lastMessage, label);

        GUILayout.BeginHorizontal();
        godMode = GUILayout.Toggle(godMode, " Ölümsüzlük", button);
        bool freeze = GUILayout.Toggle(botsFrozen, " Botları dondur", button);
        if (freeze != botsFrozen) SetBotsFrozen(freeze);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Zaman x{timeScale:0.##}", label, GUILayout.Width(90f));
        foreach (float scale in new[] { 0.25f, 0.5f, 1f, 2f })
            if (GUILayout.Button($"x{scale:0.##}", smallButton)) SetTime(scale);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Botları yanıma topla", button)) GatherBots();
        if (GUILayout.Button("Herkesi dirilt", button)) ReviveAll();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Yaratıkları sil", button)) ClearCreatures();
        if (GUILayout.Button("Kanı temizle", button)) ClearGore();
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Kelime ara (sol tık: ben yazayım, sağ tık: bot yazsın):", label);
        filter = GUILayout.TextField(filter ?? string.Empty);

        scroll = GUILayout.BeginScrollView(scroll);
        foreach (WordEntry word in FilteredWords())
        {
            string text = $"{word.display}  <color=#9aa>{Category(word)}</color>";
            if (GUILayout.Button(text, smallButton))
            {
                bool asBot = Event.current.button == 1;
                Play(word, asBot);
            }
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    IEnumerable<WordEntry> FilteredWords()
    {
        string key = WordNormalizer.ToMatchKey(filter ?? string.Empty);
        foreach (WordEntry word in runner.Words.Words)
        {
            if (string.IsNullOrEmpty(key)) { yield return word; continue; }
            if (WordNormalizer.ToMatchKey(word.display).Contains(key) || WordNormalizer.ToMatchKey(word.id).Contains(key)) { yield return word; continue; }
            if (word.aliases == null) continue;
            foreach (string alias in word.aliases)
                if (WordNormalizer.ToMatchKey(alias).Contains(key)) { yield return word; break; }
        }
    }

    static string Category(WordEntry word)
    {
        if (word.scope == WordEntry.ScopeGlobal) return "büyük tehdit";
        if (word.category == WordEntry.CategoryDefense) return "kendine";
        return "saldırı";
    }

    void Play(WordEntry word, bool asBot)
    {
        PlayerState writer = asBot ? RandomAliveBot() : runner.LocalPlayer;
        if (writer == null) { lastMessage = asBot ? "Hayatta bot yok." : "Sen elendin: önce 'Herkesi dirilt'."; return; }

        bool ok = runner.Turns.DebugPlay(writer, word);
        lastMessage = ok ? $"{writer.Name} yazdı: {word.display}" : "Maç bitti: R ile yeniden başlat.";
    }

    PlayerState RandomAliveBot()
    {
        var alive = new List<PlayerState>();
        foreach (PlayerState player in runner.Players)
            if (player.IsBot && player.IsAlive) alive.Add(player);
        return alive.Count == 0 ? null : alive[Random.Range(0, alive.Count)];
    }

    void SetBotsFrozen(bool frozen)
    {
        botsFrozen = frozen;
        foreach (BotController bot in FindObjectsByType<BotController>(FindObjectsSortMode.None)) bot.enabled = !frozen;
        lastMessage = frozen ? "Botlar donduruldu." : "Botlar serbest.";
    }

    void SetTime(float scale)
    {
        timeScale = scale;
        Time.timeScale = scale;
        lastMessage = $"Zaman x{scale:0.##}";
    }

    void GatherBots()
    {
        PlayerController me = FindAnyObjectByType<PlayerController>();
        if (me == null) return;

        var bots = new List<BotController>();
        foreach (BotController bot in FindObjectsByType<BotController>(FindObjectsSortMode.None))
            if (bot.State != null && bot.State.IsAlive) bots.Add(bot);

        Vector2 center = me.transform.position;
        for (int i = 0; i < bots.Count; i++)
        {
            float angle = i * Mathf.PI * 2f / Mathf.Max(1, bots.Count);
            Vector2 at = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.5f;
            if (bootstrap != null && bootstrap.Terrain != null) at = bootstrap.Terrain.NearestSafe(at);
            bots[i].transform.position = new Vector3(at.x, at.y, bots[i].transform.position.z);
        }
        lastMessage = $"{bots.Count} bot yanına geldi.";
    }

    void ReviveAll()
    {
        int count = 0;
        foreach (PlayerState player in runner.Players)
            if (!player.IsAlive && player.Health.Revive(player.Health.Max) > 0) count++;
        lastMessage = $"{count} kişi dirildi.";
    }

    void ClearCreatures()
    {
        int count = 0;
        foreach (CreatureView view in FindObjectsByType<CreatureView>(FindObjectsSortMode.None)) { Destroy(view.gameObject); count++; }
        lastMessage = $"{count} yaratık silindi.";
    }

    void ClearGore()
    {
        int count = 0;
        foreach (BloodDrop drop in FindObjectsByType<BloodDrop>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Destroy(drop.gameObject); count++; }
        foreach (GibPiece gib in FindObjectsByType<GibPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Destroy(gib.gameObject); count++; }
        lastMessage = $"{count} kan izi/parça silindi.";
    }

    void BuildStyles()
    {
        if (box != null) return;
        box = new GUIStyle(GUI.skin.box);
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.1f, 0.92f));
        tex.Apply();
        box.normal.background = tex;

        label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
        label.normal.textColor = Color.white;
        button = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        smallButton = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft, richText = true };
    }
}
#endif

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// İnsan oyuncunun saldırı hedefini fareyle seçtirir: üzerine gelinen oyuncunun etrafında halka çıkar,
/// tıklayınca hedef seçilir. Süre dolarsa rastgele bir rakip seçilir. Kimse kendini seçemez.
/// Süre ve doğrulama <see cref="TargetSelection"/>'da, "hangi oyuncu tıklandı" hesabı <see cref="TargetHitTester"/>'dadır;
/// burası yalnızca fare, ekran ve bağlantıdır.
/// </summary>
public class TargetSelector : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchBootstrap bootstrap;
    [SerializeField] Camera worldCamera;
    [SerializeField] Text prompt;
    [SerializeField] Color hoverColor = new Color(1f, 0.9f, 0.3f, 0.5f);

    TargetSelection selection;
    Action<PlayerState> onChosen;
    PressEdge clickEdge;
    Transform hoverRing;
    System.Random random;

    /// <summary>Şu anda bir hedef seçiliyor mu?</summary>
    public bool IsSelecting => selection != null && selection.IsPending;

    void OnEnable()
    {
        if (config == null || bootstrap == null || worldCamera == null || prompt == null)
        {
            Debug.LogError("TargetSelector: config, bootstrap, worldCamera ya da prompt eksik.", this);
            enabled = false;
            return;
        }

        random = new System.Random(bootstrap.Random.Next());
        BuildHoverRing();
        Cleanup();
    }

    void OnDisable()
    {
        Cleanup();
    }

    /// <summary>Hedef seçmeyi başlatır. Seçim bitince <paramref name="chosen"/> hedefle çağrılır (hedef bulunamazsa çağrılmaz).</summary>
    public void Begin(PlayerState writer, Action<PlayerState> chosen)
    {
        // Önceki seçim bitmeden yenisi gelirse, öncekini rastgele bir hedefle tamamla.
        if (IsSelecting) Complete(forceRandom: true);

        selection = new TargetSelection(writer, config.targetSelectionSeconds);
        onChosen = chosen;
    }

    /// <summary>Hedefi doğrudan seçer (fare tıklaması bunu çağırır). Geçersizse false.</summary>
    public bool TryChoose(PlayerState candidate)
    {
        if (!IsSelecting || !selection.TrySelect(candidate)) return false;

        Complete(forceRandom: false);
        return true;
    }

    void Update()
    {
        // Fare tuşunun "yeni basıldı" anı, basılı durumundaki değişimden bulunur (bkz. PressEdge).
        bool clicked = clickEdge.Update(Mouse.current != null && Mouse.current.leftButton.isPressed) && !UiInputBlock.IsBlocked;

        if (selection == null) return;

        // Yazan elendiyse ya da maç bittiyse seçim anlamını yitirir.
        if (!selection.Writer.IsAlive || bootstrap.Match.IsOver)
        {
            selection.Cancel();
            Cleanup();
            return;
        }

        selection.Tick(Time.deltaTime);

        PlayerState hovered = TargetHitTester.FindAt(MouseWorldPosition(), bootstrap, selection.Writer, config.targetClickRadius);
        ShowHover(hovered);

        if (clicked && hovered != null && TryChoose(hovered)) return;

        if (selection.State == TargetSelectionState.TimedOut)
        {
            Complete(forceRandom: true);
            return;
        }

        prompt.text = $"Hedef seç! Bir oyuncuya tıkla ({Mathf.CeilToInt(selection.TimeLeft)} sn)";
    }

    /// <summary>Seçimi bitirir: seçilen (ya da süre dolduysa rastgele bir) hedefle geri çağrıyı yapar.</summary>
    void Complete(bool forceRandom)
    {
        PlayerState target = selection.State == TargetSelectionState.Selected && !forceRandom
            ? selection.Chosen
            : TargetPicker.PickRandomOpponent(bootstrap.Match.Players, selection.Writer, random);

        Action<PlayerState> callback = onChosen;
        Cleanup();

        if (target != null) callback?.Invoke(target);
    }

    void Cleanup()
    {
        selection = null;
        onChosen = null;
        if (prompt != null) prompt.text = string.Empty;
        if (hoverRing != null) hoverRing.gameObject.SetActive(false);
    }

    Vector2 MouseWorldPosition()
    {
        if (Mouse.current == null) return new Vector2(float.MaxValue, float.MaxValue); // Fare yoksa hiçbir şey seçilemez.

        Vector2 screen = Mouse.current.position.ReadValue();
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
        return world;
    }

    void ShowHover(PlayerState hovered)
    {
        if (hoverRing == null) return;

        if (hovered == null || !bootstrap.TryGetPosition(hovered.Id, out Vector2 position))
        {
            hoverRing.gameObject.SetActive(false);
            return;
        }

        hoverRing.gameObject.SetActive(true);
        hoverRing.position = new Vector3(position.x, position.y, 0f);
    }

    void BuildHoverRing()
    {
        var ring = new GameObject("HedefHalkasi");
        ring.transform.SetParent(transform, false);
        ring.transform.localScale = Vector3.one * (config.playerRadius * 2.8f);

        var spriteRenderer = ring.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.Circle();
        spriteRenderer.color = hoverColor;
        spriteRenderer.sortingOrder = 4;

        ring.SetActive(false);
        hoverRing = ring.transform;
    }
}

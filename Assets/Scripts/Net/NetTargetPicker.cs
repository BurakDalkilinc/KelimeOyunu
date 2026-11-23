using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Ağ maçında hedefi fareyle seçtirir: sıra sendeyse ve saldırı kelimesi yazdıysan, üzerine gelinen rakibin etrafında halka çıkar,
/// tıklayınca hedef host'a bildirilir. Seçimin geçerli olup olmadığına host karar verir (kendini, elenmişi, görünmezi seçemezsin).
/// "Hangi oyuncu tıklandı" hesabı <see cref="TargetHitTester"/>'daki mantıkla aynıdır; burası yalnızca fare ve ekrandır.
/// </summary>
public class NetTargetPicker : MonoBehaviour
{
    [SerializeField] NetTurnChannel channel;
    [SerializeField] GameConfig config;
    [SerializeField] Camera worldCamera;

    /// <summary>İsteğe bağlı: "Hedef seç!" yazısı (Arena arayüzündeki TargetPrompt). Boşsa panelin IMGUI yazısı kullanılır.</summary>
    [SerializeField] Text prompt;

    [SerializeField] Color hoverColor = new Color(1f, 0.9f, 0.3f, 0.5f);

    PressEdge clickEdge;
    Transform ring;

    void Awake()
    {
        var go = new GameObject("HedefHalkasi");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.color = hoverColor;
        sr.sortingOrder = 4;
        go.SetActive(false);
        ring = go.transform;
    }

    void Update()
    {
        // Fare tuşunun "yeni basıldı" anı, basılı durumundaki değişimden bulunur (bkz. PressEdge).
        bool clicked = clickEdge.Update(Mouse.current != null && Mouse.current.leftButton.isPressed);

        if (prompt != null) prompt.text = PromptText();

        if (channel == null || config == null || worldCamera == null || !channel.IAmChoosingTarget)
        {
            if (ring != null && ring.gameObject.activeSelf) ring.gameObject.SetActive(false);
            return;
        }

        NetworkPlayer hovered = FindAt(MouseWorldPosition());
        ring.gameObject.SetActive(hovered != null);
        if (hovered == null) return;

        ring.position = hovered.transform.position;
        ring.localScale = Vector3.one * (config.playerRadius * 2.8f);

        if (clicked) channel.ChooseTarget(hovered.PlayerId);
    }

    string PromptText()
    {
        if (channel == null || channel.TargetingWriterId < 0) return string.Empty;
        if (channel.IAmChoosingTarget) return $"Hedef seç! Bir oyuncuya tıkla ({Mathf.CeilToInt(channel.TargetingTimeLeft)} sn)";

        string name = channel.Client.Players.TryGetValue(channel.TargetingWriterId, out PlayerState writer) ? writer.Name : "Biri";
        return name + " hedef seçiyor...";
    }

    /// <summary>Verilen noktaya en yakın, seçilebilir (kendin değil, elenmemiş, görünmez değil) karakter.</summary>
    NetworkPlayer FindAt(Vector2 point)
    {
        NetworkPlayer best = null;
        float bestDistance = float.MaxValue;

        foreach (NetworkPlayer avatar in NetworkPlayer.All)
        {
            if (avatar == null || avatar.PlayerId == channel.LocalPlayerId || avatar.IsGhost) continue;
            if ((avatar.Status & StatusFlags.Stealth) != 0) continue;

            float distance = ((Vector2)avatar.transform.position - point).magnitude;
            if (distance <= config.targetClickRadius && distance < bestDistance)
            {
                best = avatar;
                bestDistance = distance;
            }
        }

        return best;
    }

    Vector2 MouseWorldPosition()
    {
        if (Mouse.current == null) return new Vector2(float.MaxValue, float.MaxValue); // Fare yoksa hiçbir şey seçilemez.

        Vector2 screen = Mouse.current.position.ReadValue();
        return worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
    }
}

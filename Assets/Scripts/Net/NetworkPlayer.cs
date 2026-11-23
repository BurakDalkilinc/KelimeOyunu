using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağdaki bir oyuncunun temsili (Aşama 3, 1. adım: sadece görünür olsun). Her bağlanan oyuncu için host bir tane doğurur.
/// Renk ve başlangıç yeri bağlantı numarasından gelir; böylece herkes aynı oyuncuyu aynı renkte görür.
/// Hareket senkronu sonraki adımdır.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    static readonly Color[] Palette =
    {
        new Color(0.95f, 0.45f, 0.45f), new Color(0.45f, 0.7f, 0.95f), new Color(0.55f, 0.9f, 0.55f), new Color(0.95f, 0.85f, 0.4f),
        new Color(0.75f, 0.55f, 0.95f), new Color(0.95f, 0.65f, 0.35f), new Color(0.5f, 0.9f, 0.85f), new Color(0.95f, 0.6f, 0.8f),
    };

    public override void OnNetworkSpawn()
    {
        int slot = (int)(OwnerClientId % (ulong)Palette.Length);

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteLibrary.Get("char") ?? SpriteFactory.Circle();
        sr.color = Palette[slot];
        sr.sortingOrder = 10;

        var animator = gameObject.GetComponent<SpriteAnimator>();
        if (animator == null) animator = gameObject.AddComponent<SpriteAnimator>();
        animator.SetFrames(SpriteLibrary.GetFrames("char"));

        // Sunucu doğurma yerini ayarlar: oyuncular yan yana dizilir.
        if (IsServer) transform.position = new Vector3((slot - 3.5f) * 1.5f, 0f, 0f);

        Debug.Log($"[Ağ] Oyuncu doğdu: numara {OwnerClientId}" + (IsOwner ? " (bu sensin)" : ""));
    }
}

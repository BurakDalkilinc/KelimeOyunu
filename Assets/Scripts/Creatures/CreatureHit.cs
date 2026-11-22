using UnityEngine;

/// <summary>
/// Bir yaratığın oyuncuya yaptığı vuruşun fırlatma kısmı. Hasar vuruş anında zaten uygulanmıştır;
/// bu bilgi, oyuncunun sahnedeki karakterini itmek için görüntü tarafına verilir.
/// </summary>
public readonly struct CreatureHit
{
    public PlayerState Player { get; }

    /// <summary>Oyuncunun kayacağı yön ve mesafe (birim). Fırlatma yoksa sıfırdır.</summary>
    public Vector2 Knockback { get; }

    public CreatureHit(PlayerState player, Vector2 knockback)
    {
        Player = player;
        Knockback = knockback;
    }
}

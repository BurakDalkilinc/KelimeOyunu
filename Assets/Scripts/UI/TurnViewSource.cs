using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ekran bileşenlerinin (zar yazısı, durum yazısı, yazı kutusu, can, etkiler, maç sonu) okuduğu kaynak.
/// Tek kişilik sahnede <see cref="MatchRunner"/>, ağ maçında <see cref="NetTurnViewSource"/> uygular; böylece aynı arayüz iki yerde çalışır.
/// Soyut sınıftır, çünkü Unity arayüz referanslarını sahneye kaydedemez.
/// </summary>
public abstract class TurnViewSource : MonoBehaviour
{
    /// <summary>Tur durumu (aşama, yazan, süre, olaylar). Bileşenler açıldığında hazır olmalıdır.</summary>
    public abstract ITurnView View { get; }

    /// <summary>Maçtaki herkes (elenenler dahil).</summary>
    public abstract IReadOnlyList<PlayerState> Players { get; }

    /// <summary>Bu maçta oynanan bilinen kelimeler, oynanma sırasıyla (maç sonu özeti için).</summary>
    public abstract IReadOnlyList<WordEntry> UsedWords { get; }

    /// <summary>Bu bilgisayardaki oyuncu; henüz belli değilse null.</summary>
    public abstract PlayerState LocalPlayer { get; }

    /// <summary>Bu bilgisayardaki oyuncunun üzerindeki etkilerin seçili dildeki özeti ("Kalkan var"...); etki yoksa boş.</summary>
    public abstract string DescribeLocalStatus();

    /// <summary>Bu bilgisayardaki oyuncunun itiş durumu yazısı ("İtiş (Boşluk): hazır" ya da "İtiş: 2 sn").</summary>
    public abstract string DescribeShove();

    /// <summary>
    /// Sol üst göstergenin (<see cref="PlayerHud"/>) bilgilerini doldurur. Varsayılan: yerel oyuncunun canı ve etkileri;
    /// tek kişilik ve ağ kaynakları itiş beklemesini ve seçilme şansını da ekler.
    /// </summary>
    public virtual void FillHud(HudSnapshot hud)
    {
        hud.Clear();
        PlayerState local = LocalPlayer;
        if (local == null) return;

        hud.HasPlayer = true;
        hud.Alive = local.IsAlive;
        hud.Health = local.Health.Current;
        hud.MaxHealth = local.Health.Max;
        if (!local.IsAlive) return;

        HudStatusCollector.Collect(local.Status, hud.Statuses);
        hud.Armed = local.Status.HasWeapon;
        if (hud.Armed)
        {
            hud.WeaponShots = local.Status.WeaponShots;
            hud.WeaponSprite = HudStatusCollector.WeaponSprite(local.Status.Weapon);
        }
    }

    /// <summary>
    /// Yazılan kelimeyi gönderir. Sonuç (kabul, uzun, yasaklı, kullanılmış...) hazır olunca <paramref name="onResult"/> çağrılır:
    /// tek kişilikte hemen, ağda host cevap verince.
    /// </summary>
    public abstract void SubmitWord(string text, Action<SubmitStatus> onResult);

    /// <summary>Maç bitince yeni maç başlatılabilir mi (ağda yalnızca host)?</summary>
    public abstract bool CanRestart { get; }

    /// <summary>Yeni maç için sıfırlar/yeniden başlatır.</summary>
    public abstract void Restart();

    /// <summary>Maç sonu ekranında "Ana menü" düğmesi gösterilsin mi?</summary>
    public virtual bool CanLeaveToMenu => false;

    /// <summary>Maçtan ana menüye döner (tek kişilikte menü sahnesi, ağda bağlantı kesilip menü).</summary>
    public virtual void LeaveToMenu() { }

    /// <summary>Maç sonu ekranının alt yazısı.</summary>
    public virtual string RestartHint => MatchSummary.GetHint();

    /// <summary>Verilen oyuncu bu bilgisayardaki oyuncu mu?</summary>
    public bool IsLocal(PlayerState player)
    {
        return player != null && player == LocalPlayer;
    }

    /// <summary>Hayattaki oyuncular (zar yazısındaki dönen isimler için).</summary>
    public IEnumerable<PlayerState> AlivePlayers
    {
        get
        {
            foreach (PlayerState player in Players)
                if (player.IsAlive) yield return player;
        }
    }
}

using System;

/// <summary>
/// Bir kelimenin sayısal değerleri (words.json içindeki "stats").
/// Her kelime hepsini kullanmaz; yazılmayan alanlar 0 kalır.
/// </summary>
[Serializable]
public class WordStats
{
    public float damage;
    public float speed;
    public float duration;
    public float radius;

    /// <summary>Kaç birim doğar (arı sayısı, meteor sayısı...).</summary>
    public int count;
    /// <summary>Yavaşlatma süresi (sn).</summary>
    public float slowSeconds;
    /// <summary>Etki başlamadan önce uyarı süresi (sn).</summary>
    public float warningSeconds;
    public float pullForce;
    /// <summary>Kalkanın engelleyeceği hasar sayısı.</summary>
    public int blocks;
    /// <summary>İyileştirilen can.</summary>
    public int heal;
    /// <summary>Duvar uzunluğu (birim).</summary>
    public float length;
}

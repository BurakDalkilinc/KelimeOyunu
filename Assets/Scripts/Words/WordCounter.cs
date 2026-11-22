using System;

/// <summary>
/// Global tehdidin haritada çıkardığı çözüm (words.json içindeki "counter").
/// Örnek: atom bombası → "shelter" (sınırlı kapasiteli sığınak).
/// </summary>
[Serializable]
public class WordCounter
{
    /// <summary>Çözüm türü: shelter, tower, cave, shadows, houses.</summary>
    public string type;
    /// <summary>Kaç tane çözüm nesnesi çıkar.</summary>
    public int count;
    /// <summary>Her birinin alabileceği oyuncu sayısı.</summary>
    public int capacityPerItem;
    /// <summary>Kapasite, hayattaki oyuncuların bu oranı kadardır (sığınak için).</summary>
    public float capacityRatio;
}

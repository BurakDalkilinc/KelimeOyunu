using System;

/// <summary>
/// Defterde tek bir kelimenin sayaçları: kaç kez kendin yazdın, kaç kez başkasının yazdığını gördün.
/// Kayıt dosyasına (collection.json) olduğu gibi yazılır; kelime kimliği (<see cref="WordEntry.id"/>) ile tutulur.
/// </summary>
[Serializable]
public class CollectionEntry
{
    public string id;
    public int timesWritten;
    public int timesSeen;

    /// <summary>Durum sayaçlardan çıkar: yazdıysan keşfedildi, yalnızca gördüysen görüldü, hiçbiri yoksa kilitli.</summary>
    public CollectionStatus Status
    {
        get
        {
            if (timesWritten > 0) return CollectionStatus.Discovered;
            if (timesSeen > 0) return CollectionStatus.Seen;
            return CollectionStatus.Locked;
        }
    }
}

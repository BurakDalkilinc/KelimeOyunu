/// <summary>
/// Bir kelimenin defterdeki durumu. Sıra önemlidir: Discovered (kendin yazdın) > Seen (başkası yazdı) > Locked (hiç rastlamadın).
/// </summary>
public enum CollectionStatus
{
    Locked = 0,
    Seen = 1,
    Discovered = 2
}

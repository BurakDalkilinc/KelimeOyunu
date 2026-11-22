using UnityEngine;

/// <summary>Baktığı yönü bilen sahne karakteri (oyuncu ve botlar): en son yürüdüğü yön.</summary>
public interface IFacing
{
    Vector2 FacingDirection { get; }
}

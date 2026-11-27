using UnityEngine;

/// <summary>Zafer konfetisinin tek parçası (konum ekran pikselinde, y aşağı doğru artar).</summary>
public struct ConfettiPiece
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Spin;
    public float SpinSpeed;
    public int ColorIndex;
}

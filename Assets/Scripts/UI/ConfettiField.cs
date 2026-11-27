using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zafer ekranının piksel konfetisi: üstten saçılır, yerçekimiyle düşer, havada yavaşlar, döner (genişliği titrer).
/// Düz hesap; görünüş rastgeleliği dışarıdan verilen kendi <see cref="System.Random"/>'ından gelir, oyun mantığına karışmaz.
/// Çizimi <see cref="MatchEndScreen"/> yapar.
/// </summary>
public class ConfettiField
{
    readonly List<ConfettiPiece> pieces = new List<ConfettiPiece>();

    public IReadOnlyList<ConfettiPiece> Pieces => pieces;

    /// <summary>Ekranın üst kenarının biraz üstünden <paramref name="count"/> parça saçar.</summary>
    public void Burst(System.Random random, int count, float width, float minSpeed, float maxSpeed, int colorCount)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Lerp(-0.6f, 0.6f, (float)random.NextDouble()); // Çoğu aşağı, biraz yana
            float speed = Mathf.Lerp(minSpeed, maxSpeed, (float)random.NextDouble());
            pieces.Add(new ConfettiPiece
            {
                Position = new Vector2((float)random.NextDouble() * width, -20f - (float)random.NextDouble() * 200f),
                Velocity = new Vector2(Mathf.Sin(angle) * speed, Mathf.Cos(angle) * speed * 0.3f),
                Spin = (float)random.NextDouble() * Mathf.PI * 2f,
                SpinSpeed = Mathf.Lerp(4f, 12f, (float)random.NextDouble()),
                ColorIndex = colorCount > 0 ? random.Next(colorCount) : 0,
            });
        }
    }

    /// <summary>Parçaları ilerletir; ekranın altından (<paramref name="bottom"/>) çıkanlar silinir.</summary>
    public void Step(float deltaTime, float gravity, float drag, float bottom)
    {
        float keep = Mathf.Clamp01(1f - drag * deltaTime);
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            ConfettiPiece p = pieces[i];
            p.Velocity = new Vector2(p.Velocity.x * keep, p.Velocity.y * keep + gravity * deltaTime);
            p.Position += p.Velocity * deltaTime;
            p.Spin += p.SpinSpeed * deltaTime;
            if (p.Position.y > bottom) pieces.RemoveAt(i);
            else pieces[i] = p;
        }
    }

    public void Clear() => pieces.Clear();
}

/// <summary>
/// Bir tuşun "yeni basıldı" anını bulur: önceki karede basılı değildi, şimdi basılı.
/// Tuşun basılı durumundan çalıştığı için, "bu karede basıldı" bayrağına güvenemeyen durumlarda da doğru sonuç verir.
/// Tuş basılı tutulurken tekrar tetiklenmez.
/// </summary>
public struct PressEdge
{
    bool wasDown;

    /// <summary>Her karede tuşun şimdiki durumuyla çağrılır; yalnızca basıldığı karede true döner.</summary>
    public bool Update(bool isDown)
    {
        bool justPressed = isDown && !wasDown;
        wasDown = isDown;
        return justPressed;
    }
}

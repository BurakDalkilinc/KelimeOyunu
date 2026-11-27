/// <summary>
/// Bir menü düğmesinin hareketi: üzerine gelince yayla büyür, basılıyken küçülür, bırakınca yerine oturur.
/// Düz hesap; çizimi <see cref="ButtonPainter"/> yapar.
/// </summary>
public class MenuButtonMotion
{
    readonly UiSpring scale = new UiSpring(1f);
    bool wasHovered;

    /// <summary>Çizimde kullanılacak ölçek (1 = normal).</summary>
    public float Scale => scale.Value;

    /// <summary>Bir kare ilerletir. İmleç düğmeye bu karede yeni geldiyse true döner (üzerine gelme sesi için).</summary>
    public bool Update(bool hovered, bool pressed, float deltaTime, float hoverScale, float pressScale, float stiffness, float damping)
    {
        float target = !hovered ? 1f : pressed ? pressScale : hoverScale;
        scale.Step(target, deltaTime, stiffness, damping);

        bool entered = hovered && !wasHovered;
        wasHovered = hovered;
        return entered;
    }
}

using UnityEngine;

/// <summary>
/// Büyük tehdidin "havası": uyarı süresince ekran giderek temanın rengine döner ve temaya uygun parçacıklar çıkar (kar, kül,
/// kıvılcım, sis, kor); etki anında temanın renginde güçlü bir parlama ve kamera sarsıntısı olur, ton yavaşça söner.
/// Tek kişilikte <see cref="ThreatDirector"/>, ağda <see cref="NetThreatRenderer"/> her kare durumu bildirir. Yalnızca görüntüdür.
/// </summary>
public class ThreatAtmosphere : MonoBehaviour
{
    const int OverlayOrder = 30;

    SpriteRenderer overlay;
    Camera cam;
    ThreatFxPreset preset;
    float tint;            // Şu anki ton yoğunluğu
    float targetTint;
    float flash;           // Etki parlaması (ayrı, hızlı söner)
    Color flashColor = Color.white;
    float spawnBudget;
    float spawnBudgetSecond;
    float intensity;       // 0..1 uyarının ilerleyişi (parçacık yoğunluğu)
    float lastReport = -1f;

    /// <summary>Ana kameradaki atmosfer bileşeni (yoksa kurar).</summary>
    public static ThreatAtmosphere Instance
    {
        get
        {
            Camera main = Camera.main;
            if (main == null) return null;
            var a = main.GetComponent<ThreatAtmosphere>();
            if (a == null) a = main.gameObject.AddComponent<ThreatAtmosphere>();
            return a;
        }
    }

    /// <summary>
    /// Bu kare süren bir tehdit var: <paramref name="fx"/> teması, <paramref name="progress"/> uyarının ilerleyişi (0 başı,
    /// 1 etki anı). Her kare çağrılmalı; çağrılmazsa ton söner.
    /// </summary>
    public void Report(string fx, float progress)
    {
        ThreatFxPreset p = ThreatFxPreset.For(fx);
        if (p == null) return;
        preset = p;
        intensity = Mathf.Clamp01(progress);
        targetTint = preset.MaxTint * Mathf.SmoothStep(0f, 1f, intensity);
        lastReport = Time.time;
    }

    /// <summary>Etki anı: temanın renginde parlama, sert sarsıntı, ton bir süre kalıp söner.</summary>
    public void Impact(string fx)
    {
        ThreatFxPreset p = ThreatFxPreset.For(fx);
        if (p != null) preset = p;
        flashColor = p != null ? p.Flash : Color.white;
        flash = 0.9f;
        if (p != null) tint = Mathf.Max(tint, p.MaxTint);
        CameraShake.Shake(0.45f, 0.6f);
    }

    /// <summary>Etki parlamasının rengi (ekran arayüzündeki parlama bunu kullanır).</summary>
    public Color FlashColor => flashColor;

    void LateUpdate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (overlay == null) BuildOverlay();

        bool reported = Time.time - lastReport < 0.2f;
        if (!reported)
        {
            float fade = preset != null ? preset.MaxTint / Mathf.Max(0.2f, preset.AfterglowSeconds) : 1f;
            targetTint = 0f;
            tint = Mathf.MoveTowards(tint, 0f, fade * Time.deltaTime);
            intensity = 0f;
        }
        else tint = Mathf.MoveTowards(tint, targetTint, Time.deltaTime * 0.5f);

        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.2f);

        // Ton + parlama birlikte; parlama baskın.
        Color c = preset != null ? preset.Tint : Color.white;
        c = Color.Lerp(c, flashColor, flash);
        c.a = Mathf.Clamp01(Mathf.Max(tint, flash));
        overlay.color = c;
        overlay.enabled = c.a > 0.003f;

        FitOverlay();
        if (reported && preset != null) SpawnParticles();
    }

    void BuildOverlay()
    {
        var go = new GameObject("Tehdit havası");
        go.transform.SetParent(transform, false);
        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = SpriteFactory.Square();
        overlay.sortingOrder = OverlayOrder;
        overlay.enabled = false;
    }

    void FitOverlay()
    {
        if (cam == null || !cam.orthographic) return;
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        Vector2 spriteSize = overlay.sprite != null ? (Vector2)overlay.sprite.bounds.size : Vector2.one;
        overlay.transform.localPosition = new Vector3(0f, 0f, 1f);
        overlay.transform.localScale = new Vector3(w / Mathf.Max(0.01f, spriteSize.x) * 1.1f, h / Mathf.Max(0.01f, spriteSize.y) * 1.1f, 1f);
    }

    /// <summary>Görünen alanın içine temanın parçacıklarını saçar; uyarı ilerledikçe yoğunlaşır.</summary>
    void SpawnParticles()
    {
        float rate = preset.ParticlesPerSecond * (0.25f + 0.75f * intensity);
        spawnBudget += rate * Time.deltaTime;
        spawnBudgetSecond += rate * 0.5f * Time.deltaTime;

        while (spawnBudget >= 1f) { spawnBudget -= 1f; SpawnOne(preset.Particles); }
        if (preset.SecondParticles != ThreatFxPreset.ParticleKind.None)
            while (spawnBudgetSecond >= 1f) { spawnBudgetSecond -= 1f; SpawnOne(preset.SecondParticles); }
        else spawnBudgetSecond = 0f;
    }

    void SpawnOne(ThreatFxPreset.ParticleKind kind)
    {
        if (cam == null) return;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Vector2 center = transform.position;
        Vector2 at = center + new Vector2(Random.Range(-w, w), Random.Range(-h, h));

        switch (kind)
        {
            case ThreatFxPreset.ParticleKind.Snow:
                BloodDrop.Spawn(at, new Vector2(Random.Range(-0.6f, 0.2f), 0f), Random.Range(2f, 5f), -0.5f, Random.Range(0.12f, 0.22f), preset.ParticleColor, false);
                break;
            case ThreatFxPreset.ParticleKind.Ash:
                BloodDrop.Spawn(at, new Vector2(Random.Range(-0.4f, 0.4f), 0f), Random.Range(2f, 4f), -0.2f, Random.Range(0.1f, 0.18f),
                                new Color(0.35f, 0.33f, 0.32f, 0.8f), false);
                break;
            case ThreatFxPreset.ParticleKind.Sparks:
                BloodDrop.Spawn(at, Random.insideUnitCircle * 0.8f, Random.Range(1f, 3f), Random.Range(1f, 3f), Random.Range(0.1f, 0.18f), preset.ParticleColor, false);
                break;
            case ThreatFxPreset.ParticleKind.Embers:
                // Yerden yükselen korlar: yüksek dikey hız, kısa ömür.
                BloodDrop.Spawn(at, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 1f)), 0.05f, Random.Range(6f, 10f), Random.Range(0.1f, 0.2f),
                                Color.Lerp(preset.ParticleColor, new Color(1f, 0.9f, 0.3f), Random.value), false);
                break;
            case ThreatFxPreset.ParticleKind.Fog:
                FogPuff.Spawn(at, preset.ParticleColor, Random.Range(2f, 4f), Random.Range(3f, 5f));
                break;
        }
    }
}

/// <summary>
/// Yaratığın üstündeki yazının dilden bağımsız hali. Kural tarafı (tehditler, ağ host'u) kelime adını yazmak yerine
/// <see cref="NameToken"/> koyar ("{w}", boss'ta "{w} 7/10"); ekrana basan taraf onu seçili dildeki adla değiştirir.
/// Böylece ağda her oyuncu kelimeyi kendi dilinde görür. Düz hesap, test edilebilir.
/// </summary>
public static class CreatureLabel
{
    /// <summary>Kelime adının yeri.</summary>
    public const string NameToken = "{w}";

    /// <summary>Yazıdaki <see cref="NameToken"/>'ı kelimenin seçili dildeki adıyla değiştirir. Belirteç yoksa yazı aynen döner.</summary>
    public static string Resolve(string label, WordEntry word)
    {
        if (string.IsNullOrEmpty(label)) return string.Empty;
        if (!label.Contains(NameToken)) return label;
        return label.Replace(NameToken, word != null ? word.LocalizedDisplay : "?");
    }
}

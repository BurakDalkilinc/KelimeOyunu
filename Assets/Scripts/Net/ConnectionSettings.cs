/// <summary>
/// "Katıl" ekranına yazılan adresi (örn. "192.168.1.5" ya da "192.168.1.5:7777") ayrıştırır ve doğrular.
/// Düz hesap, ağdan bağımsız; test edilebilir.
/// </summary>
public static class ConnectionSettings
{
    public const string DefaultAddress = "127.0.0.1";
    public const ushort DefaultPort = 7777;

    /// <summary>
    /// "adres" ya da "adres:port" metnini çözer. Adres geçerli bir IPv4 olmalı; port 1-65535 arası olmalı (yoksa varsayılan).
    /// Geçersizse false döner ve <paramref name="error"/> oyuncuya gösterilecek Türkçe mesajı içerir.
    /// </summary>
    public static bool TryParse(string text, out string address, out ushort port, out string error)
    {
        address = DefaultAddress;
        port = DefaultPort;
        error = null;

        text = text == null ? "" : text.Trim();
        if (text.Length == 0) return true; // Boşsa yerel makine (aynı bilgisayarda deneme)

        string addressPart = text;
        int colon = text.LastIndexOf(':');
        if (colon >= 0)
        {
            addressPart = text.Substring(0, colon).Trim();
            string portPart = text.Substring(colon + 1).Trim();
            if (!int.TryParse(portPart, out int parsedPort) || parsedPort < 1 || parsedPort > 65535)
            {
                error = "Port 1 ile 65535 arasında bir sayı olmalı.";
                return false;
            }
            port = (ushort)parsedPort;
        }

        if (!IsIpv4(addressPart))
        {
            error = "Adres 192.168.1.5 gibi bir IP olmalı.";
            return false;
        }

        address = addressPart;
        return true;
    }

    static bool IsIpv4(string value)
    {
        string[] parts = value.Split('.');
        if (parts.Length != 4) return false;

        foreach (string part in parts)
        {
            if (part.Length == 0 || part.Length > 3) return false;
            foreach (char c in part)
                if (c < '0' || c > '9') return false;
            if (int.Parse(part) > 255) return false;
        }
        return true;
    }
}

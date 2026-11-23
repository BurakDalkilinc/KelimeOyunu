# Üçüncü taraf bileşenler ve lisanslar

| Bileşen | Nerede | Lisans | Kaynak |
| --- | --- | --- | --- |
| Pixelify Sans (yazı tipi) | `Assets/Resources/Fonts/` | SIL Open Font License (dosya yanında) | Google Fonts (`google/fonts`, `ofl/pixelifysans`) |
| Facepunch.Steamworks 2.3.3 (`Facepunch.Steamworks.Win64.dll`) | `Assets/Plugins/Steamworks/` | MIT | NuGet `facepunch.steamworks` 2.3.3 |
| `steam_api64.dll` (Valve Steamworks yeniden dağıtılabilir) | `Assets/Plugins/Steamworks/x86_64/` | Valve Steamworks SDK koşulları (imzası doğrulandı: Valve) | `Unity-Technologies/multiplayer-community-contributions` içindeki `redistributable_bin/win64` |
| `FacepunchTransport.cs` (Netcode için Steam aktarım katmanı) | `Assets/Scripts/Steam/` | MIT (Unity Technologies, 2021) | `Unity-Technologies/multiplayer-community-contributions`, `Transports/com.community.netcode.transport.facepunch` (yalnızca `[ReadOnly]` satırı çıkarıldı) |
| Netcode for GameObjects 2.14.0, Multiplayer Play Mode 3.0.0 | Unity paketleri | Unity Companion License | Unity paket deposu |

Steam testleri Spacewar uygulama kimliği (480) ile yapılır. Oyun Steam'de yayınlanırken kendi App ID'ne geçilmeli (`SteamLobbyMenu.appId` ve `FacepunchTransport.steamAppId`).

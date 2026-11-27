import json, sys

# (anahtar, Türkçe, İngilizce)
ROWS = [
    # Ayarlar
    ("settings.title", "Ayarlar", "Settings"),
    ("settings.menu_button", "Ayarlar", "Settings"),
    ("settings.master_volume", "Ana ses", "Master volume"),
    ("settings.music_volume", "Müzik", "Music"),
    ("settings.sfx_volume", "Efektler", "Effects"),
    ("settings.fullscreen", "Tam ekran", "Fullscreen"),
    ("settings.resolution", "Çözünürlük", "Resolution"),
    ("settings.vsync", "Dikey eşitleme (VSync)", "Vertical sync (VSync)"),
    ("settings.name_tags", "İsim etiketleri", "Name tags"),
    ("settings.language", "Dil", "Language"),
    ("settings.on", "Açık", "On"),
    ("settings.off", "Kapalı", "Off"),
    ("settings.native_resolution", "Ekranın kendisi", "Native"),
    ("settings.apply", "Uygula", "Apply"),
    ("settings.close", "Kapat (Esc)", "Close (Esc)"),
    ("settings.back_to_menu", "Ana menüye dön", "Back to main menu"),
    ("settings.display_hint", "Görüntü ayarları \"Uygula\" ile değişir. Ses ayarları hemen duyulur.",
     "Display settings change with \"Apply\". Sound settings are heard right away."),
    ("settings.percent", "%{0}", "{0}%"),

    # Defter
    ("collection.title", "Koleksiyon Defteri", "Collection Book"),
    ("collection.menu_button", "Defter (koleksiyon)", "Book (collection)"),
    ("collection.seen_only", "Kendin yazınca açılır", "Unlocks when you write it yourself"),
    ("collection.kind_and_length", "{0} · {1} harf", "{0} · {1} letters"),
    ("collection.counts", "{0} kez yazdın, {1} kez gördün", "Written: {0}, seen: {1}"),
    ("collection.progress", "{0} / {1} keşfedildi", "{0} / {1} discovered"),
    ("collection.new_words", "Deftere {0} yeni kelime eklendi!", "{0} new words added to the book!"),
    ("collection.page", "Sayfa {0} / {1}", "Page {0} / {1}"),
    ("collection.tab_all", "Tümü", "All"),
    ("collection.tab_creatures", "Yaratıklar", "Creatures"),
    ("collection.tab_defense", "Savunma", "Defense"),
    ("collection.tab_threats", "Büyük tehditler", "Big threats"),
    ("collection.kind_threat", "Büyük tehdit", "Big threat"),
    ("collection.kind_defense", "Savunma", "Defense"),
    ("collection.kind_creature", "Yaratık", "Creature"),
    ("collection.kind_word", "Kelime", "Word"),
    ("collection.empty_tab", "Bu sekmede kelime yok.", "No words in this tab."),
    ("collection.previous", "< Önceki", "< Previous"),
    ("collection.next", "Sonraki >", "Next >"),
    ("collection.close", "Kapat (Esc)", "Close (Esc)"),
    ("collection.pick_card", "Ayrıntı için bir karta tıkla.", "Click a card for details."),
    ("collection.locked_hint", "Bir maçta bu kelimeyi yaz ya da yazılırken gör.", "Write this word in a match, or see someone write it."),
    ("collection.traits", "Özellikler: {0}", "Traits: {0}"),

    # Tur
    ("turn.rolling", "Zar atılıyor...", "Rolling the dice..."),
    ("turn.your_turn", "Sıra sende! Bir kelime yaz ({0} sn)", "Your turn! Type a word ({0} s)"),
    ("turn.other_typing", "{0} bir kelime yazıyor... ({1} sn)", "{0} is typing a word... ({1} s)"),
    ("turn.wrote", "{0} yazdı: {1}  ({2} sn)", "{0} wrote: {1}  ({2} s)"),
    ("turn.chance", "Seçilme şansın: %{0}", "Your chance to be picked: {0}%"),
    ("typing.input", "Kelime: {0}", "Word: {0}"),
    ("typing.hint", "Bir kelime yaz ve Enter'a bas", "Type a word and press Enter"),
    ("dice.match_over", "Maç bitti", "Match over"),

    # Kelime gönderme geri bildirimi
    ("feedback.empty", "Bir kelime yazmalısın.", "You need to type a word."),
    ("feedback.too_long", "Çok uzun! En fazla {0} karakter.", "Too long! At most {0} characters."),
    ("feedback.blocked", "Bu kelime yasak.", "This word is not allowed."),
    ("feedback.already_used", "Bu kelime bu maçta zaten kullanıldı!", "This word was already used this match!"),
    ("feedback.not_writer", "Şu an sıra sende değil.", "It's not your turn right now."),

    # Maç sonu
    ("match.winner", "Kazanan: {0}", "Winner: {0}"),
    ("match.no_winner", "Kazanan yok", "No winner"),
    ("match.no_words", "Maçta hiç kelime yazılmadı.", "No words were written this match."),
    ("match.words_header", "Yazılan kelimeler ({0}):", "Words written ({0}):"),
    ("match.restart_hint", "Yeniden başlamak için R'ye bas  (Esc: ana menü)", "Press R to restart  (Esc: main menu)"),
    ("match.restart_hint_host", "Yeni maç için R'ye bas", "Press R for a new match"),
    ("match.wait_host_restart", "Host'un yeni maçı hazırlaması bekleniyor...", "Waiting for the host to set up a new match..."),

    # Durum etkileri
    ("status.slowed_time", "Yavaşladın! ({0} sn)", "Slowed! ({0} s)"),
    ("status.slowed", "Yavaşladın!", "Slowed!"),
    ("status.pulled", "Çekiliyorsun!", "You're being pulled!"),
    ("status.shield", "Kalkan var", "Shield up"),
    ("status.burning", "YANIYORSUN!", "YOU'RE ON FIRE!"),
    ("status.driving_time", "{0} sürüyorsun ({1} sn)", "Driving: {0} ({1} s)"),
    ("status.driving_explodes", " — eve çarparsan patlarsın!", " — hit a house and you explode!"),
    ("status.disguised_time", "Kamufle oldun ({0} sn)", "Disguised ({0} s)"),
    ("status.stealth_time", "Görünmezsin ({0} sn)", "Invisible ({0} s)"),
    ("status.stealth", "Görünmezsin", "Invisible"),
    ("status.flying_time", "Uçuyorsun ({0} sn)", "Flying ({0} s)"),
    ("status.flying", "Uçuyorsun", "Flying"),
    ("status.vehicle_net", "Araç sürüyorsun — eve çarparsan patlarsın!", "Driving a vehicle — hit a house and you explode!"),

    # Silah, itiş, can
    ("weapon.armed_net", "Silah elinde: Boşluk ile ateş et!", "Weapon in hand: fire with Space!"),
    ("weapon.default_name", "Silah", "Weapon"),
    ("weapon.melee", "{0} (Boşluk ile vur): {1} vuruş, {2} sn", "{0} (Space to hit): {1} hits, {2} s"),
    ("weapon.ranged", "{0} (Boşluk ile ateş): {1} atış, {2} sn", "{0} (Space to fire): {1} shots, {2} s"),
    ("shove.ready", "İtiş (Boşluk): hazır", "Shove (Space): ready"),
    ("shove.wait", "İtiş: {0} sn", "Shove: {0} s"),
    ("health.value", "Can: {0} / {1}", "Lives: {0} / {1}"),
    ("health.ghost", "Elendin: hayalet olarak izliyorsun", "Eliminated: watching as a ghost"),
    ("health.value_net", "Can: {0}", "Lives: {0}"),
    ("health.ghost_net", "Elendin! Hayalet olarak izliyorsun.", "Eliminated! Watching as a ghost."),

    # Hedef seçme
    ("target.choose", "Hedef seç! Bir oyuncuya tıkla ({0} sn)", "Pick a target! Click a player ({0} s)"),
    ("target.other_choosing", "{0} hedef seçiyor...", "{0} is picking a target..."),
    ("target.someone", "Biri", "Someone"),

    # Büyük tehditler ve çözüm alanları
    ("threat.hint_shelter", "Sığınağa gir!", "Get into the shelter!"),
    ("threat.hint_tower", "Kuleye çık!", "Climb a tower!"),
    ("threat.hint_cave", "Mağaraya sığınabilirsin!", "You can hide in a cave!"),
    ("threat.hint_houses", "Eve sığınabilirsin!", "You can hide in a house!"),
    ("threat.hint_shadows", "Gölgelere dikkat!", "Watch the shadows!"),
    ("threat.hint_default", "Kaç!", "Run!"),
    ("threat.alarm", "{0}! {1} ({2} sn)", "{0}! {1} ({2} s)"),
    ("threat.alarm_no_time", "{0}! {1}", "{0}! {1}"),
    ("zone.shelter", "Sığınak", "Shelter"),
    ("zone.tower", "Kule", "Tower"),
    ("zone.cave", "Mağara", "Cave"),
    ("zone.houses", "Ev", "House"),
    ("zone.default", "Alan", "Zone"),
    ("zone.label", "{0} {1}/{2}", "{0} {1}/{2}"),

    # Oyuncu adları
    ("player.you", "Sen", "You"),
    ("player.default_name", "Oyuncu {0}", "Player {0}"),
    ("boss.default_name", "Boss", "Boss"),

    # Ağ maçı paneli
    ("net.not_started", "Maç henüz başlamadı.", "The match hasn't started yet."),
    ("net.lobby_players", "Lobidekiler:", "In the lobby:"),
    ("net.you_suffix", " (SEN)", " (YOU)"),
    ("net.player_count", "Oyuncu sayısı: {0}  ({1} insan + {2} bot)", "Players: {0}  ({1} human + {2} bot)"),
    ("net.start_match", "Maçı başlat", "Start match"),
    ("net.wait_host_start", "Host'un maçı başlatması bekleniyor...", "Waiting for the host to start the match..."),
    ("net.turn_line", "Tur {0} · {1:0.0} sn", "Turn {0} · {1:0.0} s"),
    ("net.prepare_new_match", "Yeni maç için hazırla", "Prepare a new match"),
    ("net.wait_host_new_match", "Host'un yeni maçı başlatması bekleniyor...", "Waiting for the host to start a new match..."),
    ("net.bot_took_over", " (bot devraldı)", " (bot took over)"),
    ("net.headline_dice", "Zar atılıyor... {0}", "Rolling the dice... {0}"),
    ("net.headline_typing", "{0} yazıyor", "{0} is typing"),
    ("net.headline_result", "{0}: {1}", "{0}: {1}"),
    ("net.type_prompt", "Bir kelime yaz ve Enter'a bas:", "Type a word and press Enter:"),
    ("net.send", "Gönder", "Send"),
    ("net.kick_match_started", "Maç başladı, bir sonraki maçı bekle.", "The match has started, wait for the next one."),
    ("net.kick_room_full", "Oda dolu (en çok {0} oyuncu).", "The room is full (at most {0} players)."),

    # Yerel ağ menüsü
    ("netmenu.no_manager", "NetworkManager yok.", "No NetworkManager."),
    ("netmenu.title", "Çok oyunculu deneme", "Multiplayer test"),
    ("netmenu.address", "Host'un IP adresi (aynı bilgisayarda boş bırak):", "Host IP address (leave empty on the same computer):"),
    ("netmenu.host", "Host (oyunu kur)", "Host (set up the game)"),
    ("netmenu.join", "Katıl", "Join"),
    ("netmenu.solo", "Tek kişilik oyna (botlarla)", "Play solo (with bots)"),
    ("netmenu.host_count", "Host: {0} oyuncu bağlı", "Host: {0} players connected"),
    ("netmenu.connected", "Host'a bağlandın.", "Connected to the host."),
    ("netmenu.connecting", "Bağlanılıyor...", "Connecting..."),
    ("netmenu.disconnect", "Bağlantıyı kes", "Disconnect"),
    ("netmenu.no_transport", "UnityTransport bulunamadı.", "UnityTransport not found."),
    ("netmenu.host_failed", "Host başlatılamadı (port dolu olabilir).", "Couldn't start the host (the port may be in use)."),
    ("netmenu.client_failed", "Bağlantı başlatılamadı.", "Couldn't start the connection."),
    ("connection.bad_port", "Port 1 ile 65535 arasında bir sayı olmalı.", "The port must be a number between 1 and 65535."),
    ("connection.bad_address", "Adres 192.168.1.5 gibi bir IP olmalı.", "The address must be an IP like 192.168.1.5."),

    # Steam lobisi
    ("steam.title", "Steam ile oyna", "Play with Steam"),
    ("steam.not_connected", "Steam'e bağlanılmadı.", "Not connected to Steam."),
    ("steam.connect", "Steam'e bağlan", "Connect to Steam"),
    ("steam.client_hint", "Steam istemcisi açık ve giriş yapılmış olmalı.", "The Steam client must be running and logged in."),
    ("steam.friends_only", "Yalnızca arkadaşlar (davet ile katılır, aramada görünmez)", "Friends only (join by invite, hidden from search)"),
    ("steam.create_lobby", "Lobi kur (host ol)", "Create lobby (be the host)"),
    ("steam.find_lobbies", "Lobileri bul", "Find lobbies"),
    ("steam.join_lobby", "Katıl: {0} ({1}/{2})", "Join: {0} ({1}/{2})"),
    ("steam.lobby_members", "Lobi: {0}/{1} kişi", "Lobby: {0}/{1} players"),
    ("steam.invite", "Arkadaş davet et (Steam)", "Invite friends (Steam)"),
    ("steam.leave", "Lobiden ayrıl", "Leave lobby"),
    ("steam.connected", "Steam'e bağlandı: {0}", "Connected to Steam: {0}"),
    ("steam.connect_failed", "Steam'e bağlanılamadı. Steam açık mı? ({0})", "Couldn't connect to Steam. Is Steam running? ({0})"),
    ("steam.creating", "Lobi kuruluyor...", "Creating lobby..."),
    ("steam.create_failed", "Lobi kurulamadı.", "Couldn't create the lobby."),
    ("steam.lobby_ready", "Lobi hazır. Arkadaşını davet et, sonra Maçı başlat.", "Lobby ready. Invite your friends, then Start match."),
    ("steam.host_failed", "Host başlatılamadı.", "Couldn't start the host."),
    ("steam.lobby_error", "Lobi hatası: {0}", "Lobby error: {0}"),
    ("steam.searching", "Lobiler aranıyor...", "Searching for lobbies..."),
    ("steam.none_found", "Lobi bulunamadı.", "No lobbies found."),
    ("steam.found", "{0} lobi bulundu.", "{0} lobbies found."),
    ("steam.search_error", "Arama hatası: {0}", "Search error: {0}"),
    ("steam.joining", "Lobiye katılınıyor...", "Joining the lobby..."),
    ("steam.join_failed", "Lobiye girilemedi: {0}", "Couldn't enter the lobby: {0}"),
    ("steam.connecting_host", "Host'a bağlanılıyor...", "Connecting to the host..."),
    ("steam.client_failed", "Bağlantı başlatılamadı.", "Couldn't start the connection."),
    ("steam.join_error", "Katılma hatası: {0}", "Join error: {0}"),
    ("steam.left", "Lobiden ayrıldın.", "You left the lobby."),
    # Arayüz cilası: ana menü, duyurular, maç sonu
    ("menu.solo", "Tek kişilik oyna", "Play solo"),
    ("menu.multiplayer", "Çok oyunculu", "Multiplayer"),
    ("menu.book", "Defter", "Book"),
    ("menu.quit", "Çıkış", "Quit"),
    ("menu.back", "< Geri", "< Back"),
    ("menu.lan_host", "Yerel ağda kur (host)", "Host on local network"),
    ("menu.lan_join", "Yerel ağda katıl", "Join on local network"),
    ("menu.advanced_closed", "Gelişmiş (+)", "Advanced (+)"),
    ("menu.advanced_open", "Gelişmiş (-)", "Advanced (-)"),
    ("banner.ready", "HAZIR!", "READY!"),
    ("banner.your_turn", "SIRA SENDE!", "YOUR TURN!"),
    ("typing.hint_time", "Bir kelime yaz ve Enter'a bas  ·  {0}", "Type a word and press Enter  ·  {0}"),
    ("match.you_won", "KAZANDIN!", "YOU WON!"),
    ("match.stat_turns", "Oynanan tur: {0}", "Turns played: {0}"),
    ("match.stat_players", "Oyuncu sayısı: {0}", "Players: {0}"),
    ("match.play_again", "Tekrar oyna", "Play again"),
    ("match.main_menu", "Ana menü", "Main menu"),
    ("settings.screen_shake", "Ekran sarsıntısı", "Screen shake"),
    ("settings.screen_shake_strength", "Sarsıntı şiddeti", "Shake strength"),
    ("hit.block", "BLOK", "BLOCK"),
    ("nametag.bot_badge", "[BOT]", "[BOT]"),
    ("hud.ghost", "HAYALET", "GHOST"),
    ("hud.key_space", "BOŞLUK", "SPACE"),
    ("typing.writing_other", "{0} yazıyor", "{0} is typing"),
    ("typing.rolling", "Zar atılıyor", "Rolling the dice"),
]


def write(path, lang, index):
    keys = set()
    entries = []
    for row in ROWS:
        if row[0] in keys:
            sys.exit("iki kez: " + row[0])
        keys.add(row[0])
        entries.append({"key": row[0], "value": row[index]})
    data = {"version": 1, "language": lang, "entries": entries}
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("{\n  \"version\": 1,\n  \"language\": \"" + lang + "\",\n  \"entries\": [\n")
        lines = ["    " + json.dumps(e, ensure_ascii=False) for e in entries]
        f.write(",\n".join(lines))
        f.write("\n  ]\n}\n")
    json.loads(open(path, encoding="utf-8").read())


base = sys.argv[1]
write(base + "/lang_tr.json", "tr", 1)
write(base + "/lang_en.json", "en", 2)
print(len(ROWS), "anahtar")

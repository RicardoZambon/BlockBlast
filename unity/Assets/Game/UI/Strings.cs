#nullable enable
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BlockPuzzle.Game
{
    public enum Language
    {
        English,
        BrazilianPortuguese,
        LatinAmericanSpanish,
        Indonesian,
    }

    /// <summary>
    /// Every word the player reads, by key, in English, Brazilian Portuguese, Latin-American Spanish and Indonesian
    /// (master plan 2.8). Keys never change; texts may. A key missing from a translation falls back to English, and one
    /// missing from English shows itself, so a gap is visible on screen and never blank. The translations are drafts
    /// for a native speaker to review.
    /// </summary>
    public static class Strings
    {
        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["home.play"] = "Play",
            ["home.continue"] = "Continue",
            ["home.daily"] = "Daily",
            ["home.daily_streak"] = "Daily · {0}-day streak",
            ["home.daily_continue"] = "Daily · continue",
            ["home.daily_done"] = "Daily · done",
            ["home.settings"] = "Settings",
            ["common.home"] = "Home",
            ["pause.title"] = "Paused",
            ["pause.resume"] = "Resume",
            ["game_over.title"] = "Game over",
            ["game_over.daily_title"] = "Daily done",
            ["game_over.play_again"] = "Play again",
            ["game_over.revive"] = "Revive",
            ["notify.daily_ready"] = "Today's puzzle is ready",
            ["game_over.share"] = "Share",
            ["game_over.scores"] = "Score {0}\nBest {1}",
            ["game_over.streak"] = "{0}-day streak",
            ["hud.best"] = "Best {0}",
            ["hud.streak"] = "Streak {0}",
            ["praise.great"] = "Great!",
            ["praise.amazing"] = "Amazing!",
            ["praise.incredible"] = "Incredible!",
            ["praise.board_clear"] = "Board clear!",
            ["praise.streak"] = "Streak {0}!",
            ["tutorial.drag"] = "Drag the piece onto the board",
            ["tutorial.row"] = "Fill a row to clear it",
            ["tutorial.column"] = "Columns clear too",
            ["tutorial.skip"] = "Skip",
            ["settings.title"] = "Settings",
            ["settings.sound"] = "Sound",
            ["settings.music"] = "Music",
            ["settings.haptics"] = "Haptics",
            ["settings.reduced_motion"] = "Reduced motion",
            ["settings.version"] = "Version {0}",
            ["settings.back"] = "Back",
            ["settings.remove_ads"] = "Remove ads",
            ["settings.remove_ads_price"] = "Remove ads · {0}",
            ["settings.ads_removed"] = "Ads removed",
            ["settings.restore"] = "Restore purchases",
            ["settings.privacy"] = "Privacy settings",
            ["share.result"] = "{0} Daily {1}: {2} points",
            ["share.streak"] = ", {0}-day streak",
            ["share.chooser"] = "Share",
        };

        static readonly Dictionary<string, string> BrazilianPortuguese = new Dictionary<string, string>
        {
            ["home.play"] = "Jogar",
            ["home.continue"] = "Continuar",
            ["home.daily"] = "Desafio diário",
            ["home.daily_streak"] = "Desafio diário · {0} dias seguidos",
            ["home.daily_continue"] = "Desafio diário · continuar",
            ["home.daily_done"] = "Desafio diário · feito",
            ["home.settings"] = "Configurações",
            ["common.home"] = "Início",
            ["pause.title"] = "Pausado",
            ["pause.resume"] = "Continuar",
            ["game_over.title"] = "Fim de jogo",
            ["game_over.daily_title"] = "Desafio concluído",
            ["game_over.play_again"] = "Jogar de novo",
            ["game_over.revive"] = "Reviver",
            ["notify.daily_ready"] = "O desafio de hoje está pronto",
            ["game_over.share"] = "Compartilhar",
            ["game_over.scores"] = "Pontos {0}\nRecorde {1}",
            ["game_over.streak"] = "{0} dias seguidos",
            ["hud.best"] = "Recorde {0}",
            ["hud.streak"] = "Sequência {0}",
            ["praise.great"] = "Ótimo!",
            ["praise.amazing"] = "Incrível!",
            ["praise.incredible"] = "Fantástico!",
            ["praise.board_clear"] = "Tabuleiro limpo!",
            ["praise.streak"] = "Sequência {0}!",
            ["tutorial.drag"] = "Arraste a peça para o tabuleiro",
            ["tutorial.row"] = "Complete uma linha para limpá-la",
            ["tutorial.column"] = "Colunas também são limpas",
            ["tutorial.skip"] = "Pular",
            ["settings.title"] = "Configurações",
            ["settings.sound"] = "Som",
            ["settings.music"] = "Música",
            ["settings.haptics"] = "Vibração",
            ["settings.reduced_motion"] = "Reduzir movimento",
            ["settings.version"] = "Versão {0}",
            ["settings.back"] = "Voltar",
            ["settings.remove_ads"] = "Remover anúncios",
            ["settings.remove_ads_price"] = "Remover anúncios · {0}",
            ["settings.ads_removed"] = "Anúncios removidos",
            ["settings.restore"] = "Restaurar compras",
            ["settings.privacy"] = "Configurações de privacidade",
            ["share.result"] = "{0} Desafio diário {1}: {2} pontos",
            ["share.streak"] = ", {0} dias seguidos",
            ["share.chooser"] = "Compartilhar",
        };

        static readonly Dictionary<string, string> LatinAmericanSpanish = new Dictionary<string, string>
        {
            ["home.play"] = "Jugar",
            ["home.continue"] = "Continuar",
            ["home.daily"] = "Reto diario",
            ["home.daily_streak"] = "Reto diario · {0} días seguidos",
            ["home.daily_continue"] = "Reto diario · continuar",
            ["home.daily_done"] = "Reto diario · hecho",
            ["home.settings"] = "Ajustes",
            ["common.home"] = "Inicio",
            ["pause.title"] = "En pausa",
            ["pause.resume"] = "Reanudar",
            ["game_over.title"] = "Fin del juego",
            ["game_over.daily_title"] = "Reto completado",
            ["game_over.play_again"] = "Jugar de nuevo",
            ["game_over.revive"] = "Revivir",
            ["notify.daily_ready"] = "El reto de hoy está listo",
            ["game_over.share"] = "Compartir",
            ["game_over.scores"] = "Puntos {0}\nRécord {1}",
            ["game_over.streak"] = "{0} días seguidos",
            ["hud.best"] = "Récord {0}",
            ["hud.streak"] = "Racha {0}",
            ["praise.great"] = "¡Genial!",
            ["praise.amazing"] = "¡Increíble!",
            ["praise.incredible"] = "¡Fantástico!",
            ["praise.board_clear"] = "¡Tablero limpio!",
            ["praise.streak"] = "¡Racha de {0}!",
            ["tutorial.drag"] = "Arrastra la pieza al tablero",
            ["tutorial.row"] = "Completa una fila para borrarla",
            ["tutorial.column"] = "Las columnas también se borran",
            ["tutorial.skip"] = "Omitir",
            ["settings.title"] = "Ajustes",
            ["settings.sound"] = "Sonido",
            ["settings.music"] = "Música",
            ["settings.haptics"] = "Vibración",
            ["settings.reduced_motion"] = "Reducir movimiento",
            ["settings.version"] = "Versión {0}",
            ["settings.back"] = "Volver",
            ["settings.remove_ads"] = "Quitar anuncios",
            ["settings.remove_ads_price"] = "Quitar anuncios · {0}",
            ["settings.ads_removed"] = "Anuncios quitados",
            ["settings.restore"] = "Restaurar compras",
            ["settings.privacy"] = "Configuración de privacidad",
            ["share.result"] = "{0} Reto diario {1}: {2} puntos",
            ["share.streak"] = ", {0} días seguidos",
            ["share.chooser"] = "Compartir",
        };

        static readonly Dictionary<string, string> Indonesian = new Dictionary<string, string>
        {
            ["home.play"] = "Main",
            ["home.continue"] = "Lanjutkan",
            ["home.daily"] = "Tantangan harian",
            ["home.daily_streak"] = "Tantangan harian · {0} hari beruntun",
            ["home.daily_continue"] = "Tantangan harian · lanjutkan",
            ["home.daily_done"] = "Tantangan harian · selesai",
            ["home.settings"] = "Pengaturan",
            ["common.home"] = "Beranda",
            ["pause.title"] = "Dijeda",
            ["pause.resume"] = "Lanjutkan",
            ["game_over.title"] = "Permainan selesai",
            ["game_over.daily_title"] = "Tantangan selesai",
            ["game_over.play_again"] = "Main lagi",
            ["game_over.revive"] = "Hidupkan lagi",
            ["notify.daily_ready"] = "Tantangan hari ini sudah siap",
            ["game_over.share"] = "Bagikan",
            ["game_over.scores"] = "Skor {0}\nTerbaik {1}",
            ["game_over.streak"] = "{0} hari beruntun",
            ["hud.best"] = "Terbaik {0}",
            ["hud.streak"] = "Beruntun {0}",
            ["praise.great"] = "Hebat!",
            ["praise.amazing"] = "Luar biasa!",
            ["praise.incredible"] = "Menakjubkan!",
            ["praise.board_clear"] = "Papan bersih!",
            ["praise.streak"] = "Beruntun {0}!",
            ["tutorial.drag"] = "Seret keping ke papan",
            ["tutorial.row"] = "Penuhi satu baris untuk menghapusnya",
            ["tutorial.column"] = "Kolom juga bisa dihapus",
            ["tutorial.skip"] = "Lewati",
            ["settings.title"] = "Pengaturan",
            ["settings.sound"] = "Suara",
            ["settings.music"] = "Musik",
            ["settings.haptics"] = "Getaran",
            ["settings.reduced_motion"] = "Kurangi gerakan",
            ["settings.version"] = "Versi {0}",
            ["settings.back"] = "Kembali",
            ["settings.remove_ads"] = "Hapus iklan",
            ["settings.remove_ads_price"] = "Hapus iklan · {0}",
            ["settings.ads_removed"] = "Iklan dihapus",
            ["settings.restore"] = "Pulihkan pembelian",
            ["settings.privacy"] = "Pengaturan privasi",
            ["share.result"] = "{0} Tantangan harian {1}: {2} poin",
            ["share.streak"] = ", {0} hari beruntun",
            ["share.chooser"] = "Bagikan",
        };

        static readonly Dictionary<Language, Dictionary<string, string>> Tables = new Dictionary<Language, Dictionary<string, string>>
        {
            [Language.English] = English,
            [Language.BrazilianPortuguese] = BrazilianPortuguese,
            [Language.LatinAmericanSpanish] = LatinAmericanSpanish,
            [Language.Indonesian] = Indonesian,
        };

        /// <summary>The language the screens read. App sets it from the phone's language when it boots.</summary>
        public static Language Current { get; set; } = Language.English;

        public static IEnumerable<string> Keys => English.Keys;

        public static IEnumerable<Language> Languages => Tables.Keys;

        /// <summary>The table for the phone's language: Unity reports any Portuguese as Portuguese, so Brazil's is used.</summary>
        public static Language FromSystem(SystemLanguage language) => language switch
        {
            SystemLanguage.Portuguese => Language.BrazilianPortuguese,
            SystemLanguage.Spanish => Language.LatinAmericanSpanish,
            SystemLanguage.Indonesian => Language.Indonesian,
            _ => Language.English,
        };

        public static bool Has(Language language, string key) => Tables[language].ContainsKey(key);

        /// <summary>The text for a key in the current language, falling back to English, then to the key itself.</summary>
        public static string Get(string key) =>
            Tables[Current].TryGetValue(key, out var text) ? text : English.TryGetValue(key, out var english) ? english : key;

        /// <summary>The text for a key with its {0}, {1}… filled in, numbers written the same way everywhere.</summary>
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}

// Program.cs — ESSS DZ Bot (single-file, pro UI, tri-lingual, exact calendar + eligibility tables)
// TargetFramework: net8.0
// NuGet: Telegram.Bot (22.3.0)
// Run: TELEGRAM_BOT_TOKEN=xxxxx dotnet run
//
// Data sources provided by the user and reflected here (2025–2026):
// - Official ESSS Master concours Arabic notice (programs, seats, eligibility tables w/ weights, dossier, phases, full calendar).
// - Seats allocation paragraph (Maghreb/Africa francophone 1/4, social-security cadres 1/4, Algerian students outside funds 1/2).
// See README note at bottom of this file for details.

using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Esssbot;

public class Program
{
    // --- Runtime config ---
    private static readonly string BotToken =
        Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
        ?? "REPLACE_ME_WITH_ENV";

    // --- Minimal persistence (in-memory) ---
    private static readonly Dictionary<long, string> UserLangByChat = new(); // chatId -> "en|fr|ar"
    private static readonly Dictionary<long, UserInfo> Users = new();        // userId -> info
    private static readonly string[] Langs = ["en", "fr", "ar"];

    // --- Official links ---
    private static class Links
    {
        public const string Website = "https://www.esss.dz/";
        public const string Inscription = "https://inscription.esss.dz/candidat/inscription";
    }

    // --- Admissions data (2025–2026) from uploaded PDFs ---
    private static readonly AdmissionsData Admissions = new();

    // --- Translations ---
    private static readonly Dictionary<string, Dictionary<string, string>> Txt = new()
    {
        // English
        ["en"] = new()
        {
            ["welcome"] =
                "Welcome to the ESSS Bot! Choose an option below.",
            ["about"] =
                "The Higher School of Social Security (ESSS) is a public higher-education institution created in 2012 in Ben Aknoun, Algiers. It provides initial and continuing training for social-security organizations and contributes to studies and international cooperation.",
            ["programs_header"] = "Professional Master Tracks (2025–2026):",
            ["programs_list"] =
                "• Social Protection Law\n" +
                "• Administration & Human Resources (Management)\n" +
                "• Information Systems & Digital Transformation\n" +
                "• Risk & Finance (Actuarial/Quant)\n",
            ["concours_header"] = "National Entrance Competition (Master, 2025–2026)",
            ["concours_phases"] =
                "Selection occurs in two phases:\n" +
                "1) Ranking (file-based): coefficient for degree/specialty alignment (per table) + weighted last-year university average.\n" +
                "2) Exams: Written exam, then oral for those admitted after the written.",
            ["eligibility_intro"] =
                "Eligibility & alignment weights (examples, see tables below). The ranking phase uses:\n" +
                "• Alignment coefficient: depends on your degree/specialty match with the chosen Master (see weight tables).\n" +
                "• Last-year average: weighted in the ranking (see notice).",
            ["eligibility_tables"] = "Alignment weight tables by track:",
            ["dossier"] =
                "Application dossier (upload as a single merged file at online registration; originals at final enrollment):\n" +
                "• Baccalaureate transcript\n" +
                "• Degree (Licence/Master/Engineer) or equivalent + transcript of the last academic year + diploma supplement (for the degree used to enter the competition)\n" +
                "• National ID card\n" +
                "• Birth certificate\n" +
                "• Certificate of good conduct (added at final registration if required)\n",
            ["calendar"] = "Competition Calendar (2025–2026):",
            ["seats"] =
                "Seats allocation: 1/4 Maghreb & French-speaking Africa; 1/4 social-security fund cadres; 1/2 Algerian students outside the funds.",
            ["contact"] = "Contact: Phone 023 06 76 16 — Email contact@esss.dz",
            ["choose_lang"] = "Please pick your language:",
            ["picked_lang"] = "Language updated ✅",
            ["menu_about"] = "ℹ️ About",
            ["menu_programs"] = "📚 Programs",
            ["menu_concours"] = "📝 Concours",
            ["menu_calendar"] = "🗓️ Calendar",
            ["menu_eligibility"] = "📎 Eligibility",
            ["menu_contact"] = "📞 Contact",
            ["menu_website"] = "🌐 Website",
            ["menu_lang"] = "🔄 Language",
            ["help"] =
                "Commands:\n" +
                "/start – menu\n/help – this help\n/lang – change language\n/about /programs /concours /calendar /eligibility /contact /website",
            ["link_inscription"] = "Register online (official portal)",
            ["link_website"] = "Open official website",
            ["home"] = "🏠 Home",
        },

        // Français
        ["fr"] = new()
        {
            ["welcome"] =
                "Bienvenue sur le bot ESSS ! Choisissez une option ci-dessous.",
            ["about"] =
                "L’École Supérieure de la Sécurité Sociale (ESSS), créée en 2012 à Ben Aknoun (Alger), est un établissement public qui assure la formation initiale et continue au profit des organismes de sécurité sociale et contribue aux études et à la coopération internationale.",
            ["programs_header"] = "Masters Professionnels (2025–2026) :",
            ["programs_list"] =
                "• Droit de la Protection Sociale\n" +
                "• Administration & Ressources Humaines (Management)\n" +
                "• Systèmes d’Information & Transformation Digitale\n" +
                "• Risque & Finance (Actuariat/Quant)\n",
            ["concours_header"] = "Concours National d’accès (Master, 2025–2026)",
            ["concours_phases"] =
                "Sélection en deux phases :\n" +
                "1) Classement (sur dossier) : coefficient d’adéquation diplôme/spécialité (selon tableau) + moyenne de la dernière année universitaire (pondérée).\n" +
                "2) Épreuves : écrit puis oral pour les admissibles après l’écrit.",
            ["eligibility_intro"] =
                "Éligibilité & coefficients d’adéquation (exemples, voir tableaux). Le classement utilise :\n" +
                "• Coefficient d’adéquation : dépend de la concordance diplôme/spécialité avec le master choisi (voir tableaux des poids).\n" +
                "• Moyenne de dernière année : pondérée dans le classement (voir l’avis).",
            ["eligibility_tables"] = "Tableaux des coefficients par parcours :",
            ["dossier"] =
                "Dossier (à fusionner en un seul fichier lors de l’inscription en ligne ; originaux à l’inscription finale) :\n" +
                "• Relevé du baccalauréat\n" +
                "• Diplôme (Licence/Master/Ingénieur) ou équivalent + relevé de la dernière année universitaire + supplément au diplôme (pour le diplôme utilisé au concours)\n" +
                "• Carte nationale d’identité\n" +
                "• Extrait de naissance\n" +
                "• Certificat de bonne conduite (ajouté lors de l’inscription finale si requis)\n",
            ["calendar"] = "Calendrier du concours (2025–2026) :",
            ["seats"] =
                "Répartition des places : 1/4 pour le Maghreb & l’Afrique francophone ; 1/4 pour les cadres des caisses de sécurité sociale ; 1/2 pour les étudiants algériens hors caisses.",
            ["contact"] = "Contact : Tél. 023 06 76 16 — Email contact@esss.dz",
            ["choose_lang"] = "Choisissez votre langue :",
            ["picked_lang"] = "Langue mise à jour ✅",
            ["menu_about"] = "ℹ️ À propos",
            ["menu_programs"] = "📚 Programmes",
            ["menu_concours"] = "📝 Concours",
            ["menu_calendar"] = "🗓️ Calendrier",
            ["menu_eligibility"] = "📎 Éligibilité",
            ["menu_contact"] = "📞 Contact",
            ["menu_website"] = "🌐 Site Web",
            ["menu_lang"] = "🔄 Langue",
            ["help"] =
                "Commandes :\n" +
                "/start – menu\n/help – aide\n/lang – changer de langue\n/about /programs /concours /calendar /eligibility /contact /website",
            ["link_inscription"] = "S’inscrire en ligne (portail officiel)",
            ["link_website"] = "Ouvrir le site officiel",
            ["home"] = "🏠 Accueil",
        },

        // العربية
        ["ar"] = new()
        {
            ["welcome"] =
                "مرحبًا بكم في روبوت ESSS! اختر خيارًا من القائمة.",
            ["about"] =
                "المدرسة العليا للضمان الاجتماعي (ESSS) مؤسسة عمومية أنشئت سنة 2012 ببن عكنون – الجزائر. تقدّم تكوينًا أساسيًا ومستمرًا لفائدة هيئات الضمان الاجتماعي وتساهم في الدراسات والتعاون الدولي.",
            ["programs_header"] = "مسارات الماستر المهني (2025–2026):",
            ["programs_list"] =
                "• قانون الحماية الاجتماعية\n" +
                "• الإدارة والموارد البشرية (تسيير)\n" +
                "• نظم المعلومات والتحول الرقمي\n" +
                "• حساب المخاطرة والمالية (اكتواري/كمي)\n",
            ["concours_header"] = "مسابقة الالتحاق (ماستر 2025–2026)",
            ["concours_phases"] =
                "الانتقاء على مرحلتين:\n" +
                "1) ترتيب بالملف: معامل مواءمة الشهادة مع التخصص المختار (حسب الجداول) + معدل السنة الجامعية الأخيرة (مُوزن).\n" +
                "2) امتحانات: كتابي ثم شفهي للناجحين في الكتابي.",
            ["eligibility_intro"] =
                "شروط القبول ومعاملات المواءمة (أمثلة؛ راجع الجداول أدناه). يعتمد الترتيب على:\n" +
                "• معامل المواءمة: وفق توافق الشهادة/التخصص مع الماستر المختار (جداول الأوزان).\n" +
                "• معدل السنة الأخيرة: يُؤخذ بوزن محدد في الترتيب (حسب الإعلان).",
            ["eligibility_tables"] = "جداول معاملات المواءمة حسب المسار:",
            ["dossier"] =
                "ملف الترشح (يُدمج في ملف واحد عند التسجيل الإلكتروني؛ تُقدَّم الأصول عند التسجيل النهائي):\n" +
                "• نسخة من كشف نقاط شهادة البكالوريا\n" +
                "• شهادة (ليسانس/ماستر/مهندس دولة) أو شهادة معادلة + كشف نقاط السنة الجامعية الأخيرة + الملحق الوصفي للشهادة (المستعملة للمسابقة)\n" +
                "• نسخة من بطاقة التعريف الوطنية\n" +
                "• نسخة من شهادة الميلاد\n" +
                "• شهادة حسن السيرة والسلوك (تُستكمل عند التسجيل النهائي عند الاقتضاء)\n",
            ["calendar"] = "رزنامة المسابقة (2025–2026):",
            ["seats"] =
                "توزيع المقاعد: 1/4 لبلدان المغرب العربي وإفريقيا الناطقة بالفرنسية، و1/4 لإطارات صناديق الضمان الاجتماعي، و1/2 للطلبة الجزائريين خارج الصناديق.",
            ["contact"] = "الهاتف: 023 06 76 16 — البريد: contact@esss.dz",
            ["choose_lang"] = "اختر لغتك:",
            ["picked_lang"] = "تم تحديث اللغة ✅",
            ["menu_about"] = "ℹ️ حول",
            ["menu_programs"] = "📚 البرامج",
            ["menu_concours"] = "📝 المسابقة",
            ["menu_calendar"] = "🗓️ الرزنامة",
            ["menu_eligibility"] = "📎 شروط القبول",
            ["menu_contact"] = "📞 اتصال",
            ["menu_website"] = "🌐 الموقع",
            ["menu_lang"] = "🔄 تغيير اللغة",
            ["help"] =
                "أوامر:\n" +
                "/start – القائمة\n/help – المساعدة\n/lang – تغيير اللغة\n/about /programs /concours /calendar /eligibility /contact /website",
            ["link_inscription"] = "التسجيل الإلكتروني (البوابة الرسمية)",
            ["link_website"] = "فتح الموقع الرسمي",
            ["home"] = "🏠 الرئيسية",
        }
    };

    // === ENTRYPOINT ===
    public static async Task Main()
    {
        if (BotToken == "REPLACE_ME_WITH_ENV")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️ Set TELEGRAM_BOT_TOKEN env var before running.");
            Console.ResetColor();
        }

        var bot = new TelegramBotClient(BotToken);
        var me = await bot.GetMe();
        Console.WriteLine($"ESSS DZ Bot ready as @{me.Username}");

        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };
        bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);

        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    // === UPDATE ROUTER ===
    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } cb)
            {
                await HandleCallback(bot, cb, ct);
                return;
            }

            if (update.Message is not { } msg) return;

            // Create user profile if first time
            if (msg.From is { } u && !Users.ContainsKey(u.Id))
            {
                var guessed = MapTelegramLang(u.LanguageCode);
                Users[u.Id] = new UserInfo(u.Username ?? "", u.FirstName ?? "", u.LastName ?? "", guessed);
                if (!UserLangByChat.ContainsKey(msg.Chat.Id)) UserLangByChat[msg.Chat.Id] = guessed;
            }

            var chatId = msg.Chat.Id;
            var lang = UserLangByChat.TryGetValue(chatId, out var v) ? v : "en";
            var text = (msg.Text ?? "").Trim();

            switch (text)
            {
                case "/start":
                    if (!UserLangByChat.ContainsKey(chatId))
                        UserLangByChat[chatId] = GuessLangFromUser(msg.From);
                    await SendWelcome(bot, chatId, UserLangByChat[chatId], ct);
                    break;

                case "/help":
                    await bot.SendTextMessageAsync(chatId, Txt[lang]["help"], replyMarkup: NavBar(lang), cancellationToken: ct);
                    break;

                case "/lang":
                case "🔄 Language":
                case "🔄 Langue":
                case "🔄 تغيير اللغة":
                    await SendLangPicker(bot, chatId, lang, ct);
                    break;

                case "/about":
                case "ℹ️ About":
                case "ℹ️ À propos":
                case "ℹ️ حول":
                    await SendAbout(bot, chatId, lang, ct);
                    break;

                case "/programs":
                case "📚 Programs":
                case "📚 Programmes":
                case "📚 البرامج":
                    await SendPrograms(bot, chatId, lang, ct);
                    break;

                case "/concours":
                case "📝 Concours":
                case "📝 Concours (Master)":
                case "📝 المسابقة":
                case "📝 المسابقة (ماستر)":
                    await SendConcours(bot, chatId, lang, ct);
                    break;

                case "/calendar":
                case "🗓️ Calendar":
                case "🗓️ Calendrier":
                case "🗓️ الرزنامة":
                    await SendCalendar(bot, chatId, lang, ct);
                    break;

                case "/eligibility":
                case "📎 Eligibility":
                case "📎 Éligibilité":
                case "📎 شروط القبول":
                    await SendEligibility(bot, chatId, lang, ct);
                    break;

                case "/contact":
                case "📞 Contact":
                case "📞 اتصل بنا":
                    await SendContact(bot, chatId, lang, ct);
                    break;

                case "/website":
                case "🌐 Website":
                case "🌐 Site Web":
                case "🌐 الموقع":
                    await SendWebsite(bot, chatId, lang, ct);
                    break;

                default:
                    await SendWelcome(bot, chatId, lang, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(bot, ex, ct);
        }
    }

    // === CALLBACK HANDLER ===
    private static async Task HandleCallback(ITelegramBotClient bot, CallbackQuery cb, CancellationToken ct)
    {
        var chatId = cb.Message?.Chat.Id ?? cb.From.Id;
        var data = cb.Data ?? "";

        if (data.StartsWith("lang:"))
        {
            var newLang = data["lang:".Length..];
            if (!Langs.Contains(newLang)) newLang = "en";

            UserLangByChat[chatId] = newLang;
            if (Users.TryGetValue(cb.From.Id, out var info))
                Users[cb.From.Id] = info with { Language = newLang };

            await bot.AnswerCallbackQueryAsync(cb.Id, Txt[newLang]["picked_lang"], cancellationToken: ct);
            await SendWelcome(bot, chatId, newLang, ct);
            return;
        }

        // Inline navbar routes
        var route = data switch
        {
            "goto:home"       => (Func<Task>)(() => SendWelcome(bot, chatId, Lang(chatId), ct)),
            "goto:programs"   => () => SendPrograms(bot, chatId, Lang(chatId), ct),
            "goto:concours"   => () => SendConcours(bot, chatId, Lang(chatId), ct),
            "goto:calendar"   => () => SendCalendar(bot, chatId, Lang(chatId), ct),
            "goto:eligibility"=> () => SendEligibility(bot, chatId, Lang(chatId), ct),
            "goto:lang"       => () => SendLangPicker(bot, chatId, Lang(chatId), ct),
            _                 => () => Task.CompletedTask
        };
        await route();
    }

    // === VIEWS ===
    private static async Task SendWelcome(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        await bot.SendTextMessageAsync(
            chatId,
            Txt[lang]["welcome"],
            replyMarkup: NavBar(lang),
            cancellationToken: ct
        );
    }

    private static async Task SendLangPicker(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var ikb = new InlineKeyboardMarkup(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("🇬🇧 English", "lang:en"),
                InlineKeyboardButton.WithCallbackData("🇫🇷 Français", "lang:fr"),
                InlineKeyboardButton.WithCallbackData("🇩🇿 العربية", "lang:ar"),
            }
        });
        await bot.SendTextMessageAsync(chatId, Txt[lang]["choose_lang"], replyMarkup: ikb, cancellationToken: ct);
    }

    private static async Task SendAbout(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine(Txt[lang]["about"]);
        b.AppendLine();
        b.AppendLine(Txt[lang]["seats"]); // seats paragraph
        b.AppendLine();
        b.AppendLine($"• {Txt[lang]["link_website"]}: {Links.Website}");
        await bot.SendTextMessageAsync(chatId, b.ToString(), replyMarkup: NavBar(lang), cancellationToken: ct);
    }

    private static async Task SendPrograms(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine(Txt[lang]["programs_header"]);
        b.AppendLine(Txt[lang]["programs_list"]);
        await bot.SendTextMessageAsync(chatId, b.ToString(), replyMarkup: NavBar(lang), cancellationToken: ct);
    }

    private static async Task SendConcours(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine($"<b>{Txt[lang]["concours_header"]}</b>");
        b.AppendLine();
        b.AppendLine(Txt[lang]["seats"]);
        b.AppendLine();
        b.AppendLine(Txt[lang]["concours_phases"]);
        b.AppendLine();
        b.AppendLine($"<b>{Title(lang, "Dossier")}</b>");
        b.AppendLine(Txt[lang]["dossier"]);
        b.AppendLine();
        b.AppendLine($"<b>{Title(lang, "Portal")}</b>");
        b.AppendLine($"{Txt[lang]["link_inscription"]}: {Links.Inscription}");

        var ikb = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("📝 Portal", Links.Inscription),
                InlineKeyboardButton.WithUrl("🌐 Website", Links.Website),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏠", "goto:home"),
                InlineKeyboardButton.WithCallbackData("📚", "goto:programs"),
                InlineKeyboardButton.WithCallbackData("📝", "goto:concours"),
                InlineKeyboardButton.WithCallbackData("🗓️", "goto:calendar"),
                InlineKeyboardButton.WithCallbackData("📎", "goto:eligibility"),
                InlineKeyboardButton.WithCallbackData("🔄", "goto:lang"),
            }
        });

        await bot.SendTextMessageAsync(chatId, b.ToString(), parseMode: ParseMode.Html, replyMarkup: ikb, cancellationToken: ct);
    }

    private static async Task SendCalendar(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine($"<b>{Txt[lang]["calendar"]}</b>");

        foreach (var item in Admissions.Calendar)
        {
            var dateTxt = item.EndDate is null
                ? item.StartDate.ToString("dd/MM/yyyy")
                : $"{item.StartDate:dd/MM/yyyy} → {item.EndDate:dd/MM/yyyy}";
            b.AppendLine($"• {dateTxt} — {item.Label(lang)}");
        }

        await bot.SendTextMessageAsync(chatId, b.ToString(), parseMode: ParseMode.Html, replyMarkup: NavBar(lang), cancellationToken: ct);
    }

    private static async Task SendEligibility(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine($"<b>{Txt[lang]["eligibility_tables"]}</b>");
        b.AppendLine();
        b.AppendLine(Txt[lang]["eligibility_intro"]);
        b.AppendLine();

        foreach (var track in Admissions.Tracks)
        {
            b.AppendLine($"<b>• {track.Title(lang)}</b>");
            foreach (var row in track.Rows)
            {
                b.AppendLine($"  — {row.Field(lang)}: <code>{row.Weight:0.0}</code>");
            }
            b.AppendLine();
        }

        await bot.SendTextMessageAsync(chatId, b.ToString(), parseMode: ParseMode.Html, replyMarkup: NavBar(lang), cancellationToken: ct);
    }

    private static async Task SendContact(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        await bot.SendTextMessageAsync(chatId, Txt[lang]["contact"], replyMarkup: NavBar(lang), cancellationToken: ct);
    }

    private static async Task SendWebsite(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var ikb = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl(Txt[lang]["link_website"], Links.Website) },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(Txt[lang]["home"], "goto:home"),
                InlineKeyboardButton.WithCallbackData(Txt[lang]["menu_programs"], "goto:programs")
            }
        });
        await bot.SendTextMessageAsync(chatId, Links.Website, replyMarkup: ikb, cancellationToken: ct);
    }

    // === NAVBAR ===
    private static InlineKeyboardMarkup NavBar(string lang) => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(Txt[lang]["home"], "goto:home"),
            InlineKeyboardButton.WithCallbackData(Txt[lang]["menu_programs"], "goto:programs"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(Txt[lang]["menu_concours"], "goto:concours"),
            InlineKeyboardButton.WithCallbackData(Txt[lang]["menu_calendar"], "goto:calendar"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(Txt[lang]["menu_eligibility"], "goto:eligibility"),
            InlineKeyboardButton.WithUrl(Txt[lang]["menu_website"], Links.Website),
            InlineKeyboardButton.WithCallbackData(Txt[lang]["menu_lang"], "goto:lang"),
        }
    });

    private static string Title(string lang, string key) => (lang switch
    {
        "en" => key switch
        {
            "Dossier" => "Application dossier",
            "Portal" => "Online registration",
            _ => key
        },
        "fr" => key switch
        {
            "Dossier" => "Dossier de candidature",
            "Portal" => "Inscription en ligne",
            _ => key
        },
        "ar" => key switch
        {
            "Dossier" => "ملف الترشح",
            "Portal" => "التسجيل الإلكتروني",
            _ => key
        },
        _ => key
    });

    private static string Lang(long chatId) => UserLangByChat.TryGetValue(chatId, out var v) ? v : "en";

    // === ERROR HANDLER ===
    private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
    {
        var msg = ex switch
        {
            ApiRequestException api => $"Telegram API error [{api.ErrorCode}] {api.Message}",
            TaskCanceledException => "Request timed out.",
            _ => ex.ToString()
        };
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
        return Task.CompletedTask;
    }

    // === Helpers ===
    private static string GuessLangFromUser(User? u)
    {
        var guessed = MapTelegramLang(u?.LanguageCode);
        return Langs.Contains(guessed) ? guessed : "en";
    }

    private static string MapTelegramLang(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "en";
        code = code.ToLowerInvariant();
        if (code.StartsWith("fr")) return "fr";
        if (code.StartsWith("ar")) return "ar";
        return "en";
    }

    // === Data types ===
    private record UserInfo(string Username, string FirstName, string LastName, string Language);

    private sealed class AdmissionsData
    {
        // ————— Calendar (2025–2026) —————
        // Based on the uploaded Arabic notice table (with exact ranges and activities).
        public IReadOnlyList<CalItem> Calendar { get; } = new[]
        {
            // 01: Portal opens 13/10 → 24/10
            new CalItem(new(2025,10,13), new(2025,10,24), new()
            {
                ["en"] = "Online registration portal open",
                ["fr"] = "Ouverture du portail d’inscription en ligne",
                ["ar"] = "فتح موقع التسجيل الإلكتروني"
            }),
            // 02: File review 25/10 → 26/10
            new CalItem(new(2025,10,25), new(2025,10,26), new()
            {
                ["en"] = "Review of applications",
                ["fr"] = "Étude des dossiers des candidats",
                ["ar"] = "دراسة ملفات المترشحين"
            }),
            // 03: Admitted list to the competition 26/10
            new CalItem(new(2025,10,26), null, new()
            {
                ["en"] = "Announcement: list admitted to sit the competition",
                ["fr"] = "Annonce : liste des candidats admis à concourir",
                ["ar"] = "الإعلان عن قائمة المقبولين لاجتياز المسابقة"
            }),
            // 04: Appeals (online) 26/10 → 28/10
            new CalItem(new(2025,10,26), new(2025,10,28), new()
            {
                ["en"] = "Online appeals submissions",
                ["fr"] = "Dépôt des recours en ligne",
                ["ar"] = "تقديم الطعون إلكترونيا"
            }),
            // 05: Appeals review + final admitted list (written) 29/10
            new CalItem(new(2025,10,29), null, new()
            {
                ["en"] = "Appeals review & list admitted to the written exam",
                ["fr"] = "Étude des recours & liste des admis à l’écrit",
                ["ar"] = "دراسة الطعون والإعلان عن قائمة المقبولين لاجتياز الامتحان الكتابي"
            }),
            // 06: Written exam 08/11
            new CalItem(new(2025,11,08), null, new()
            {
                ["en"] = "Written exam",
                ["fr"] = "Épreuve écrite",
                ["ar"] = "الامتحان الكتابي"
            }),
            // 07: Marking written papers 09/11 → 11/11
            new CalItem(new(2025,11,09), new(2025,11,11), new()
            {
                ["en"] = "Marking of written exam papers",
                ["fr"] = "Correction des copies de l’écrit",
                ["ar"] = "تصحيح أوراق الامتحانات الكتابية"
            }),
            // 08: Deliberations & written results 12/11
            new CalItem(new(2025,11,12), null, new()
            {
                ["en"] = "Deliberations & list of successful candidates (written)",
                ["fr"] = "Délibérations & publication des admis à l’écrit",
                ["ar"] = "المداولات ونشر القائمة الاسمية للناجحين في المسابقة الكتابية"
            }),
            // 09: Oral appeals 12/11 → 14/11
            new CalItem(new(2025,11,12), new(2025,11,14), new()
            {
                ["en"] = "Online appeals (oral stage)",
                ["fr"] = "Dépôt des recours (phase orale)",
                ["ar"] = "تقديم الطعون إلكترونيا (مرحلة الشفهي)"
            }),
            // 10: Appeals review & admitted to oral 15/11
            new CalItem(new(2025,11,15), null, new()
            {
                ["en"] = "Appeals review & list admitted to oral exam",
                ["fr"] = "Étude des recours & liste des admis à l’oral",
                ["ar"] = "دراسة الطعون والإعلان عن قائمة المقبولين لاجتياز الامتحان الشفهي"
            }),
            // 11: Oral exam 17/11
            new CalItem(new(2025,11,17), null, new()
            {
                ["en"] = "Oral exam",
                ["fr"] = "Épreuve orale",
                ["ar"] = "الامتحان الشفهي"
            }),
            // 12: Final deliberations & results 18/11
            new CalItem(new(2025,11,18), null, new()
            {
                ["en"] = "Final deliberations & publication of final results",
                ["fr"] = "Délibérations finales & publication des résultats",
                ["ar"] = "المداولات والمصادقة على القائمة الاسمية للناجحين وإعلان النتائج النهائية"
            }),
            // 13: Final pedagogical registration 20/11
            new CalItem(new(2025,11,20), null, new()
            {
                ["en"] = "Final pedagogical registration",
                ["fr"] = "Inscription pédagogique finale",
                ["ar"] = "التسجيل البيداغوجي النهائي"
            }),
            // 14: Start of academic year (M1 2025/26) 23/11
            new CalItem(new(2025,11,23), null, new()
            {
                ["en"] = "Start of academic year (Master 1, 2025/26)",
                ["fr"] = "Rentrée (Master 1, 2025/26)",
                ["ar"] = "الدخول الجامعي لطلبة السنة الأولى ماستر 2025/2026"
            }),
        };

        // ————— Eligibility weight tables (alignment coefficients) —————
        // Short, readable subsets extracted from the Arabic tables.
        public IReadOnlyList<Track> Tracks { get; } = new[]
        {
            new Track(
                new() { ["en"] = "Social Protection Law", ["fr"] = "Droit de la Protection Sociale", ["ar"] = "قانون الحماية الاجتماعية" },
                new[]
                {
                    new Row(
                        new() {
                            ["en"]="Licence in Law (Public/Private) or equivalent (Law)",
                            ["fr"]="Licence en Droit (public/privé) ou équivalent (Droit)",
                            ["ar"]="ليسانس في الحقوق (عام/خاص) أو شهادة معادلة ضمن ميدان الحقوق "
                        }, 0.8),
                    // Other closely-related legal sub-fields can be added as needed (weights typically ≥0.6)
                }
            ),
            new Track(
                new() { ["en"] = "Administration & HR (Management)", ["fr"] = "Administration & RH (Management)", ["ar"] = "الإدارة والموارد البشرية (تسيير)" },
                new[]
                {
                    new Row(new() { ["en"]="Economics/Management/Commercial sciences (Licence)", ["fr"]="Économie/Gestion/Sciences commerciales (Licence)", ["ar"]="علوم اقتصادية/التسيير/العلوم التجارية (ليسانس)" }, 0.8),
                    new Row(new() { ["en"]="Accounting, Audit & Tax", ["fr"]="Comptabilité, Audit & Fiscalité", ["ar"]="محاسبة، وتدقيق وجباية" }, 0.4),
                    new Row(new() { ["en"]="Quantitative Economics", ["fr"]="Économie quantitative", ["ar"]="اقتصاد كمي" }, 0.2),
                    new Row(new() { ["en"]="Budget Management / Corporate Finance / HR", ["fr"]="Gestion budgétaire / Finance d’entreprise / RH", ["ar"]="تسيير الميزانية / مالية المؤسسة / تسيير الموارد البشرية" }, 0.4),
                    new Row(new() { ["en"]="Banking/Insurance/Finance", ["fr"]="Banques, Assurances, Finance", ["ar"]="مالية، بنوك والتأمينات" }, 0.4),
                    new Row(new() { ["en"]="Management (general)", ["fr"]="Management (général)", ["ar"]="إدارة العمال (مانجمنت)" }, 0.8),
                }
            ),
            new Track(
                new() { ["en"]="Information Systems & Digital Transformation", ["fr"]="Systèmes d’Information & Transformation Digitale", ["ar"]="نظم المعلومات والتحول الرقمي" },
                new[]
                {
                    new Row(new() { ["en"]="Computer Science / IS / Software Engineering", ["fr"]="Informatique / SI / Génie logiciel", ["ar"]="إعلام آلي / نظم المعلومات / هندسة البرمجيات" }, 0.8),
                    new Row(new() { ["en"]="Applied Mathematics + CS", ["fr"]="Maths appliquées + Info", ["ar"]="رياضيات تطبيقية وإعلام آلي" }, 0.6),
                    new Row(new() { ["en"]="Information Systems & Web / Web Development", ["fr"]="Systèmes d’information & Web / Développement Web", ["ar"]="نظم المعلومات والويب / تطوير برمجيات وتكنولوجيا الويب" }, 0.8),
                    new Row(new() { ["en"]="ICT / Communication Technologies", ["fr"]="STIC / TIC", ["ar"]="علوم وتكنولوجيا الإعلام والاتصال" }, 0.6),
                }
            ),
            new Track(
                new() { ["en"]="Risk & Finance (Actuarial/Quant)", ["fr"]="Risque & Finance (Actuariat/Quant)", ["ar"]="حساب المخاطرة والمالية" },
                new[]
                {
                    new Row(new() { ["en"]="Mathematics / Applied Mathematics", ["fr"]="Mathématiques / Mathématiques appliquées", ["ar"]="رياضيات / رياضيات تطبيقية" }, 0.8),
                    new Row(new() { ["en"]="Statistics / Applied Statistics / Statistical Engineering", ["fr"]="Statistique / Statistique appliquée / Ingénierie statistique", ["ar"]="الإحصاء / الإحصاء التطبيقي / الهندسة الإحصائية" }, 0.6),
                    new Row(new() { ["en"]="Probability & Statistics / Data (Stats & CS)", ["fr"]="Probabilités & statistiques / Données (Stats & Info)", ["ar"]="احتمالات وإحصاء / إحصاء ومعالجة الإعلام الآلي للمعطيات" }, 0.4),
                    new Row(new() { ["en"]="Operations Research / Stochastic Modelling", ["fr"]="Recherche opérationnelle / Modélisation stochastique", ["ar"]="بحوث العمليات / نمذجة عشوائية" }, 0.4),
                    new Row(new() { ["en"]="Financial Mathematics / Risk & Finance", ["fr"]="Maths financières / Risque & Finance", ["ar"]="رياضيات مالية / حساب المخاطرة والمالية" }, 0.8),
                    new Row(new() { ["en"]="Economics/Management/Accounting (relevant majors)", ["fr"]="Économie/Gestion/Comptabilité (spécialités pertinentes)", ["ar"]="علوم اقتصادية/التسيير/محاسبة (تخصصات ملائمة)" }, 0.8),
                }
            ),
        };

        // ————— Seats paragraph (shown in /about and /concours) —————
        public string SeatsBlurb(string lang) => lang switch
        {
            "fr" => "Répartition des places : 1/4 Maghreb & Afrique francophone ; 1/4 cadres des caisses de sécurité sociale ; 1/2 étudiants algériens hors caisses.",
            "ar" => "توزيع المقاعد: ربع لبلدان المغرب العربي وإفريقيا الناطقة بالفرنسية، وربع لإطارات صناديق الضمان الاجتماعي، والنصف للطلبة الجزائريين خارج الصناديق.",
            _    => "Seats allocation: 1/4 Maghreb & French-speaking Africa; 1/4 social-security fund cadres; 1/2 Algerian students outside the funds."
        };

        // ————— Types —————
        public sealed record CalItem(DateTime StartDate, DateTime? EndDate, Dictionary<string,string> Labels)
        {
            public string Label(string lang) => Labels.TryGetValue(lang, out var v) ? v : Labels["en"];
        }

        public sealed record Track(Dictionary<string,string> TitleMap, Row[] Rows)
        {
            public string Title(string lang) => TitleMap.TryGetValue(lang, out var v) ? v : TitleMap["en"];
        }

        public sealed record Row(Dictionary<string,string> FieldMap, double Weight)
        {
            public string Field(string lang) => FieldMap.TryGetValue(lang, out var v) ? v : FieldMap["en"];
        }
    }
}

/*
README / Notes (data provenance)

• Calendar, dossier items, phases (file-based ranking + written + oral), eligibility tables with alignment weights,
  and the seats allocation rule are taken directly from the Arabic ESSS concours notice for 2025–2026 (pages with tables and schedule).
  Source files were provided by the user.

  Seats paragraph reference: :contentReference[oaicite:0]{index=0}
  Eligibility (tracks + example weights) & dossier list & phases: :contentReference[oaicite:1]{index=1}
  Full calendar (dates & ranges) exactly as published: :contentReference[oaicite:2]{index=2}

• An ancillary scan was also provided (CamScanner), not containing additional structured data we needed for the bot copy: :contentReference[oaicite:3]{index=3}
*/

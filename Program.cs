// Program.cs — ESSS DZ Bot (pro, single-file)
// TargetFramework: net8.0
// NuGet: Telegram.Bot (22.3.0)
// Run with: TELEGRAM_BOT_TOKEN=xxxxx dotnet run

using System.Globalization;
using System.Net.Http.Headers;
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
        ?? "8409133925:AAFJ-ExOjEKREIgrtIkwhjjsMZxp7Y_4gR0"; // <— safer than hardcoding

    // --- Minimal persistence (memory) ---
    private static readonly Dictionary<long, string> UserLang = new();          // chatId -> "en|fr|ar"
    private static readonly Dictionary<long, UserInfo> Users = new();           // userId -> info
    private static readonly HashSet<long> Admins = new();                       // optional: add your chat IDs

    // --- HTTP for link health / future fetches (kept simple) ---
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    // --- Supported languages ---
    private static readonly string[] Langs = ["en", "fr", "ar"];

    // --- School links (official / authoritative) ---
    private static class Links
    {
        public const string Website = "https://www.esss.dz/"; // site officiel (can be intermittently slow)
        public const string Inscription = "https://inscription.esss.dz/candidat/inscription"; // portail concours
        public const string MinistryNews = "https://www.mtess.gov.dz/"; // contexte général
    }

    // --- Canonical admissions (from your PDF 2025–2026) ---
    // Source: "مسابقة الالتحاق... 2025-2026" (uploaded). Keep this centralized.
    private static readonly AdmissionsData Admissions = new();

    // --- Translations (concise, bot-optimized) ---
    private static Dictionary<string, T> TStr<T>() where T : notnull => new();

    private static readonly Dictionary<string, Dictionary<string, string>> Txt = new()
    {
        // English
        ["en"] = new()
        {
            ["welcome"] = "Welcome to the ESSS Bot! Choose an option below.",
            ["about"] =
                "The Higher School of Social Security (ESSS) is a public higher education institution created in 2012 in Ben Aknoun, Algiers. It provides initial and continuing training for social security organizations and contributes to studies and international cooperation.",
            ["programs_header"] = "Master Professional Tracks (2025–2026):",
            ["programs_list"] =
                "• Social Protection Law\n" +
                "• Administration & HR (Management)\n" +
                "• Information Systems & Digital Transformation\n" +
                "• Risk & Finance (Actuarial/Quant)\n",
            ["concours_header"] = "National Entrance Competition (Master, 2025–2026)",
            ["concours_phases"] =
                "Selection in two phases:\n" +
                "1) Ranking (document review): degree match with the chosen specialty + last academic year average (weighting applies).\n" +
                "2) Exams: Written, then oral for shortlisted candidates.",
            ["eligibility"] =
                "Eligibility (degree alignment examples):\n" +
                "• Social Protection Law: Bachelor in Law/Political Science (etc.)\n" +
                "• Administration & HR: Economics, Management, Finance, Accounting, HR, etc.\n" +
                "• Info Systems & Digital: CS, IS, Software Eng., Applied Math, Web/IS, etc.\n" +
                "• Risk & Finance: Math/Stats/Actuarial/OR; also Economics/Management (per table).",
            ["dossier"] =
                "Required documents (upload as a single merged file at registration; originals at final enrollment):\n" +
                "• Baccalaureate transcript\n" +
                "• Degree (Licence/Master/Engineer) + transcript of the last year + diploma supplement\n" +
                "• National ID, Birth certificate",
            ["calendar"] = "Competition Calendar (2025–2026):",
            ["contact"] = "Contact: Phone 023 06 76 16 — Email contact@esss.dz",
            ["choose_lang"] = "Please pick your language:",
            ["picked_lang"] = "Language updated ✅",
            ["menu_main"] = "Main Menu",
            ["menu_about"] = "ℹ️ About",
            ["menu_programs"] = "📚 Programs",
            ["menu_concours"] = "📝 Concours (Master)",
            ["menu_calendar"] = "🗓️ Calendar",
            ["menu_contact"] = "📞 Contact",
            ["menu_website"] = "🌐 Website",
            ["menu_lang"] = "🔄 Language",
            ["help"] = "Commands:\n/start – menu\n/help – this help\n/lang – change language\n/about /programs /concours /calendar /contact /website",
            ["link_inscription"] = "Register online (official portal)",
            ["link_website"] = "Open official website",
        },

        // Français
        ["fr"] = new()
        {
            ["welcome"] = "Bienvenue sur le bot ESSS ! Choisissez une option ci-dessous.",
            ["about"] =
                "L’École Supérieure de la Sécurité Sociale (ESSS), créée en 2012 à Ben Aknoun (Alger), est un établissement public qui assure la formation initiale et continue au profit des organismes de sécurité sociale et mène des études et coopérations internationales.",
            ["programs_header"] = "Masters Professionnels (2025–2026) :",
            ["programs_list"] =
                "• Droit de la Protection Sociale\n" +
                "• Administration & RH (Management)\n" +
                "• Systèmes d’Information & Transformation Digitale\n" +
                "• Risque & Finance (Actuariat/Quant)\n",
            ["concours_header"] = "Concours National d’accès (Master, 2025–2026)",
            ["concours_phases"] =
                "Sélection en deux phases :\n" +
                "1) Classement (dossier) : adéquation diplôme/spécialité + moyenne de l’année universitaire (pondérations).\n" +
                "2) Épreuves : écrit puis oral pour les admissibles.",
            ["eligibility"] =
                "Éligibilité (exemples d’adéquation) :\n" +
                "• Droit de la Protection Sociale : Licence en Droit/Sciences politiques (etc.)\n" +
                "• Administration & RH : Économie, Gestion, Finance, Comptabilité, RH, etc.\n" +
                "• SI & Digital : Info, SI, Génie logiciel, Maths appliquées, Web/SI, etc.\n" +
                "• Risque & Finance : Maths/Stats/Actuariat/RO ; aussi Économie/Gestion (selon tableau).",
            ["dossier"] =
                "Dossier (à téléverser en un seul fichier fusionné lors de l’inscription ; originaux lors de l’inscription finale) :\n" +
                "• Relevé du Bac\n" +
                "• Diplôme (Licence/Master/Ingénieur) + relevé de la dernière année + supplément au diplôme\n" +
                "• Carte nationale, Extrait de naissance",
            ["calendar"] = "Calendrier du concours (2025–2026) :",
            ["contact"] = "Contact : Tél. 023 06 76 16 — Email contact@esss.dz",
            ["choose_lang"] = "Choisissez votre langue :",
            ["picked_lang"] = "Langue mise à jour ✅",
            ["menu_main"] = "Menu principal",
            ["menu_about"] = "ℹ️ À propos",
            ["menu_programs"] = "📚 Programmes",
            ["menu_concours"] = "📝 Concours (Master)",
            ["menu_calendar"] = "🗓️ Calendrier",
            ["menu_contact"] = "📞 Contact",
            ["menu_website"] = "🌐 Site Web",
            ["menu_lang"] = "🔄 Langue",
            ["help"] = "Commandes :\n/start – menu\n/help – aide\n/lang – changer de langue\n/about /programs /concours /calendar /contact /website",
            ["link_inscription"] = "S’inscrire en ligne (portail officiel)",
            ["link_website"] = "Ouvrir le site officiel",
        },

        // العربية
        ["ar"] = new()
        {
            ["welcome"] = "مرحبًا بكم في روبوت ESSS! اختر خيارًا من القائمة.",
            ["about"] =
                "المدرسة العليا للضمان الاجتماعي (ESSS) مؤسسة عمومية أُنشئت سنة 2012 ببن عكنون – الجزائر. تُقدم تكوينًا أساسيًا ومُستمرًا لفائدة هيئات الضمان الاجتماعي وتُسهم في الدراسات والتعاون الدولي.",
            ["programs_header"] = "مسارات الماستر المهني (2025–2026):",
            ["programs_list"] =
                "• قانون الحماية الاجتماعية\n" +
                "• الإدارة والموارد البشرية (تسيير)\n" +
                "• نظم المعلومات والتحول الرقمي\n" +
                "• حساب المخاطرة والمالية (إكتواري/كمي)\n",
            ["concours_header"] = "مسابقة الالتحاق (ماستر 2025–2026)",
            ["concours_phases"] =
                "الانتقاء على مرحلتين:\n" +
                "1) ترتيب بالملف: ملاءمة الشهادة مع التخصص + معدل آخر سنة جامعية (معاملات).\n" +
                "2) امتحانات: كتابي ثم شفهي للناجحين في الكتابي.",
            ["eligibility"] =
                "شروط القبول (أمثلة الملاءمة):\n" +
                "• قانون الحماية الاجتماعية: ليسانس قانون/علوم سياسية (…)\n" +
                "• الإدارة والموارد البشرية: اقتصاد، تسيير، مالية، محاسبة، موارد بشرية…\n" +
                "• نظم المعلومات والرقمنة: إعلام آلي، نظم معلومات، هندسة برمجيات، رياضيات تطبيقية، ويب/نظم…\n" +
                "• حساب المخاطرة والمالية: رياضيات/إحصاء/إكتوارية/بحوث عمليات؛ وكذلك اقتصاد/تسيير (حسب الجدول).",
            ["dossier"] =
                "ملف الترشح (يُدمج في ملف واحد عند التسجيل الإلكتروني؛ وتُقدَّم الأصول عند التسجيل النهائي):\n" +
                "• كشف نقاط البكالوريا\n" +
                "• الشهادة (ليسانس/ماستر/مهندس) + كشف نقاط السنة الأخيرة + الملحق الوصفي\n" +
                "• بطاقة التعريف الوطنية، شهادة الميلاد",
            ["calendar"] = "رزنامة المسابقة (2025–2026):",
            ["contact"] = "الهاتف: 023 06 76 16 — البريد: contact@esss.dz",
            ["choose_lang"] = "اختر لغتك:",
            ["picked_lang"] = "تم تحديث اللغة ✅",
            ["menu_main"] = "القائمة الرئيسية",
            ["menu_about"] = "ℹ️ حول",
            ["menu_programs"] = "📚 البرامج",
            ["menu_concours"] = "📝 المسابقة (ماستر)",
            ["menu_calendar"] = "🗓️ الرزنامة",
            ["menu_contact"] = "📞 اتصال",
            ["menu_website"] = "🌐 الموقع",
            ["menu_lang"] = "🔄 تغيير اللغة",
            ["help"] = "أوامر:\n/start – القائمة\n/help – المساعدة\n/lang – تغيير اللغة\n/about /programs /concours /calendar /contact /website",
            ["link_inscription"] = "التسجيل الإلكتروني (البوابة الرسمية)",
            ["link_website"] = "فتح الموقع الرسمي",
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
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

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

            // Track user
            if (msg.From is { } u && !Users.ContainsKey(u.Id))
                Users[u.Id] = new UserInfo(u.Username ?? "", u.FirstName ?? "", u.LastName ?? "", "en");

            // Determine language (by chat)
            var chatId = msg.Chat.Id;
            if (!UserLang.TryGetValue(chatId, out var lang)) lang = "en";

            // Commands or text menu
            var text = msg.Text?.Trim() ?? "";

            switch (text)
            {
                case "/start":
                    await SendWelcome(bot, chatId, lang, ct);
                    break;

                case "/help":
                    await bot.SendTextMessageAsync(chatId, Txt[lang]["help"], cancellationToken: ct);
                    break;

                case "/lang":
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
                case "📝 Concours (Master)":
                case "📝 المسابقة (ماستر)":
                    await SendConcours(bot, chatId, lang, ct);
                    break;

                case "/calendar":
                case "🗓️ Calendar":
                case "🗓️ Calendrier":
                case "🗓️ الرزنامة":
                    await SendCalendar(bot, chatId, lang, ct);
                    break;

                case "/contact":
                case "📞 Contact":
                case "📞 اتصل بنا":
                    await SendContact(bot, chatId, lang, ct);
                    break;

                case "/website":
                case "🌐 Website":
                case "🌐 Site Web":
                case "🌐 موقع الويب":
                    await SendWebsite(bot, chatId, lang, ct);
                    break;

                default:
                    // If first time, show language picker; otherwise show menu
                    if (!UserLang.ContainsKey(chatId))
                        await SendLangPicker(bot, chatId, "en", ct);
                    else
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
            var lang = data["lang:".Length..];
            if (!Langs.Contains(lang)) lang = "en";
            UserLang[chatId] = lang;
           if (Users.TryGetValue(cb.From.Id, out var info))
               Users[cb.From.Id] = info with { Language = lang };

            await bot.AnswerCallbackQueryAsync(cb.Id, Txt[lang]["picked_lang"], cancellationToken: ct);
            await SendWelcome(bot, chatId, lang, ct);
            return;
        }

        if (data == "open:inscription")
        {
            await bot.AnswerCallbackQueryAsync(cb.Id, "Opening portal…", cancellationToken: ct);
            await bot.SendTextMessageAsync(chatId, Links.Inscription, cancellationToken: ct);
            return;
        }

        if (data == "open:website")
        {
            await bot.AnswerCallbackQueryAsync(cb.Id, "Opening site…", cancellationToken: ct);
            await bot.SendTextMessageAsync(chatId, Links.Website, cancellationToken: ct);
            return;
        }
    }

    // === VIEWS ===
    private static async Task SendWelcome(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var kb = MainMenu(lang);
        await bot.SendTextMessageAsync(chatId, Txt[lang]["welcome"], replyMarkup: kb, cancellationToken: ct);
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
        b.AppendLine($"• {Txt[lang]["link_website"]}: {Links.Website}");
        await bot.SendTextMessageAsync(chatId, b.ToString(), cancellationToken: ct);
    }

    private static async Task SendPrograms(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine(Txt[lang]["programs_header"]);
        b.AppendLine(Txt[lang]["programs_list"]);
        await bot.SendTextMessageAsync(chatId, b.ToString(), cancellationToken: ct);
    }

    private static async Task SendConcours(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine($"<b>{Txt[lang]["concours_header"]}</b>");
        b.AppendLine();
        b.AppendLine(Admissions.SeatsBlurb(lang));
        b.AppendLine();
        b.AppendLine(Txt[lang]["concours_phases"]);
        b.AppendLine();
        b.AppendLine(Txt[lang]["eligibility"]);
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
                InlineKeyboardButton.WithCallbackData("📝 Portal", "open:inscription"),
                InlineKeyboardButton.WithCallbackData("🌐 Website", "open:website")
            }
        });

        await bot.SendTextMessageAsync(chatId, b.ToString(), parseMode: ParseMode.Html, replyMarkup: ikb, cancellationToken: ct);
    }

    private static async Task SendCalendar(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var b = new StringBuilder();
        b.AppendLine($"<b>{Txt[lang]["calendar"]}</b>");
        foreach (var item in Admissions.Calendar)
            b.AppendLine($"• {item.Date:dd/MM/yyyy} — {item.Label(lang)}");
        await bot.SendTextMessageAsync(chatId, b.ToString(), parseMode: ParseMode.Html, cancellationToken: ct);
    }

    private static async Task SendContact(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        await bot.SendTextMessageAsync(chatId, Txt[lang]["contact"], cancellationToken: ct);
    }

    private static async Task SendWebsite(ITelegramBotClient bot, long chatId, string lang, CancellationToken ct)
    {
        var ikb = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData(Txt[lang]["link_website"], "open:website"));
        await bot.SendTextMessageAsync(chatId, Links.Website, replyMarkup: ikb, cancellationToken: ct);
    }

    private static ReplyKeyboardMarkup MainMenu(string lang)
    {
        var rows = lang switch
        {
            "en" => new[]
            {
                new[] { new KeyboardButton("ℹ️ About"), new KeyboardButton("📚 Programs") },
                new[] { new KeyboardButton("📝 Concours (Master)"), new KeyboardButton("🗓️ Calendar") },
                new[] { new KeyboardButton("📞 Contact"), new KeyboardButton("🌐 Website") },
                new[] { new KeyboardButton("🔄 Language") }
            },
            "fr" => new[]
            {
                new[] { new KeyboardButton("ℹ️ À propos"), new KeyboardButton("📚 Programmes") },
                new[] { new KeyboardButton("📝 Concours (Master)"), new KeyboardButton("🗓️ Calendrier") },
                new[] { new KeyboardButton("📞 Contact"), new KeyboardButton("🌐 Site Web") },
                new[] { new KeyboardButton("🔄 Langue") }
            },
            "ar" => new[]
            {
                new[] { new KeyboardButton("ℹ️ حول"), new KeyboardButton("📚 البرامج") },
                new[] { new KeyboardButton("📝 المسابقة (ماستر)"), new KeyboardButton("🗓️ الرزنامة") },
                new[] { new KeyboardButton("📞 اتصال"), new KeyboardButton("🌐 الموقع") },
                new[] { new KeyboardButton("🔄 تغيير اللغة") }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lang))
        };

        return new ReplyKeyboardMarkup(rows) { ResizeKeyboard = true };
    }

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

    // === Data types ===
    private record UserInfo(string Username, string FirstName, string LastName, string Language);

    private sealed class AdmissionsData
    {
        // Seats policy (from PDF): total seats per specialty; reserved quarter for Maghreb/Africa (francophone),
        // quarter for social security cadres; remaining half for Algerian students outside funds.
        public string SeatsBlurb(string lang) => lang switch
        {
            "fr" => "Répartition des places : 1/4 pour les pays du Maghreb/Afrique francophone, 1/4 pour les cadres des caisses de sécurité sociale, et 1/2 pour les étudiants algériens hors caisses.",
            "ar" => "توزيع المقاعد: ربع لبلدان المغرب العربي وإفريقيا الناطقة بالفرنسية، وربع لإطارات صناديق الضمان الاجتماعي، والنصف للطلبة الجزائريين خارج الصناديق.",
            _    => "Seats allocation: 1/4 for Maghreb/Africa (French-speaking), 1/4 for social-security cadres, and 1/2 for Algerian students outside the funds."
        };

        // Official calendar (from PDF page with dates)
        public IReadOnlyList<CalItem> Calendar { get; } = new[]
        {
            new CalItem(new DateTime(2025,10,13), new()
            {
                ["en"] = "Online registration opens (through the portal)",
                ["fr"] = "Ouverture des inscriptions en ligne (via le portail)",
                ["ar"] = "فتح التسجيلات الإلكترونية (عبر البوابة)"
            }),
            new CalItem(new DateTime(2025,10,24), new()
            {
                ["en"] = "Online registration closes",
                ["fr"] = "Clôture des inscriptions en ligne",
                ["ar"] = "غلق التسجيلات الإلكترونية"
            }),
            new CalItem(new DateTime(2025,10,26), new()
            {
                ["en"] = "Admitted list for written exam published",
                ["fr"] = "Affichage de la liste des admis à l’écrit",
                ["ar"] = "الإعلان عن قائمة المقبولين للاختبار الكتابي"
            }),
            new CalItem(new DateTime(2025,11,08), new()
            {
                ["en"] = "Written exam",
                ["fr"] = "Épreuve écrite",
                ["ar"] = "الاختبار الكتابي"
            }),
            new CalItem(new DateTime(2025,11,12), new()
            {
                ["en"] = "Deliberations & results of written exam",
                ["fr"] = "Délibérations & résultats de l’écrit",
                ["ar"] = "المداولات ونشر نتائج الكتابي"
            }),
            new CalItem(new DateTime(2025,11,17), new()
            {
                ["en"] = "Oral exam",
                ["fr"] = "Épreuve orale",
                ["ar"] = "الاختبار الشفهي"
            }),
            new CalItem(new DateTime(2025,11,18), new()
            {
                ["en"] = "Final results (school + website)",
                ["fr"] = "Résultats finaux (école + site)",
                ["ar"] = "النتائج النهائية (بالمدرسة وعلى الموقع)"
            }),
            new CalItem(new DateTime(2025,11,20), new()
            {
                ["en"] = "Final pedagogical registration",
                ["fr"] = "Inscription pédagogique finale",
                ["ar"] = "التسجيل البيداغوجي النهائي"
            }),
            new CalItem(new DateTime(2025,11,23), new()
            {
                ["en"] = "Start of academic year (Master 1, 2025/2026)",
                ["fr"] = "Rentrée M1 (2025/2026)",
                ["ar"] = "انطلاق الموسم الجامعي (ماستر 1، 2025/2026)"
            }),
        };

        public sealed record CalItem(DateTime Date, Dictionary<string,string> Labels)
        {
            public string Label(string lang) => Labels.TryGetValue(lang, out var v) ? v : Labels["en"];
        }
    }
}

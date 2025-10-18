﻿﻿using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling; 
namespace Esssbot
{
class Program
{
    private static Dictionary<long, string> userLanguages = new Dictionary<long, string>();
     private static Dictionary<long, UserInfo> users = new Dictionary<long, UserInfo>();

    

    class UserInfo
    {
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime FirstSeen { get; set; }
        public string Language { get; set; }

        public UserInfo(string username, string firstName, string lastName, string language)
        {
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            FirstSeen = DateTime.Now;
            Language = language;
        }
    }
    private static string  GenerateUserStatsMessage(User user, string language)
    {
        string welcomeMessage = translations[language]["welcome"] + "\n\n";
        
        // New user message
        if (user != null && !users.ContainsKey(user.Id))
        {
            string newUserInfo = $"Welcome 👤 {(user.FirstName ?? "")} {(user.LastName ?? "")}\n";
                         
            return welcomeMessage + newUserInfo;
        }
        
        // Existing user message
        if (user == null)
        {
            return welcomeMessage + "User information is not available.";
        }

        string userInfo = $"Welcome back {(user.FirstName ?? "User")}!\n" ;
                    
        return welcomeMessage + userInfo;
    }

    private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
    {
         ["fr"] = new Dictionary<string, string>
        {
            ["welcome"] = "Bienvenue sur le bot ESSS ! Choisissez une option pour en savoir plus sur nos formations.",
            ["contact"] = "Contactez-nous:\nTéléphone: 023 06 76 16\nEmail: contact@esss.dz\nSite web: www.esss.dz",
            ["about"] = "L'École Supérieure de la Sécurité Sociale (ESSS) est un établissement public de formation supérieure créé en 2012. Notre mission est de former les cadres et gestionnaires du secteur de la sécurité sociale en Algérie.",
            ["programs"] = "Nos programmes de formation :\n\n1. Formation Initiale\n2. Formation Continue\n\nSélectionnez une option pour plus de détails.",
            ["website_msg"] = "Visitez notre site web :",
            ["initial_training"] = "Formation Initiale (Bac+3) :\n\n" +
                "1. Master professional  de la Protection Sociale\n" +
                "   • Durée : 2 ans\n" +
                "   • Spécialisations :\n" +
                "     - Droit de la Protection Sociale \n " +
                "     - Actuariat à finalité spécialisée – Sécurité Sociale \n" +
                "     - Management Stratégique et Opérationnel des Organismes de Protection Sociale \n" +
                "     -Gestion des Systèmes d’Information de la Protection Sociale "
                ,
            ["continuing_education_intro"] = "Formation Continue :\n\nNos programmes de formation continue sont conçus pour les professionnels en activité.",
            ["continuing_education_list"] = "Programmes de Formation Continue :\n\n" +
                "1. Certificat en Gestion des Prestations Sociales\n" +
                "   • Durée : 6 mois\n" +
                "   • Format : Cours du soir\n\n" +
                "2. Formation en Management des Organismes Sociaux\n" +
                "   • Durée : 3 mois\n" +
                "   • Modules : Leadership, Gestion financière\n\n" +
                "3. Formations Courtes Spécialisées\n" +
                "   • Réglementation sociale\n" +
                "   • Gestion des réclamations\n" +
                "   • Systèmes d'information",
            ["menu_about"] = "ℹ️ À propos",
            ["menu_programs"] = "📚 Programmes",
            ["menu_contact"] = "📞 Contact",
            ["menu_website"] = "🌐 Site Web",
            ["menu_change_language"] = "🔄 Changer de Langue",
            ["menu_back"] = "🔙 Retour au Menu Principal",
            ["menu_initial_training"] = "1. Formation Initiale",
            ["menu_continuing_education"] = "2. Formation Continue"
        },
        ["en"] = new Dictionary<string, string>
        {
            ["welcome"] = "Welcome to ESSS Bot! Choose an option to learn more about our training programs.",
            ["contact"] = "Contact us:\nPhone:  023 06 76 16\nEmail: contact@esss.dz\nWebsite: www.esss.dz",
            ["about"] = "The Higher School of Social Security (ESSS) is a public higher education institution established in 2012. Our mission is to train managers and administrators in Algeria's social security sector.",
            ["programs"] = "Our training programs:\n\n1. Initial Training\n2. Continuing Education\n\nSelect an option for more details.",
            ["website_msg"] = "Visit our website:",
            ["initial_training"] = "Initial Training (2-year program):\n\n" +
             
                "1. Master's in Social Protection\n" +
                "   • Duration: 2 years\n" +
                "   • Specializations:\n" +
                "     - Social Protection Law \n" +
                "     - Specialized Actuarial Studies – Social Security\n" +
                "     - Strategic and Operational Management of Social Protection Organizations \n" +
                "     - Information Systems Management of Social Protection"
                ,
            ["continuing_education_intro"] = "Continuing Education:\n\nOur continuing education programs are designed for working professionals.",
            ["continuing_education_list"] = "Continuing Education Programs:\n\n" +
                "1. Certificate in Social Benefits Management\n" +
                "   • Duration: 6 months\n" +
                "   • Format: Evening classes\n\n" +
                "2. Social Organizations Management Training\n" +
                "   • Duration: 3 months\n" +
                "   • Modules: Leadership, Financial Management\n\n" +
                "3. Specialized Short Courses\n" +
                "   • Social Regulations\n" +
                "   • Claims Management\n" +
                "   • Information Systems",
            ["menu_about"] = "ℹ️ About",
            ["menu_programs"] = "📚 Programs",
            ["menu_contact"] = "📞 Contact",
            ["menu_website"] = "🌐 Website",
            ["menu_change_language"] = "🔄 Change Language",
            ["menu_back"] = "🔙 Back to Main Menu",
            ["menu_initial_training"] = "1. Initial Training",
            ["menu_continuing_education"] = "2. Continuing Education"
        },
        ["ar"] = new Dictionary<string, string>
        {
            ["welcome"] = "مرحباً بكم في روبوت ESSS! اختر خياراً لمعرفة المزيد عن برامجنا التدريبية.",
             ["contact"] = "اتصل بنا:\nهاتف:   023 06 76 16  \nبريد إلكتروني: contact@esss.dz\nموقع الويب: www.esss.dz",  
             ["about"] = "المدرسة العليا للضمان الاجتماعي (ESSS) هي مؤسسة تعليم عالي عمومية تأسست عام 2012. مهمتنا هي تكوين إطارات ومسيري قطاع الضمان الاجتماعي في الجزائر.",
            ["programs"] = "برامجنا التدريبية:\n\n1. التكوين الأساسي\n2. التكوين المستمر\n\nاختر خياراً للمزيد من التفاصيل.",
            ["website_msg"] = "قم بزيارة موقعنا الإلكتروني:",
            ["initial_training"] = "التكوين الأساسي (برنامج سنتين 2):\n\n" +
                "1. ماستر  مهني في  الحماية الاجتماعية\n" +
                "   • المدة: سنتان\n" +
                "   • التخصصات:\n" +
                "     -قانون الحماية الاجتماعية\n" +
                "     -حساب المخاطرة للضمان الاجتماعي\n" +
                "     - لإدارة الاستراتيجية والتشغيلية للمؤسسات الاجتماعية\n" +
                "     - إدارة نظم المعلومات للحماية الاجتماعية"

                ,
            ["continuing_education_intro"] = "التكوين المستمر:\n\nبرامج التكوين المستمر مصممة للمهنيين العاملين.",
            ["continuing_education_list"] = "برامج التكوين المستمر:\n\n" +
                "1. شهادة في إدارة المنافع الاجتماعية\n" +
                "   • المدة: 6 أشهر\n" +
                "   • النظام: دروس مسائية\n\n" +
                "2. تكوين في إدارة المؤسسات الاجتماعية\n" +
                "   • المدة: 3 أشهر\n" +
                "   • المواد: القيادة، الإدارة المالية\n\n" +
                "3. دورات تخصصية قصيرة\n" +
                "   • التنظيمات الاجتماعية\n" +
                "   • إدارة المطالبات\n" +
                "   • نظم المعلومات",
            ["menu_about"] = "ℹ️ حول",
            ["menu_programs"] = "📚 البرامج",
            ["menu_contact"] = "📞 اتصل بنا",
            ["menu_website"] = "🌐 موقع الويب",
            ["menu_change_language"] = "🔄 تغيير اللغة",
            ["menu_back"] = "🔙 العودة إلى القائمة الرئيسية",
            ["menu_initial_training"] = "1. التكوين الأساسي",
            ["menu_continuing_education"] = "2. التكوين المستمر"
        }

    };

    static async Task Main(string[] args)
        {
            var botClient = new TelegramBotClient("8409133925:AAFJ-ExOjEKREIgrtIkwhjjsMZxp7Y_4gR0");
            var me = await botClient.GetMe();
            Console.WriteLine($"Bot {me.Username} is running...");

            using CancellationTokenSource cts = new();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            // Instead of Console.ReadKey(), use a ManualResetEvent to keep the application running
            var exitEvent = new ManualResetEvent(false);

            // Handle shutdown gracefully
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            // Wait until the exit event is triggered
            exitEvent.WaitOne();
            
            // Cleanup when shutting down
            cts.Cancel();
        }

   static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;

    var chatId = message.Chat.Id;
     var user = message.From;

    if (message.Text == "/start")
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🇬🇧 English"), new KeyboardButton("🇫🇷 Français") },
            new[] { new KeyboardButton("🇩🇿 العربية") },
        })
        {
            ResizeKeyboard = true
        };

         string statsMessage = GenerateUserStatsMessage(user ?? new User(), "en");

            await botClient.SendMessage(
                chatId: chatId,
                text: statsMessage + "\n\n" + "Please select your language / Veuillez sélectionner votre langue / يرجى اختيار لغتك:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            return;
    }

    // Handle language selection
    if (message.Text is "🇬🇧 English" or "🇫🇷 Français" or "🇩🇿 العربية")
    {
        string lang = message.Text switch
        {
            "🇬🇧 English" => "en",
            "🇫🇷 Français" => "fr",
            "🇩🇿 العربية" => "ar",
            _ => "en"
        };
         // Update or add user info
            if (user != null && !users.ContainsKey(user.Id))
            {
                users[user.Id] = new UserInfo(
                    user.Username ?? "N/A",
                    user.FirstName ?? "N/A",
                    user.LastName ?? "N/A",
                    lang
                );

                // Log new user to console
                Console.WriteLine($"New user: {user.FirstName} {user.LastName} (@{user.Username}) - Total users: {users.Count}");
            }

        userLanguages[chatId] = lang;
        await ShowMainMenu(botClient, chatId, lang, cancellationToken);
        return;
    }

    // Check if user has selected a language
    if (!userLanguages.TryGetValue(chatId, out string? currentLang))
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: "Please select a language first using /start\nVeuillez d'abord sélectionner une langue avec /start\nيرجى اختيار لغة أولاً باستخدام /start",
            cancellationToken: cancellationToken
        );
        return;
    }

    // Handle menu options
    switch (message.Text)
    {
        case "ℹ️ About" or "ℹ️ À propos" or "ℹ️ حول":
            await botClient.SendMessage(
                chatId: chatId,
                text: translations[currentLang]["about"],
                cancellationToken: cancellationToken
            );
            break;

        case "📚 Programs" or "📚 Programmes" or "📚 البرامج":
            await ShowProgramsMenu(botClient, chatId, currentLang, cancellationToken);
            break;

        case var text when text == translations[currentLang]["menu_initial_training"]:
            await botClient.SendMessage(
                chatId: chatId,
                text: translations[currentLang]["initial_training"],
                cancellationToken: cancellationToken
            );
            break;

        case var text when text == translations[currentLang]["menu_continuing_education"]:
            await botClient.SendMessage(
                chatId: chatId,
                text: translations[currentLang]["continuing_education_intro"] + "\n\n" + 
                      translations[currentLang]["continuing_education_list"],
                cancellationToken: cancellationToken
            );
            break;

        case var text when text == translations[currentLang]["menu_back"]:
            await ShowMainMenu(botClient, chatId, currentLang, cancellationToken);
            break;

        case "📞 Contact" or "📞 Contact" or "📞 اتصل بنا":
            await botClient.SendMessage(
                chatId: chatId,
                text: translations[currentLang]["contact"],
                cancellationToken: cancellationToken
            );
            break;

        case "🌐 Website" or "🌐 Site Web" or "🌐 موقع الويب":
            await botClient.SendMessage(
                chatId: chatId,
                text: $"{translations[currentLang]["website_msg"]} www.esss.dz",
                cancellationToken: cancellationToken
            );
            break;

        case "🔄 Change Language" or "🔄 Changer de Langue" or "🔄 تغيير اللغة":
            await HandleUpdateAsync(botClient, new Update { Message = new Message { Text = "/start", Chat = message.Chat } }, cancellationToken);
            break;
    }
}

    static async Task ShowProgramsMenu(ITelegramBotClient botClient, long chatId, string language, CancellationToken cancellationToken)
{
    var programButtons = new[]
    {
        new[] { new KeyboardButton(translations[language]["menu_initial_training"]), 
                new KeyboardButton(translations[language]["menu_continuing_education"]) },
        new[] { new KeyboardButton(translations[language]["menu_back"]) }
    };

    var keyboard = new ReplyKeyboardMarkup(programButtons)
    {
        ResizeKeyboard = true
    };

    await botClient.SendMessage(
        chatId: chatId,
        text: translations[language]["programs"],
        replyMarkup: keyboard,
        cancellationToken: cancellationToken
    );
}

    static async Task ShowMainMenu(ITelegramBotClient botClient, long chatId, string language, CancellationToken cancellationToken)
    {
        var menuButtons = language switch
        {
            "en" => new[]
            {
                new[] { new KeyboardButton("ℹ️ About"), new KeyboardButton("📚 Programs") },
                new[] { new KeyboardButton("📞 Contact"), new KeyboardButton("🌐 Website") },
                new[] { new KeyboardButton("🔄 Change Language") }
            },
            "fr" => new[]
            {
                new[] { new KeyboardButton("ℹ️ À propos"), new KeyboardButton("📚 Programmes") },
                new[] { new KeyboardButton("📞 Contact"), new KeyboardButton("🌐 Site Web") },
                new[] { new KeyboardButton("🔄 Changer de Langue") }
            },
            "ar" => new[]
            {
                new[] { new KeyboardButton("ℹ️ حول"), new KeyboardButton("📚 البرامج") },
                new[] { new KeyboardButton("📞 اتصل بنا"), new KeyboardButton("🌐 موقع الويب") },
                new[] { new KeyboardButton("🔄 تغيير اللغة") }
            },
            _ => throw new ArgumentException("Invalid language code")
        };

        var keyboard = new ReplyKeyboardMarkup(menuButtons)
        {
            ResizeKeyboard = true
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: translations[language]["welcome"],
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
      static async Task DisplayUserStatistics(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        string stats = $"📊 Bot Statistics:\n" +
                      $"Total users: {users.Count}\n" +
                      $"Active today: {users.Count(u => u.Value.FirstSeen.Date == DateTime.Now.Date)}\n" +
                      $"Language distribution:\n" +
                      $"🇬🇧 English: {users.Count(u => u.Value.Language == "en")}\n" +
                      $"🇫🇷 French: {users.Count(u => u.Value.Language == "fr")}\n" +
                      $"🇩🇿 Arabic: {users.Count(u => u.Value.Language == "ar")}";

        await botClient.SendMessage(
            chatId: chatId,
            text: stats,
            cancellationToken: cancellationToken
        );
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
}
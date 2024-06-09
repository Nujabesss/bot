using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

const string TELEGRAM_TOKEN = "7477245322:AAGTwUv8a26LDrKJI11s1MDdZiRZ875YENo";
var botClient = new TelegramBotClient(TELEGRAM_TOKEN);

using var cts = new CancellationTokenSource();
var userCarts = new Dictionary<long, List<(string Item, string Price)>>(); // Корзины пользователей

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // Получать все типы обновлений
};

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token
);

Console.WriteLine("Бот запущен. Нажмите Enter для выхода.");
Console.ReadLine();
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Type != UpdateType.Message || update.Message == null)
        return;

    var chatId = update.Message.Chat.Id;
    var message = update.Message;

    if (message.Type == MessageType.Text)
    {
        var messageText = message.Text;
        Console.WriteLine($"Получено сообщение '{messageText}' в чате {chatId}.");

        switch (messageText)
        {
            case "/start":
                await SendWelcomeMessage(chatId, cancellationToken);
                break;
            case "Меню":
            case "Назад":
                await SendMenuOptions(chatId, cancellationToken);
                break;
            case "Корзина":
                await ShowCart(chatId, cancellationToken);
                break;
            case "Пицца":
            case "Суши":
            case "Бургеры":
            case "Напитки":
                await SendMenu(chatId, messageText, cancellationToken);
                break;
            case "Заказать":
                await RequestContactInfo(chatId, cancellationToken);
                break;
            case "Вернуться":
                await Back(chatId, cancellationToken);
                break;
            default:
                await AddItemToCart(chatId, messageText, cancellationToken);
                break;
        }
    }
    else if (message.Contact != null)
    {
        await HandleContactMessage(chatId, message.Contact.PhoneNumber, cancellationToken);
    }
}

async Task SendWelcomeMessage(long chatId, CancellationToken cancellationToken)
{
    var replyKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "Меню" },
        new KeyboardButton[] { "Корзина" }
    })
    {
        ResizeKeyboard = true
    };

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "Добро пожаловать в бот по доставке еды! \n\nНажмите на кнопку 'Меню' для просмотра меню или 'Корзина' для просмотра вашей корзины.",
        replyMarkup: replyKeyboard,
        cancellationToken: cancellationToken
    );
}

async Task Back(long chatId, CancellationToken cancellationToken)
{
    var replyKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "Меню" },
        new KeyboardButton[] { "Корзина" }
    })
    {
        ResizeKeyboard = true
    };

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "Нажмите на кнопку 'Меню' для просмотра меню или 'Корзина' для просмотра вашей корзины.",
        replyMarkup: replyKeyboard,
        cancellationToken: cancellationToken
    );
}

async Task SendMenuOptions(long chatId, CancellationToken cancellationToken)
{
    var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "Пицца", "Суши" },
        new KeyboardButton[] { "Бургеры", "Напитки" },
        new KeyboardButton[] { "Вернуться" },
    })
    {
        ResizeKeyboard = true
    };

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "Выберите категорию из меню:",
        replyMarkup: replyKeyboardMarkup,
        cancellationToken: cancellationToken
    );
}

async Task RequestContactInfo(long chatId, CancellationToken cancellationToken)
{
    var requestReplyKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton("Отправить контакт") { RequestContact = true }
    })
    {
        ResizeKeyboard = true
    };

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "Нажмите на кнопку ниже, чтобы отправить свой контактный номер:",
        replyMarkup: requestReplyKeyboard,
        cancellationToken: cancellationToken
    );
}

async Task HandleContactMessage(long chatId, string phoneNumber, CancellationToken cancellationToken)
{
    var replyKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "Вернуться" }
    })
    {
        ResizeKeyboard = true
    };

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: $"Спасибо! Ваш номер телефона: {phoneNumber}. Мы свяжемся с вами для подтверждения заказа.\n\nСпасибо за доверие!",
        replyMarkup: replyKeyboard,
        cancellationToken: cancellationToken
    );

    // Очистка корзины после отправки заказа
    if (userCarts.ContainsKey(chatId))
    {
        userCarts[chatId].Clear();
    }

    Console.WriteLine($"Получен контактный номер телефона: {phoneNumber}");
}

async Task SendMenu(long chatId, string category, CancellationToken cancellationToken)
{
    List<(string Name, string Price, string ImageUrl)> menuItems = category switch
    {
        "Пицца" => new List<(string, string, string)>
        {
            ("Пицца Маргарита", "10$", "https://mykaleidoscope.ru/x/uploads/posts/2023-12/1703317637_mykaleidoscope-ru-p-klassicheskaya-italyanskaya-pitstsa-krasiv-30.jpg"),
            ("Пицца Пепперони", "12$", "http://i6.photo.2gis.com/images/branch/46/6473924497949606_9f9a.jpg"),
            ("Пицца Гавайская", "11$", "https://static.tildacdn.com/tild6262-3636-4739-b931-393063323066/_2022-03-24_173311.png")
        },
        "Суши" => new List<(string, string, string)>
        {
            ("Набор 'Филадельфия'", "15$", "https://static.tildacdn.com/tild6438-6439-4430-b732-373537376230/2019-08-14-01-31-06.jpg"),
            ("Набор 'Калифорния'", "18$", "https://sun9-24.userapi.com/wHtpaUZaAnG_vt3nkaW2h9eFdKi77uSqw5BYjw/iwr4PzRwSxI.jpg"),
            ("Ролл 'Унаги'", "10$", "https://100foto.club/uploads/posts/2022-06/1654848463_22-100foto-club-p-slivochnii-unagi-36.jpg")
        },
        "Бургеры" => new List<(string, string, string)>
        {
            ("Гамбургер", "5$", "https://img.razrisyika.ru/kart/13/1200/51110-gamburger-3.jpg"),
            ("Чизбургер", "6$", "https://i.insider.com/5978d6c9552be58c008b6a7a?width=1276"),
            ("Веганский бургер", "8$", "https://i.pinimg.com/originals/1b/60/72/1b60727e45afb56af0ee09e51bf9fa6a.jpg")
        },
        "Напитки" => new List<(string, string, string)>
        {
            ("Кола", "2$", "https://s0.rbk.ru/v6_top_pics/media/img/8/38/756595234406388.jpg"),
            ("Фанта", "2$", "https://img.razrisyika.ru/kart/135/1200/537142-fanta-37.jpg"),
            ("Лимонад", "3$", "https://img.razrisyika.ru/kart/93/370492-limonad-12.jpg")
        },
        _ => new List<(string, string, string)>()
    };

    foreach (var (name, price, imageUrl) in menuItems)
    {
        await botClient.SendPhotoAsync(
            chatId: chatId,
            photo: InputFile.FromString(imageUrl),
            caption: $"{name} - Цена: {price}",
            cancellationToken: cancellationToken
        );
    }

    var replyKeyboard = new ReplyKeyboardMarkup(
        menuItems.Select(item => new[] { new KeyboardButton(item.Name) }).Append(new[] { new KeyboardButton("Назад") }).ToArray()
    )
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "Выберите товар из меню:",
        replyMarkup: replyKeyboard,
        cancellationToken: cancellationToken
    );
}

async Task ShowCart(long chatId, CancellationToken cancellationToken)
{
    var replyKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "Заказать" },
        new KeyboardButton[] { "Вернуться" }
    })
    {
        ResizeKeyboard = true
    };

    if (!userCarts.ContainsKey(chatId) || userCarts[chatId].Count == 0)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Ваша корзина пуста.",
            cancellationToken: cancellationToken
        );
        return;
    }

    var cartItems = userCarts[chatId];
    var cartText = "Ваши товары в корзине:\n\n" + string.Join("\n", cartItems.Select(item => $"{item.Item} - {item.Price}"));

    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: cartText,
        replyMarkup: replyKeyboard,
        cancellationToken: cancellationToken
    );
}

async Task AddItemToCart(long chatId, string itemName, CancellationToken cancellationToken)
{
    // Поиск товара в меню
    var menuItems = new List<(string Name, string Price)>
    {
        ("Пицца Маргарита", "10$"), ("Пицца Пепперони", "12$"), ("Пицца Гавайская", "11$"),
        ("Набор 'Филадельфия'", "15$"), ("Набор 'Калифорния'", "18$"), ("Ролл 'Унаги'", "10$"),
        ("Гамбургер", "5$"), ("Чизбургер", "6$"), ("Веганский бургер", "8$"),
        ("Кола", "2$"), ("Фанта", "2$"), ("Лимонад", "3$")
    };

    var selectedItem = menuItems.FirstOrDefault(item => item.Name == itemName);

    if (selectedItem != default)
    {
        if (!userCarts.ContainsKey(chatId))
        {
            userCarts[chatId] = new List<(string Item, string Price)>();
        }

        userCarts[chatId].Add((selectedItem.Name, selectedItem.Price));

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"{selectedItem.Name} добавлен в вашу корзину.",
            cancellationToken: cancellationToken
        );
    }
    else
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Этот товар не найден в меню.",
            cancellationToken: cancellationToken
        );
    }
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException => $"Ошибка Telegram API:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}
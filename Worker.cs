using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot; 
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private static TelegramBotClient Bot;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Bot = new TelegramBotClient(ConfigurationManager.AppSettings["token"]);

        var me = await Bot.GetMeAsync();
        Console.Title = me.Username;

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        Bot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                            stoppingToken);

        Console.WriteLine($"Start listening for @{me.Username}");

        while (!stoppingToken.IsCancellationRequested)
        {
            Console.ReadLine();
            await Task.Delay(1000, stoppingToken);
        }
    }

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message            => BotOnMessageReceived(update.Message),
            UpdateType.EditedMessage      => BotOnMessageReceived(update.EditedMessage),
            _                             => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }
    }

    private static async Task BotOnMessageReceived(Message message)
    {
        Console.WriteLine($"Receive message type: {message.Type}");
        if (message.Type != MessageType.Text)
            return;

        var action = (message.Text.Split(' ').First()) switch
        {
            "/hello"    => Hello(message),
            "/cross"    => Cross(message), 
            _           => Usage(message)
        };
        var sentMessage = await action;
        Console.WriteLine($"The message was sent to: {sentMessage.Chat.Username}");

        static async Task<Message> Hello(Message message)
        {
            const string reply = "Hello!";
            return await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                    text: reply,
                                                    replyMarkup: new ReplyKeyboardRemove());
        }

        static async Task<Message> Cross(Message message)
        {
            const string reply = "Dont' cross me, animal crossing \U0001F624";
            return await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                    text: reply,
                                                    replyMarkup: new ReplyKeyboardRemove());
        }

        static async Task<Message> Usage(Message message)
        {
            const string usage = "Usage:\n" +
                                    "/hello: Don't Cross Me Says Hello!";
            return await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                    text: usage,
                                                    replyMarkup: new ReplyKeyboardRemove());
        }
    }
    private static Task UnknownUpdateHandlerAsync(Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }

    // Process Inline Keyboard callback data
    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}
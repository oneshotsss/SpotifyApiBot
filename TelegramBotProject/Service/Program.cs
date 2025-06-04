using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SpotifyTelegramBot.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        string telegramToken = config["Telegram:Token"];

        var botService = new TelegramBotService(telegramToken);
        Console.WriteLine("Бот запускається...");
        await botService.StartAsync();
    }
}

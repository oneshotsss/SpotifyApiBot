using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SpotifyTelegramBot.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://localhost:7089"; // Адреса твого Web API

        public TelegramBotService(string telegramToken)
        {
            _botClient = new TelegramBotClient(telegramToken);
            _httpClient = new HttpClient();
        }

        // Метод для отримання треків за артистом через Web API
        public async Task<List<string>> GetTracksByArtistAsync(string artist)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/tracks/artist/{Uri.EscapeDataString(artist)}");
            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json);
        }

        // Метод для отримання треків за жанром через Web API
        public async Task<List<string>> GetTracksByGenreAsync(string genre)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/tracks/genre/{Uri.EscapeDataString(genre)}");
            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json);
        }

        public async Task StartAsync()
        {
            var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // отримуємо всі оновлення
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Bot started: @{me.Username}");

            await Task.Delay(-1, cts.Token);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            var message = update.Message;

            if (message.Text == null)
                return;

            if (message.Text == "/start")
            {
                var welcomeText = "Привіт! Я музичний бот 🎵\n" +
                                  "Я можу допомогти знайти топ 5 треків за артистом або жанром.\n" +
                                  "Оберіть одну з кнопок нижче або напишіть команду:\n" +
                                  "/artist <ім'я артиста>\n" +
                                  "/genre <жанр>";

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "/artist", "/genre" }
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendTextMessageAsync(message.Chat.Id, welcomeText, replyMarkup: keyboard);
                return;
            }

            // Якщо користувач просто натиснув кнопку /artist або /genre без параметра — попросимо ввести
            if (message.Text == "/artist")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введи, будь ласка, ім'я артиста після команди /artist, наприклад:\n/artist Coldplay");
                return;
            }

            if (message.Text == "/genre")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введи, будь ласка, назву жанру після команди /genre, наприклад:\n/genre rock");
                return;
            }

            // Обробка команд з параметрами
            if (message.Text.StartsWith("/artist "))
            {
                string artist = message.Text.Substring(8).Trim();
                if (string.IsNullOrEmpty(artist))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, введи ім'я артиста після команди /artist.");
                    return;
                }
                await SendTracksByArtistAsync(message.Chat.Id, artist);
            }
            else if (message.Text.StartsWith("/genre "))
            {
                string genre = message.Text.Substring(7).Trim();
                if (string.IsNullOrEmpty(genre))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, введи назву жанру після команди /genre.");
                    return;
                }
                await SendTracksByGenreAsync(message.Chat.Id, genre);
            }
            else
            {
                // Якщо команда не розпізнана
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Я не розумію цю команду. Скористайся кнопками або напиши /start для початку.",
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
        }

        private async Task SendTracksByArtistAsync(long chatId, string artist)
        {
            var tracks = await GetTracksByArtistAsync(artist);

            if (tracks == null || tracks.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Вибач, не знайшлося треків за артистом \"{artist}\".");
                return;
            }

            // Відображаємо максимум 5 треків
            var topTracks = tracks.Count > 5 ? tracks.GetRange(0, 5) : tracks;

            string reply = $"Топ {topTracks.Count} треків артиста \"{artist}\":\n" + string.Join("\n", topTracks);
            await _botClient.SendTextMessageAsync(chatId, reply);
        }

        private async Task SendTracksByGenreAsync(long chatId, string genre)
        {
            var tracks = await GetTracksByGenreAsync(genre);

            if (tracks == null || tracks.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, $"Вибач, не знайшлося треків за жанром \"{genre}\".");
                return;
            }

            // Відображаємо максимум 5 треків
            var topTracks = tracks.Count > 5 ? tracks.GetRange(0, 5) : tracks;

            string reply = $"Топ {topTracks.Count} треків жанру \"{genre}\":\n" + string.Join("\n", topTracks);
            await _botClient.SendTextMessageAsync(chatId, reply);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SpotifyTelegramBot.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly string _spotifyClientId;
        private readonly string _spotifyClientSecret;
        private string _spotifyAccessToken;
        private DateTime _tokenExpiration;

        public TelegramBotService(string telegramToken, string spotifyClientId, string spotifyClientSecret)
        {
            _botClient = new TelegramBotClient(telegramToken);
            _httpClient = new HttpClient();
            _spotifyClientId = spotifyClientId;
            _spotifyClientSecret = spotifyClientSecret;
            _spotifyAccessToken = string.Empty;
            _tokenExpiration = DateTime.MinValue;
        }
        public async Task<List<string>> GetTracksByArtistAsync(string artist)
        {
            await AuthenticateSpotifyAsync();

            string url = $"https://api.spotify.com/v1/search?q=artist:{Uri.EscapeDataString(artist)}&type=track&limit=5";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _spotifyAccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<string>(); // або null, залежно як хочеш
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var tracks = new List<string>();

            if (doc.RootElement.TryGetProperty("tracks", out JsonElement tracksElement) &&
                tracksElement.TryGetProperty("items", out JsonElement items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    string trackName = item.GetProperty("name").GetString();
                    var artistsArray = item.GetProperty("artists").EnumerateArray();
                    string firstArtist = "";
                    foreach (var a in artistsArray)
                    {
                        firstArtist = a.GetProperty("name").GetString();
                        break;
                    }
                    tracks.Add($"{trackName} - {firstArtist}");
                }
            }

            return tracks;
        }

        public async Task<List<string>> GetTracksByGenreAsync(string genre)
        {
            await AuthenticateSpotifyAsync();

            string url = $"https://api.spotify.com/v1/search?q=genre:{Uri.EscapeDataString(genre)}&type=track&limit=5";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _spotifyAccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var tracks = new List<string>();

            if (doc.RootElement.TryGetProperty("tracks", out JsonElement tracksElement) &&
                tracksElement.TryGetProperty("items", out JsonElement items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    string trackName = item.GetProperty("name").GetString();
                    var artistsArray = item.GetProperty("artists").EnumerateArray();
                    string firstArtist = "";
                    foreach (var a in artistsArray)
                    {
                        firstArtist = a.GetProperty("name").GetString();
                        break;
                    }
                    tracks.Add($"{trackName} - {firstArtist}");
                }
            }

            return tracks;
        }

        public async Task StartAsync()
        {
            var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // отримаємо всі оновлення
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Bot started: @{me.Username}");

            // Бот працюватиме доти, доки процес не зупиниться
            await Task.Delay(-1, cts.Token);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            var message = update.Message;

            if (message.Text == null)
                return;

            if (message.Text.StartsWith("/artist "))
            {
                string artist = message.Text.Substring(8).Trim();
                await SendTracksByArtistAsync(message.Chat.Id, artist);
            }
            else if (message.Text.StartsWith("/genre "))
            {
                string genre = message.Text.Substring(7).Trim();
                await SendTracksByGenreAsync(message.Chat.Id, genre);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Використовуйте команди:\n/artist <ім'я артиста>\n/genre <жанр>",
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
        }

        private async Task AuthenticateSpotifyAsync()
        {
            if (!string.IsNullOrEmpty(_spotifyAccessToken) && DateTime.UtcNow < _tokenExpiration)
                return; // Токен ще дійсний

            var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_spotifyClientId}:{_spotifyClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _spotifyAccessToken = doc.RootElement.GetProperty("access_token").GetString();
            int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60); // оновлюємо токен за хвилину до завершення
        }

        private async Task SendTracksByArtistAsync(long chatId, string artist)
        {
            await AuthenticateSpotifyAsync();

            string url = $"https://api.spotify.com/v1/search?q=artist:{Uri.EscapeDataString(artist)}&type=track&limit=5";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _spotifyAccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await _botClient.SendTextMessageAsync(chatId, "Помилка при пошуку треків у Spotify.");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var tracks = new List<string>();

            if (doc.RootElement.TryGetProperty("tracks", out JsonElement tracksElement) &&
                tracksElement.TryGetProperty("items", out JsonElement items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    string trackName = item.GetProperty("name").GetString();
                    var artistsArray = item.GetProperty("artists").EnumerateArray();
                    string firstArtist = "";
                    foreach (var a in artistsArray)
                    {
                        firstArtist = a.GetProperty("name").GetString();
                        break;
                    }
                    tracks.Add($"{trackName} - {firstArtist}");
                }
            }

            if (tracks.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "Не знайдено треків за цим артистом.");
                return;
            }

            string reply = "Топ 5 треків за артистом " + artist + ":\n" + string.Join("\n", tracks);
            await _botClient.SendTextMessageAsync(chatId, reply);
        }

        private async Task SendTracksByGenreAsync(long chatId, string genre)
        {
            await AuthenticateSpotifyAsync();

            // Spotify API не має прямого пошуку за жанром треків,
            // тож можна шукати по жанру виконавця (artist:genre), або по ключовому слову

            string url = $"https://api.spotify.com/v1/search?q=genre:{Uri.EscapeDataString(genre)}&type=track&limit=5";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _spotifyAccessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await _botClient.SendTextMessageAsync(chatId, "Помилка при пошуку треків за жанром у Spotify.");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var tracks = new List<string>();

            if (doc.RootElement.TryGetProperty("tracks", out JsonElement tracksElement) &&
                tracksElement.TryGetProperty("items", out JsonElement items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    string trackName = item.GetProperty("name").GetString();
                    var artistsArray = item.GetProperty("artists").EnumerateArray();
                    string firstArtist = "";
                    foreach (var a in artistsArray)
                    {
                        firstArtist = a.GetProperty("name").GetString();
                        break;
                    }
                    tracks.Add($"{trackName} - {firstArtist}");
                }
            }

            if (tracks.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "Не знайдено треків за цим жанром.");
                return;
            }

            string reply = "Топ 5 треків жанру " + genre + ":\n" + string.Join("\n", tracks);
            await _botClient.SendTextMessageAsync(chatId, reply);
        }
    }
}

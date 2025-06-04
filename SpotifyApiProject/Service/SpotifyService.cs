using SpotifyAPI.Web;
using SpotifyTelegramBot.Models;
using SearchRequest = SpotifyAPI.Web.SearchRequest;

namespace SpotifyApiProject.Service
{
    public class SpotifyService
    {
        private readonly SpotifyClient _spotifyClient;

        public SpotifyService(string clientId, string clientSecret)
        {
            var config = SpotifyClientConfig.CreateDefault();

            var request = new ClientCredentialsRequest(clientId, clientSecret);
            var tokenResponse = new OAuthClient(config).RequestToken(request).Result;

            _spotifyClient = new SpotifyClient(config.WithToken(tokenResponse.AccessToken));
        }

        public async Task<List<string>> GetTracksByArtistAsync(string artist)
        {
            var query = $"artist:\"{artist}\"";
            var searchResponse = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, query)
            {
                Limit = 5
            });
            return searchResponse.Tracks.Items
                .Select(track => $"{track.Name} - {track.Artists.FirstOrDefault()?.Name}")
                .ToList();
        }




        public async Task<List<string>> GetTracksByGenreAsync(string genre)
        {
            // Шукаємо плейлисти за жанром
            var searchRequest = new SearchRequest(SearchRequest.Types.Playlist, genre)
            {
                Limit = 1 // беремо найперший
            };

            var playlistSearch = await _spotifyClient.Search.Item(searchRequest);
            var playlist = playlistSearch.Playlists.Items.FirstOrDefault();

            if (playlist == null)
                return new List<string>();

            // Отримуємо треки з плейлиста
            var playlistTracks = await _spotifyClient.Playlists.GetItems(playlist.Id);

            // Беремо топ 5 треків
            var topTracks = playlistTracks.Items
                .Where(item => item.Track is FullTrack)
                .Select(item => item.Track as FullTrack)
                .Take(5)
                .Select(t => $"{t.Name} - {t.Artists.FirstOrDefault()?.Name}")
                .ToList();

            return topTracks;
        }


    }
}

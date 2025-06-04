using Microsoft.AspNetCore.Mvc;  // Для [FromBody]
using SpotifyApiProject.Service;

var builder = WebApplication.CreateBuilder(args);

// Додаємо сервіси Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Реєструємо SpotifyService як Singleton
builder.Services.AddSingleton<SpotifyService>(sp =>
{
    var config = builder.Configuration;
    var spotifyClientId = config["Spotify:ClientId"];
    var spotifyClientSecret = config["Spotify:ClientSecret"];

    return new SpotifyService(spotifyClientId, spotifyClientSecret);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Spotify API працює");
app.MapGet("/ping", () => "pong");

// Отримати треки за артистом (GET)
app.MapGet("/tracks/artist/{artist}", async (string artist, SpotifyService spotifyService) =>
{
    var tracks = await spotifyService.GetTracksByArtistAsync(artist);
    if (tracks == null || tracks.Count == 0)
        return Results.NotFound("Треків не знайдено");

    return Results.Ok(tracks);
}).WithName("GetTracksByArtist").WithTags("Spotify");

// Отримати треки за жанром (GET)
app.MapGet("/tracks/genre/{genre}", async (string genre, SpotifyService spotifyService) =>
{
    var tracks = await spotifyService.GetTracksByGenreAsync(genre);
    if (tracks == null || tracks.Count == 0)
        return Results.NotFound("Треків не знайдено");

    return Results.Ok(tracks);
}).WithName("GetTracksByGenre").WithTags("Spotify");

// Створити новий трек (POST)
app.MapPost("/tracks", async ([FromBody] TrackDto newTrack) =>
{
    // Логіка додавання нового треку (поки заглушка)
    return Results.Created($"/tracks/{newTrack.Id}", newTrack);
}).WithName("CreateTrack").WithTags("Spotify");

// Оновити трек (PUT)
app.MapPut("/tracks/{id}", async (string id, [FromBody] TrackDto updatedTrack) =>
{
    // Логіка оновлення треку (поки заглушка)
    return Results.NoContent();
}).WithName("UpdateTrack").WithTags("Spotify");

// Видалити трек (DELETE)
app.MapDelete("/tracks/{id}", async (string id) =>
{
    // Логіка видалення треку (поки заглушка)
    return Results.NoContent();
}).WithName("DeleteTrack").WithTags("Spotify");

app.Run();

public record TrackDto(string Id, string Name, string Artist);

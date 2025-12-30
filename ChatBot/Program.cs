using ChatBot;
using ChatBot.Services;
// using Indexer;

var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder);
var app = builder.Build();

// Use CORS policy defined in Startup.cs
// It allows the API response to be accessed from our frontend (running on localhost:3000)
app.UseCors("FrontendCors");

// // Uncomment to do indexing when you run the project (you only need to do this once)...
// var indexer = app.Services.GetRequiredService<IndexBuilder>();
// await indexer.BuildDocumentIndex(SourceData.LandmarkNames);

// GET /search?query=...
app.MapGet("/search", async (string query, VectorSearchService search) =>
{
    var results = await search.FindTopKArticles(query, 3);
    return Results.Ok(results);
});

app.Run();
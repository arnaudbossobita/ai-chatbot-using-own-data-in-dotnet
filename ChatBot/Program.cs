using ChatBot;
using ChatBot.Services;
// using Indexer;

var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder);
var app = builder.Build();

// Use CORS policy defined in Startup.cs
// It allows the API response to be accessed from our frontend (running on localhost:3000)
app.UseCors("FrontendCors");

// Uncomment to do indexing when you run the project (you only need to do this once)...
// Full landmark indexing
// var indexer = app.Services.GetRequiredService<IndexBuilder>();
// await indexer.BuildDocumentIndex(SourceData.LandmarkNames);
// Chunked landmark indexing
// var indexerChunk = app.Services.GetRequiredService<IndexBuilderChunk>();
// await indexerChunk.BuildIndex(SourceData.LandmarkNames);
// Full landmark PDF indexing
// var indexer = app.Services.GetRequiredService<IndexBuilderPdf>();
// await indexer.BuildDocumentIndexFromPdf("Pdfs/", parseByPage: true);

// GET /search?query=...
// Full landmark search
app.MapGet("/search", async (string query, VectorSearchService search) =>
{
    var results = await search.FindTopKArticles(query, 3);
    return Results.Ok(results);
});

// GET /search-chunk?query=...
// Chunked landmark search
app.MapGet("/search-chunk", async (string query, VectorSearchServiceChunk search) =>
{
    var results = await search.FindTopKArticles(query, 3);
    return Results.Ok(results);
});

// GET /search-pdf?query=...
// Full landmark PDF search 
app.MapGet("/search-pdf", async (string query, VectorSearchServicePdf search) =>
{
    var results = await search.FindTopKArticles(query, 3);
    return Results.Ok(results);
});

// GET /ask?question=...
app.MapGet("/ask", async (string question, RagQuestionService rag) =>
{
    var result = await rag.AnswerQuestion(question);
    return Results.Ok(result);
});

// GET /ask-pdf?question=...
app.MapGet("/ask-pdf", async (string question, RagQuestionServicePdf rag) =>
{
    var result = await rag.AnswerQuestion(question);
    return Results.Ok(result);
});

app.Run();
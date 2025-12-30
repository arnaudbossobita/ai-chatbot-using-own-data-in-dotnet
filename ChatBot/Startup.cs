using System;
using Microsoft.Extensions.AI;
using Pinecone;
using ChatBot.Services;

namespace ChatBot;

static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var openAiKey = builder.RequireEnv("OPENAI_API_KEY");
        var pineconeKey = builder.RequireEnv("PINECONE_API_KEY");

        builder.Services.AddCors(options =>
        {
            // This is to allow the API response to be accessed from our frontend (running on localhost:3000)
            options.AddPolicy("FrontendCors", policy =>
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
                model: "text-embedding-3-small",
                apiKey: openAiKey
            ).AsIEmbeddingGenerator());

        builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("wikipedia-landmarks"));

        builder.Services.AddSingleton<WikipediaClient>();
        builder.Services.AddSingleton<IndexBuilder>();
        builder.Services.AddSingleton<DocumentStore>();
        builder.Services.AddSingleton<VectorSearchService>();
    }
}

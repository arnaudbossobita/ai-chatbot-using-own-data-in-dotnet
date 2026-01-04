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

        // Landmark chunks index search
        // builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("wikipedia-landmarks"));
        builder.Services.AddKeyedSingleton<IndexClient>("wikipedia-landmarks", (s, k) => 
            new PineconeClient(pineconeKey).Index("wikipedia-landmarks"));
        builder.Services.AddSingleton<VectorSearchService>();
        builder.Services.AddSingleton<WikipediaClient>();
        builder.Services.AddSingleton<PdfReaderService>();
        builder.Services.AddSingleton<IndexBuilder>();
        builder.Services.AddSingleton<DocumentStore>();

        // Full landmark index search
        // builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("landmark-chunks"));
        builder.Services.AddKeyedSingleton<IndexClient>("landmark-chunks", (s, k) => 
            new PineconeClient(pineconeKey).Index("landmark-chunks"));
        builder.Services.AddSingleton<VectorSearchServiceChunk>();
        builder.Services.AddSingleton<WikipediaClientChunk>();
        builder.Services.AddSingleton<IndexBuilderChunk>();
        builder.Services.AddSingleton<DocumentChunkStore>();
        builder.Services.AddSingleton<ArticleSplitter>();

        // Full landmark index search from PDFs
        // builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("wikipedia-landmarks-pdf"));
        builder.Services.AddKeyedSingleton<IndexClient>("wikipedia-landmarks-pdf", (s, k) => 
            new PineconeClient(pineconeKey).Index("wikipedia-landmarks-pdf"));
        builder.Services.AddSingleton<VectorSearchServicePdf>();
        builder.Services.AddSingleton<IndexBuilderPdf>();
        builder.Services.AddSingleton<PdfReaderService>();
        builder.Services.AddSingleton<DocumentPdfStore>();
        builder.Services.AddSingleton<RagQuestionServicePdf>();


        builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        builder.Services.AddSingleton<ILoggerFactory>(sp =>
            LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var client = new OpenAI.Chat.ChatClient(
                "gpt-4.1-mini",
                openAiKey).AsIChatClient();

            return new ChatClientBuilder(client)
                .UseLogging(loggerFactory)
                .UseFunctionInvocation(loggerFactory, c => 
                {
                    c.IncludeDetailedErrors = true; // Errors details for debugging logged in the logger
                })
                .Build(sp);
        });

        builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
        {
        });

        builder.Services.AddSingleton<RagQuestionService>();
        builder.Services.AddSingleton<ArticleSplitter>();
        builder.Services.AddSingleton<PromptService>();
    }
}

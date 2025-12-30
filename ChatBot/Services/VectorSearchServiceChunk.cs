using ChatBot.Models;
using Pinecone;

namespace ChatBot.Services;

public class VectorSearchServiceChunk(
    StringEmbeddingGenerator embeddingGenerator,
    // Pinecone.IndexClient pineconeIndex,
    [FromKeyedServices("landmark-chunks")] IndexClient pineconeIndex,
    DocumentChunkStore contentStore)
{
    public async Task<List<DocumentChunk>> FindTopKArticles(string query, int k)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var embeddings = await embeddingGenerator.GenerateAsync(
            [query],
            new Microsoft.Extensions.AI.EmbeddingGenerationOptions { Dimensions = 512 }
        );

        var vector = embeddings[0].Vector.ToArray();

        var response = await pineconeIndex.QueryAsync(new Pinecone.QueryRequest
        {
            Vector = vector,
            TopK = (uint)k,
            IncludeMetadata = true
        });

        var matches = (response.Matches ?? []).ToList();
        if (matches.Count == 0)
            return [];

        var ids = matches.Select(m => m.Id!).Where(id => !string.IsNullOrEmpty(id));
        var articles = contentStore.GetDocumentChunks(ids);

        var scoreById = matches.Where(m => m.Id is not null)
                               .ToDictionary(m => m.Id!, m => m.Score);

        var ordered = articles.OrderByDescending(a => scoreById.GetValueOrDefault(a.Id, 0f))
                              .Take(k)
                              .ToList();

        return ordered;
    }
}
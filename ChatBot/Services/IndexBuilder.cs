using Pinecone;
using Microsoft.Extensions.AI;
using System.Collections.Immutable;

namespace ChatBot.Services;

public class IndexBuilder(
    // Class dependencies
    StringEmbeddingGenerator embeddingGenerator,
    // IndexClient pineconeIndex,
    [FromKeyedServices("wikipedia-landmarks")] IndexClient pineconeIndex,
    WikipediaClient wikipediaClient,
    DocumentStore documentStore)
{
    public async Task BuildDocumentIndex(string[] pageTitles)
    {
        foreach (var landmark in pageTitles)
        {
            var wikiPage = await wikipediaClient.GetWikipediaPageForTitle(landmark);

            var embeddings = await embeddingGenerator.GenerateAsync(
                [wikiPage.Content],
                new EmbeddingGenerationOptions { Dimensions = 512 }
            );

            var vectorArray = embeddings[0].Vector.ToArray();
            var pineconeVector = new Vector
            {
                Id = wikiPage.Id,
                Values = vectorArray,
                Metadata = new Metadata
                {
                    { "title", wikiPage.Title }
                }
            };

            await pineconeIndex.UpsertAsync(new UpsertRequest
            {
                Vectors = [pineconeVector]
            });

            documentStore.SaveDocument(wikiPage);
        }
    }
}

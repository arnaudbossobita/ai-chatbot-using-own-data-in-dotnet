using Pinecone;
using Microsoft.Extensions.AI;
using System.Collections.Immutable;

namespace ChatBot.Services;

public class IndexBuilderPdf(
    // Class dependencies
    StringEmbeddingGenerator embeddingGenerator,
    // IndexClient pineconeIndex,
    [FromKeyedServices("wikipedia-landmarks-pdf")] IndexClient pineconeIndex,
    PdfReaderService pdfReaderService,
    DocumentPdfStore documentPdfStore)
{
    /// <summary>
    /// Builds document index from a local PDF file
    /// </summary>
    /// <param name="pdfFilePath">Path to the PDF file containing landmark data</param>
    /// <param name="parseByPage">If true, each PDF page becomes a separate document. If false, treats entire PDF as one document.</param>
    public async Task BuildDocumentIndexFromPdf(string pdfFilePath, bool parseByPage = false)
    {
        if (!File.Exists(pdfFilePath))
        {
            throw new FileNotFoundException($"PDF file not found: {pdfFilePath}");
        }

        // Parse the PDF into documents
        var documents = parseByPage 
            ? pdfReaderService.ParseLandmarksByPage(pdfFilePath)
            : pdfReaderService.ParseLandmarksFromPdf(pdfFilePath);

        foreach (var document in documents)
        {
            var embeddings = await embeddingGenerator.GenerateAsync(
                [document.Content],
                new EmbeddingGenerationOptions { Dimensions = 512 }
            );

            var vectorArray = embeddings[0].Vector.ToArray();
            var pineconeVector = new Vector
            {
                Id = document.Id,
                Values = vectorArray,
                Metadata = new Metadata
                {
                    { "title", document.Title }
                }
            };

            await pineconeIndex.UpsertAsync(new UpsertRequest
            {
                Vectors = [pineconeVector]
            });

            documentPdfStore.SaveDocument(document);
        }
    }

    /// <summary>
    /// Builds document index from a PDF with custom section separators
    /// Useful if your PDF has clear section markers like "## Landmark Name"
    /// </summary>
    /// <param name="pdfFilePath">Path to the PDF file</param>
    /// <param name="sectionPattern">Regex pattern to identify sections (default: lines starting with ##)</param>
    public async Task BuildDocumentIndexFromPdfWithSeparator(string pdfFilePath, string sectionPattern = @"(?m)^##\s+(.+)$")
    {
        if (!File.Exists(pdfFilePath))
        {
            throw new FileNotFoundException($"PDF file not found: {pdfFilePath}");
        }

        var documents = pdfReaderService.ParseLandmarksWithSeparator(pdfFilePath, sectionPattern);

        foreach (var document in documents)
        {
            var embeddings = await embeddingGenerator.GenerateAsync(
                [document.Content],
                new EmbeddingGenerationOptions { Dimensions = 512 }
            );

            var vectorArray = embeddings[0].Vector.ToArray();
            var pineconeVector = new Vector
            {
                Id = document.Id,
                Values = vectorArray,
                Metadata = new Metadata
                {
                    { "title", document.Title }
                }
            };

            await pineconeIndex.UpsertAsync(new UpsertRequest
            {
                Vectors = [pineconeVector]
            });

            documentPdfStore.SaveDocument(document);
        }
    }
}

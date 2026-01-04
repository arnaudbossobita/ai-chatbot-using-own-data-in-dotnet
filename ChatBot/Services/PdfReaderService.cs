using ChatBot.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ChatBot.Services;

public class PdfReaderService
{
    /// <summary>
    /// Extracts all text content from a PDF file
    /// </summary>
    public async Task<string> ExtractTextFromPdf(string pdfFilePath)
    {
        return await Task.Run(() =>
        {
            using var document = PdfDocument.Open(pdfFilePath);
            var allText = string.Empty;

            foreach (Page page in document.GetPages())
            {
                allText += page.Text + "\n\n";
            }

            return allText;
        });
    }

    /// <summary>
    /// Parses PDF content into Document objects.
    /// Assumes the PDF contains landmark information with clear section separators.
    /// You may need to customize this based on your PDF structure.
    /// </summary>
    public List<DocumentPdf> ParseLandmarksFromPdf(string pdfFilePath)
    {
        using var document = PdfDocument.Open(pdfFilePath);
        var documents = new List<DocumentPdf>();

        // Option 1: Treat entire PDF as one document
        var fullText = string.Empty;
        foreach (Page page in document.GetPages())
        {
            fullText += page.Text + "\n\n";
        }

        var doc = new DocumentPdf(
            Id: Guid.NewGuid().ToString(),
            Title: Path.GetFileNameWithoutExtension(pdfFilePath),
            Content: fullText.Trim(),
            PageUrl: $"file:///{pdfFilePath}",
            PageNumber: null
        );

        documents.Add(doc);

        return documents;
    }

    /// <summary>
    /// Alternative method: Parse PDF with page-level granularity
    /// Each page becomes a separate document
    /// </summary>
    public List<DocumentPdf> ParseLandmarksByPage(string pdfFilePath)
    {
        using var document = PdfDocument.Open(pdfFilePath);
        var documents = new List<DocumentPdf>();

        foreach (Page page in document.GetPages())
        {
            var pageText = ExtractPageText(page);
            if (string.IsNullOrWhiteSpace(pageText))
                continue;

            var doc = new DocumentPdf(
                Id: $"{Path.GetFileNameWithoutExtension(pdfFilePath)}_page_{page.Number}",
                Title: $"{Path.GetFileNameWithoutExtension(pdfFilePath)} - Page {page.Number}",
                Content: pageText,
                PageUrl: $"file:///{pdfFilePath}#page={page.Number}",
                PageNumber: page.Number
            );

            documents.Add(doc);
        }

        return documents;
    }

    /// <summary>
    /// Extracts text from a page using multiple fallback methods
    /// </summary>
    private string ExtractPageText(Page page)
    {
        // Method 1: Try page.Text
        var text = page.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        // Method 2: Try GetWords()
        var words = page.GetWords();
        if (words != null && words.Any())
        {
            text = string.Join(" ", words.Select(w => w.Text));
            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        // Method 3: Try Letters
        var letters = page.Letters;
        if (letters != null && letters.Any())
        {
            text = string.Join("", letters.Select(l => l.Value));
            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Advanced method: Parse PDF with custom section separators
    /// Useful if your PDF has clear section markers (e.g., "## Landmark Name")
    /// </summary>
    public List<DocumentPdf> ParseLandmarksWithSeparator(string pdfFilePath, string sectionPattern = @"(?m)^##\s+(.+)$")
    {
        var fullText = string.Empty;
        using (var document = PdfDocument.Open(pdfFilePath))
        {
            foreach (Page page in document.GetPages())
            {
                fullText += page.Text + "\n\n";
            }
        }

        var documents = new List<DocumentPdf>();
        var regex = new System.Text.RegularExpressions.Regex(sectionPattern);
        var matches = regex.Matches(fullText);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var title = match.Groups[1].Value.Trim();
            var startIndex = match.Index + match.Length;
            var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : fullText.Length;
            var content = fullText.Substring(startIndex, endIndex - startIndex).Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                var doc = new DocumentPdf(
                    Id: Guid.NewGuid().ToString(),
                    Title: title,
                    Content: content,
                    PageUrl: $"file:///{pdfFilePath}",
                    PageNumber: null
                );

                documents.Add(doc);
            }
        }

        return documents;
    }
}

namespace ChatBot.Models;

public record DocumentPdf(
    string Id,
    string Title,
    string Content,
    string PageUrl,
    string Name,
    int? PageNumber
);

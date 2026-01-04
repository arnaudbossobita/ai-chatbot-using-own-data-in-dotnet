using ChatBot.Models;
using Microsoft.Data.Sqlite;

namespace ChatBot.Services;

/// <summary>
/// Stores complete Wikipedia articles (use for the first iteration of the search engine)
/// </summary>
public class DocumentPdfStore
{
    private const string DbFile = "DocumentPdfStore.db";

    static DocumentPdfStore()
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Documents(
                Id TEXT PRIMARY KEY,
                Title TEXT,
                Content TEXT,
                PageUrl TEXT,
                PageNumber INTEGER
            );
        ;";
        cmd.ExecuteNonQuery();
    }

    public List<DocumentPdf> GetDocuments(IEnumerable<string> ids)
    {
        var idList = ids?.Distinct().ToList() ?? [];
        if (idList.Count == 0) return [];

        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();

        var paramNames = new List<string>(idList.Count);
        for (int i = 0; i < idList.Count; i++)
        {
            var p = "$p" + i;
            paramNames.Add(p);
            cmd.Parameters.AddWithValue(p, idList[i]);
        }

        // Preserve the caller's order of ids
        var orderByCase =
            "CASE Id " +
            string.Join(" ", idList.Select((id, i) => $"WHEN $p{i} THEN {i}")) +
            " END";

        cmd.CommandText = $@"
            SELECT Id, Title, Content, PageUrl, PageNumber
            FROM Documents
            WHERE Id IN ({string.Join(", ", paramNames)})
            ORDER BY {orderByCase};";

        var results = new List<DocumentPdf>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new DocumentPdf(
                Id: reader.GetString(0),
                Title: reader.GetString(1),
                Content: reader.GetString(2),
                PageUrl: reader.GetString(3),
                PageNumber: reader.GetInt32(4)
            ));
        }

        return results;
    }

    public void SaveDocument(DocumentPdf document)
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Documents
                (Id, Title, Content, PageUrl, PageNumber)
            VALUES ($id, $title, $content, $pageUrl, $pageNumber);";
        cmd.Parameters.AddWithValue("$id", document.Id);
        cmd.Parameters.AddWithValue("$title", document.Title);
        cmd.Parameters.AddWithValue("$content", document.Content);
        cmd.Parameters.AddWithValue("$pageUrl", document.PageUrl);
        cmd.Parameters.AddWithValue("$pageNumber", document.PageNumber);
        cmd.ExecuteNonQuery();
    }
}
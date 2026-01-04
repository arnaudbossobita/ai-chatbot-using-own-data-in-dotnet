using ChatBot.Models;
using Microsoft.Extensions.AI;

namespace ChatBot.Services;

public class RagQuestionServicePdf(
    VectorSearchServicePdf vectorSearch, 
    IChatClient client, 
    ChatOptions chatOptions, 
    PromptService promptService)
{
    public async Task<string> AnswerQuestion(string question)
    {
        // Retrieve relevant document chunks (Retrieval part)
        var searchResults = await vectorSearch.FindTopKArticles(question, 5);
        
        // Build the prompt with retrieved chunks (Augmentation part)
        var systemPrompt = promptService.RagSystemPrompt;

        var userPrompt = $@"User question:
            {question}

            Retrieved article sections:
            {String.Join("\n\n", searchResults.Select(document => 
                @$"Title: {document.Title}
                Content: {document.Content}
                URL:{document.PageUrl}
                PageNumber: {document.PageNumber?.ToString() ?? "N/A"}"))}
                ";

        var messages = (new[] 
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        }).ToList();

        // Get the answer from the chat model (Generation part)
        var response = await client.GetResponseAsync(messages, chatOptions);

        return response.Text;
    }
}
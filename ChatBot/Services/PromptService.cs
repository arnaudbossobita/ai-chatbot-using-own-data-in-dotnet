namespace ChatBot.Services;

public class PromptService
{
    static readonly Dictionary<string, string> Prompts = [];

    static PromptService()
    {
        var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
        foreach (var promptName in new[] { "RagSystemPrompt" })
        {
            var promptText = File.ReadAllText(Path.Combine(promptsDirectory, promptName + ".txt"));
            Prompts[promptName] = promptText;
        }
    }

    public string RagSystemPrompt => Prompts["RagSystemPrompt"];
}
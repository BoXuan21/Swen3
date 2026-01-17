namespace Swen3.Gemini.Services
{
    public interface IGeminiService
    {
        public Task<string> SendPromptAsync(string text);
    }
}

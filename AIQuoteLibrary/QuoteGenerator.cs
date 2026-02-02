using OpenAI;
using OpenAI.Chat;
using System;

namespace AIQuoteLibrary
{
    public class QuoteGenerator
    {
        private readonly string _apiKey;

        public QuoteGenerator()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY not set.");
        }

        public string GenerateQuote(string topic)
        {
            ChatClient client = new("gpt-4o", _apiKey);
            ChatCompletion completion = client.CompleteChat(
                $"Give me one short inspirational quote about {topic}.");

            return completion.Content[0].Text;
        }

        public void PrintCreators()
        {
            Console.WriteLine("AI Quote Library by Luis Palma and Felipe G Monsalvo");
        }
    }
}

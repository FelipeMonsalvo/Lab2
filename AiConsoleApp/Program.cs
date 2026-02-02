// Felipe Monsalvo and Luis Palma - CPS*3330*01
using System;
using OpenAI;
using OpenAI.Chat;
using DotNetEnv;

class Program
{
    static void Main()
    {
        Env.Load();

        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new Exception("OPENAI_API_KEY not set.");

        ChatClient client = new(model: "gpt-4o", apiKey: apiKey);

        ChatCompletion completion = client.CompleteChat("Write a short story about a happy dog");

        Console.WriteLine(completion.Content[0].Text);
    }
}

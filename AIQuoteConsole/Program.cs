using System;
using AIQuoteLibrary;
using DotNetEnv;

class Program
{
    static void Main()
    {
        Env.Load();

        Console.WriteLine("AI Quote Console App");
        Console.WriteLine("Created by Luis Palma and Felipe Monsalvo\n");

        QuoteGenerator generator = new();
        generator.PrintCreators();

        Console.Write("\nEnter a topic: ");
        string topic = Console.ReadLine() ?? "life";

        string quote = generator.GenerateQuote(topic);

        Console.WriteLine("\nGenerated Quote:");
        Console.WriteLine(quote);
    }
}

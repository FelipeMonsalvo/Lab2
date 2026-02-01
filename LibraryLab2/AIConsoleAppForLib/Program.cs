// Felipe Monsalvo and Luis Palma - CPS*3330*01 - Lab2
using AIClassLibrary;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Main()
    {
        // Uses OPENAI_API_KEY from environment
        var aiLib = new MyAILibrary();
        Console.WriteLine("--- Starting AI Library Tests ---");

        // 1) Text Generation
        Console.WriteLine("\n[Test 1] Text Generation:");
        string story = aiLib.GenerateText("Write a 1-sentence story about a sentient robot.");
        Console.WriteLine($"Result: {story}");

        // 2) Vision
        Console.WriteLine("\n[Test 2] Image Understanding:");
        Console.WriteLine($"BaseDir: {AppContext.BaseDirectory}");
        Console.WriteLine("BaseDir files:");
        foreach (var f in Directory.GetFiles(AppContext.BaseDirectory))
            Console.WriteLine(" - " + Path.GetFileName(f));

        try
        {
            string imageAnalysis = aiLib.AnalyzeImage("test_image.jpg", "What is in this image?");
            Console.WriteLine($"Result: {imageAnalysis}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Vision failed with full error:");
            Console.WriteLine(ex.ToString());
        }

        // 3) Embeddings (print + save)
        Console.WriteLine("\n[Test 3] Embeddings:");
        var vec = aiLib.GetEmbeddings("Testing AI embeddings.");
        Console.WriteLine($"Dims: {vec.Length}");
        // Print ALL embeddings (1536 floats). If too much, change to 20.
        PrintEmbedding(vec, perLine: 8, maxToPrint: vec.Length);
        SaveEmbeddingJson("embedding.json", "Testing AI embeddings.", vec);
        Console.WriteLine("Wrote embedding.json");

        // Save multiple embeddings + cosine similarity matrix
        var texts = new[]
        {
            "pizza in new jersey",
            "newark airport",
            "hiking in watchung reservation",
            "machine learning embeddings"
        };
        SaveEmbeddingsJsonl("embeddings.jsonl", texts, aiLib);
        Console.WriteLine("Wrote embeddings.jsonl");
        Console.WriteLine("\nCosine similarity matrix:");
        PrintCosineSimilarityMatrix(texts, aiLib);

        // 4) Agent
        Console.WriteLine("\n[Test 4] Agent Building:");
        string agentReply = aiLib.RunAgent("You are a helpful travel guide.", "Suggest a place in NJ.");
        Console.WriteLine(agentReply);

        Console.WriteLine("\n--- Tests Completed ---");
        Console.ReadLine();
    }

    static void PrintEmbedding(ReadOnlyMemory<float> vec, int perLine = 8, int maxToPrint = 1536)
    {
        var span = vec.Span;
        int n = Math.Min(span.Length, maxToPrint);
        for (int i = 0; i < n; i++)
        {
            Console.Write($"{i}:{span[i].ToString("0.000000", CultureInfo.InvariantCulture)} ");
            if ((i + 1) % perLine == 0) Console.WriteLine();
        }
        Console.WriteLine();
    }

    static void SaveEmbeddingJson(string path, string text, ReadOnlyMemory<float> vec)
    {
        var payload = new
        {
            text,
            dimensions = vec.Length,
            embedding = vec.ToArray()
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    static void SaveEmbeddingsJsonl(string path, string[] texts, MyAILibrary aiLib)
    {
        using var sw = new StreamWriter(path);
        foreach (var t in texts)
        {
            var v = aiLib.GetEmbeddings(t).ToArray();
            sw.WriteLine(JsonSerializer.Serialize(new { text = t, dimensions = v.Length, embedding = v }));
        }
    }

    static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Dimension mismatch.");
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            na += (double)a[i] * a[i];
            nb += (double)b[i] * b[i];
        }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-12);
    }

    static void PrintCosineSimilarityMatrix(string[] texts, MyAILibrary aiLib)
    {
        var emb = texts.Select(t => aiLib.GetEmbeddings(t).ToArray()).ToArray();
        Console.Write("".PadRight(28));
        for (int j = 0; j < texts.Length; j++)
            Console.Write($"{j}".PadLeft(10));
        Console.WriteLine();

        for (int i = 0; i < texts.Length; i++)
        {
            string label = (texts[i].Length > 25) ? texts[i].Substring(0, 25) + "…" : texts[i];
            Console.Write(label.PadRight(28));
            for (int j = 0; j < texts.Length; j++)
            {
                double c = Cosine(emb[i], emb[j]);
                Console.Write($"{c,10:0.0000}");
            }
            Console.WriteLine();
        }
    }
}

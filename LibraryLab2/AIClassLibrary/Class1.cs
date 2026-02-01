// Felipe Monsalvo and Luis Palma - CPS*3330*01 - Lab2
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ClientModel; // BinaryData
using System.Threading;

namespace AIClassLibrary
{
    public class MyAILibrary
    {
        private readonly string _apiKey;

#pragma warning disable OPENAI001
        // Choose models here
        private const string ChatModel = "gpt-4o";
        private const string EmbeddingModel = "text-embedding-3-small";

        public MyAILibrary(string? apiKey = null)
        {
            _apiKey = "OPENAI_API_KEY";
            // private const string ApiKey =
        }

        // 1) Text Generation
        public string GenerateText(string prompt)
        {
            ChatClient client = new(model: ChatModel, apiKey: _apiKey);
            ChatCompletion completion = client.CompleteChat(prompt);
            return completion.Content[0].Text;
        }

        // 2) Vision (robust path resolution + correct MIME)
        public string AnalyzeImage(string imageNameOrPath, string prompt)
        {
            ChatClient client = new(model: ChatModel, apiKey: _apiKey);
            string imagePath = ResolveImagePath(imageNameOrPath);

            Console.WriteLine($"[Vision] BaseDir: {AppContext.BaseDirectory}");
            Console.WriteLine($"[Vision] CWD : {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"[Vision] Using : {imagePath}");

            string mimeType = Path.GetExtension(imagePath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => throw new NotSupportedException(
                    $"Unsupported image type: {Path.GetExtension(imagePath)}")
            };

            BinaryData imageBytes = BinaryData.FromBytes(
                File.ReadAllBytes(imagePath));

            List<ChatMessage> messages = new()
            {
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart(prompt),
                    ChatMessageContentPart.CreateImagePart(imageBytes, mimeType)
                )
            };

            ChatCompletion completion = client.CompleteChat(messages);
            return completion.Content[0].Text;
        }

        private static string ResolveImagePath(string imageNameOrPath)
        {
            // If absolute path, use as-is (with extension fallback if missing)
            var candidates = new List<string>();

            if (Path.IsPathRooted(imageNameOrPath))
            {
                candidates.Add(imageNameOrPath);
            }
            else
            {
                // Try likely locations
                candidates.Add(Path.Combine(AppContext.BaseDirectory, imageNameOrPath)); // bin/Debug/netX
                candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), imageNameOrPath)); // current working dir

                // Walk up from BaseDirectory (bin/Debug/netX -> Debug -> bin -> project root)
                string dir = AppContext.BaseDirectory;
                for (int i = 0; i < 6; i++)
                {
                    var parent = Directory.GetParent(dir);
                    if (parent == null) break;
                    dir = parent.FullName;
                    candidates.Add(Path.Combine(dir, imageNameOrPath));
                }

                // Common subfolders if you store assets
                candidates.Add(Path.Combine(AppContext.BaseDirectory, "Assets", imageNameOrPath));
                candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Assets", imageNameOrPath));
            }

            // If no extension provided, try common ones
            bool hasExt = candidates.Any(p => Path.HasExtension(p));
            string[] exts = hasExt ? new[] { "" } : new[] { ".png", ".jpg", ".jpeg", ".webp" };

            var tried = new List<string>();

            foreach (var basePath in candidates)
            {
                foreach (var ext in exts)
                {
                    string p = basePath + ext;
                    tried.Add(p);
                    if (File.Exists(p))
                        return p;
                }
            }

            throw new FileNotFoundException(
                "Image not found. Tried:\n" + string.Join("\n", tried));
        }

        // 3) Embeddings
        public ReadOnlyMemory<float> GetEmbeddings(string text)
        {
            EmbeddingClient client = new(model: EmbeddingModel, apiKey: _apiKey);
            OpenAIEmbedding embedding = client.GenerateEmbedding(text);
            return embedding.ToFloats(); // ReadOnlyMemory<float>
        }

        // 4) Assistant Agent (returns assistant text)
        public string RunAgent(string instructions, string userMessage)
        {
            AssistantClient client = new(apiKey: _apiKey);

            Assistant assistant = client.CreateAssistant(
                ChatModel,
                new AssistantCreationOptions { Instructions = instructions });

            ThreadCreationOptions threadOptions = new()
            {
                InitialMessages = { userMessage }
            };

            ThreadRun run = client.CreateThreadAndRun(assistant.Id, threadOptions);

            // Poll until terminal
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                run = client.GetRun(run.ThreadId, run.Id);
            }
            while (!run.Status.IsTerminal);

            if (run.Status != RunStatus.Completed)
                return $"Run ended with status: {run.Status}";

            // Fetch messages
            var messages = client.GetMessages(
                run.ThreadId,
                new MessageCollectionOptions
                {
                    Order = MessageCollectionOrder.Ascending
                });

            #pragma warning disable CS8600
            string lastAssistant = messages
                .Where(m => m.Role == MessageRole.Assistant)
                .SelectMany(m => m.Content)
                .Select(c => c.Text)
                .LastOrDefault(t => !string.IsNullOrWhiteSpace(t));
            #pragma warning restore CS8600


            return lastAssistant ?? "(No assistant text returned.)";
        }
    }
}

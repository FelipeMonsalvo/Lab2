// Felipe Monsalvo and Luis Palma - CPS*3330*01 - Lab2
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Embeddings;
using DotNetEnv;
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

        public MyAILibrary()
        {
            // Simple env (should be a bit more safe)
            Env.Load();

            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException(
                    "OPENAI_API_KEY not found. Make sure it exists in your .env file.");
        }

        // 1) Text Generation
        public string GenerateText(string prompt)
        {
            ChatClient client = new(model: ChatModel, apiKey: _apiKey);
            ChatCompletion completion = client.CompleteChat(prompt);
            return completion.Content[0].Text;
        }

        public string AnalyzeImage(string imageNameOrPath, string prompt)
        {
            ChatClient client = new(model: ChatModel, apiKey: _apiKey);
            string imagePath = ResolveImagePath(imageNameOrPath);

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
            var candidates = new List<string>();

            if (Path.IsPathRooted(imageNameOrPath))
            {
                candidates.Add(imageNameOrPath);
            }
            else
            {
                candidates.Add(Path.Combine(AppContext.BaseDirectory, imageNameOrPath));
                candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), imageNameOrPath));

                string dir = AppContext.BaseDirectory;
                for (int i = 0; i < 6; i++)
                {
                    var parent = Directory.GetParent(dir);
                    if (parent == null) break;
                    dir = parent.FullName;
                    candidates.Add(Path.Combine(dir, imageNameOrPath));
                }
            }

            string[] exts = Path.HasExtension(imageNameOrPath)
                ? new[] { "" }
                : new[] { ".png", ".jpg", ".jpeg", ".webp" };

            foreach (var basePath in candidates)
            {
                foreach (var ext in exts)
                {
                    string p = basePath + ext;
                    if (File.Exists(p))
                        return p;
                }
            }

            throw new FileNotFoundException("Image not found.");
        }

        // 3) Embeddings
        public ReadOnlyMemory<float> GetEmbeddings(string text)
        {
            EmbeddingClient client = new(model: EmbeddingModel, apiKey: _apiKey);
            OpenAIEmbedding embedding = client.GenerateEmbedding(text);
            return embedding.ToFloats();
        }

        // 4) Assistant Agent
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

            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                run = client.GetRun(run.ThreadId, run.Id);
            }
            while (!run.Status.IsTerminal);

            if (run.Status != RunStatus.Completed)
                return $"Run ended with status: {run.Status}";

            var messages = client.GetMessages(run.ThreadId);

            string lastAssistant = messages
                .Where(m => m.Role == MessageRole.Assistant)
                .SelectMany(m => m.Content)
                .Select(c => c.Text)
                .LastOrDefault(t => !string.IsNullOrWhiteSpace(t));

            return lastAssistant ?? "(No assistant text returned.)";
        }
    }
}
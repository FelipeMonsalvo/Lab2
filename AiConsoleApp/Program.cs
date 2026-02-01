// Felipe Monsalvo and Luis Palma - CPS*3330*01
using OpenAI;
using OpenAI.Chat;
string apiKey = "OPENAI_API_KEY";
ChatClient client = new(model: "gpt-4o", apiKey: apiKey);
ChatCompletion completion = client.CompleteChat("Write a short story about a happy dog");
Console.WriteLine(completion.Content[0].Text);

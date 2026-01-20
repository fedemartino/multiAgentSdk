using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Google.GenAI;
using Anthropic;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Agents.AI;

DotNetEnv.Env.TraversePath().Load();

var clients = new Dictionary<string, IChatClient>();

IChatClient client = new ChatClient(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")).AsIChatClient();
clients.Add("openai", client);

client = new Google.GenAI.Client(apiKey: Environment.GetEnvironmentVariable("GOOGLE_API_KEY")).AsIChatClient("gemini-2.0-flash");
clients.Add("google", client);

client = new Anthropic.AnthropicClient() { 
    ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") 
    }.AsIChatClient("claude-haiku-4-5");
clients.Add("anthropic", client);

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
}));

// foreach (var c in clients)
// {
//     var response = await c.Value.GetResponseAsync("What is AI? Give me a short answer using less than 100 words.");
//     Console.WriteLine($"[ASSISTANT {c.Key}]: {response}");
// }
var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
var agent = new ChatClient(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")).AsIChatClient()
    .CreateAIAgent(
        instructions: "You answer questions related to GitHub repositories only.",
        tools: [.. mcpTools.Cast<AITool>()]);
var agentResponse = await agent.RunAsync("List the top 5 most starred repositories on GitHub.");

Console.WriteLine($"[AGENT RESPONSE]: {agentResponse}");

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace ChatGPTClone
{
    // Represents a single message in the conversation (user or AI)
    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty; // The actual message text
    }

    // Represents the request payload sent to OpenAI API
    public class ChatRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty; // AI model to use (e.g., "gpt-3.5-turbo")
        
        [JsonProperty("messages")]
        public List<Message> Messages { get; set; } = new(); // List of conversation messages
        
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } // Maximum tokens in the response
        
        [JsonProperty("temperature")]
        public double Temperature { get; set; } // Controls randomness (0.0 = deterministic, 1.0 = creative)
    }

    // Represents the response received from OpenAI API
    public class ChatResponse
    {
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; } = new(); // List of response choices from AI
    }

    // Represents a single choice in the AI response
    public class Choice
    {
        [JsonProperty("message")]
        public Message Message { get; set; } = new(); // The actual message content from AI
    }

    // Represents the saved conversation history structure
    public class ConversationHistory
    {
        public List<Message> Messages { get; set; } = new(); // All messages in the conversation
    }

    class Program
    {
        // HTTP client for making API requests to OpenAI
        private static readonly HttpClient httpClient = new HttpClient();
        // Configuration object to access settings from appsettings.json
        private static IConfiguration? configuration;
        // List to store the current conversation history in memory
        private static List<Message> conversationHistory = new List<Message>();

        // Main entry point of the application
        static async Task Main(string[] args)
        {
            // Create configuration builder to read from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set current directory as base path
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); // Load JSON config file
            
            // Build the configuration object
            configuration = builder.Build();

            // Load any existing conversation history from file
            await LoadConversationHistory();

            // Display welcome message and instructions
            Console.WriteLine("ChatGPT Clone - Type 'exit' to quit, 'clear' to clear history");
            Console.WriteLine("================================================");

            // Check if we're running in test mode (with command line arguments)
            if (args.Length > 0)
            {
                // Test mode - process the first argument as a message
                await ProcessTestMessage(args[0]);
                return;
            }

            // Main chat loop - runs until user types 'exit'
            while (true)
            {
                // Prompt user for input
                Console.Write("\nYou: ");
                // Read user input from console
                string? userInput = Console.ReadLine();

                // Skip empty or whitespace-only input
                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                // Check if user wants to exit the application
                if (userInput.ToLower() == "exit")
                {
                    // Save conversation before exiting
                    await SaveConversationHistory();
                    Console.WriteLine("Goodbye!");
                    break; // Exit the main loop
                }

                // Check if user wants to clear conversation history
                if (userInput.ToLower() == "clear")
                {
                    // Clear the in-memory conversation history
                    conversationHistory.Clear();
                    // Save the empty history to file
                    await SaveConversationHistory();
                    Console.WriteLine("Conversation history cleared!");
                    continue; // Skip to next iteration
                }

                try
                {
                    // Process the user message
                    await ProcessUserMessage(userInput);
                }
                catch (Exception ex)
                {
                    // Display any errors that occur during AI communication
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        // Method to process a user message and get AI response
        static async Task ProcessUserMessage(string userInput)
        {
            // Add user's message to the conversation history
            conversationHistory.Add(new Message { Role = "user", Content = userInput });

            // Send request to AI and get response
            string aiResponse = await GetAIResponse(userInput);
            
            // Add AI's response to the conversation history
            conversationHistory.Add(new Message { Role = "assistant", Content = aiResponse });

            // Display AI's response to the user
            Console.WriteLine($"\nAI: {aiResponse}");

            // Save the updated conversation history to file
            await SaveConversationHistory();
        }

        // Method to process a test message (for automated testing)
        static async Task ProcessTestMessage(string testMessage)
        {
            Console.WriteLine($"\nTest Mode - Processing message: {testMessage}");
            
            try
            {
                // Add user's message to the conversation history
                conversationHistory.Add(new Message { Role = "user", Content = testMessage });

                // Send request to AI and get response
                string aiResponse = await GetAIResponse(testMessage);
                
                // Add AI's response to the conversation history
                conversationHistory.Add(new Message { Role = "assistant", Content = aiResponse });

                // Display AI's response
                Console.WriteLine($"\nAI: {aiResponse}");

                // Save the updated conversation history to file
                await SaveConversationHistory();
                
                Console.WriteLine("\nTest completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed with error: {ex.Message}");
            }
        }

        // Method to send user input to OpenAI API and get AI response
        static async Task<string> GetAIResponse(string userInput)
        {
            // Get API key from configuration, throw exception if not found
            string apiKey = configuration!["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found in configuration");
            // Get AI model from configuration, default to gpt-3.5-turbo if not specified
            string model = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
            // Get maximum tokens from configuration, default to 1000
            int maxTokens = int.Parse(configuration["OpenAI:MaxTokens"] ?? "1000");
            // Get temperature setting from configuration, default to 0.7
            double temperature = double.Parse(configuration["OpenAI:Temperature"] ?? "0.7");

            // Create list to hold all messages for the API request
            var messages = new List<Message>();
            
            // Get maximum number of history messages to include (prevents token overflow)
            int maxHistoryMessages = int.Parse(configuration["App:MaxHistoryMessages"] ?? "20");
            // Take only the most recent messages from conversation history
            var recentHistory = conversationHistory.TakeLast(maxHistoryMessages).ToList();
            // Add the recent conversation history to the messages list
            messages.AddRange(recentHistory);

            // Create the request object with all necessary parameters
            var request = new ChatRequest
            {
                Model = model, // AI model to use
                Messages = messages, // Conversation messages
                MaxTokens = maxTokens, // Maximum response length
                Temperature = temperature // Response creativity level
            };

            // Convert request object to JSON string using JsonProperty attributes
            string jsonContent = JsonConvert.SerializeObject(request);
            // Create HTTP content with JSON string and proper encoding
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Clear any existing headers and add authorization header
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            // Send POST request to OpenAI API endpoint
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            
            // Check if the API request was successful
            if (!response.IsSuccessStatusCode)
            {
                // Read error content from response
                string errorContent = await response.Content.ReadAsStringAsync();
                // Throw exception with error details
                throw new Exception($"API request failed: {response.StatusCode} - {errorContent}");
            }

            // Read the successful response content
            string responseContent = await response.Content.ReadAsStringAsync();
            // Deserialize JSON response into ChatResponse object
            var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(responseContent);

            // Check if we received a valid response with choices
            if (chatResponse?.Choices?.Count > 0)
            {
                // Return the content of the first (and usually only) choice
                return chatResponse.Choices[0].Message.Content;
            }

            // Throw exception if no valid response was received
            throw new Exception("No response received from AI");
        }

        // Method to load conversation history from file when application starts
        static async Task LoadConversationHistory()
        {
            try
            {
                // Get the history file name from configuration, default to "conversation_history.json"
                string historyFile = configuration!["App:ConversationHistoryFile"] ?? "conversation_history.json";
                
                // Check if the history file exists
                if (File.Exists(historyFile))
                {
                    // Read all text content from the history file
                    string jsonContent = await File.ReadAllTextAsync(historyFile);
                    // Deserialize JSON content into ConversationHistory object
                    var history = JsonConvert.DeserializeObject<ConversationHistory>(jsonContent);
                    // Set conversation history to loaded messages, or empty list if null
                    conversationHistory = history?.Messages ?? new List<Message>();
                    // Inform user how many messages were loaded
                    Console.WriteLine($"Loaded {conversationHistory.Count} messages from conversation history.");
                }
            }
            catch (Exception ex)
            {
                // If loading fails, show warning and start with empty history
                Console.WriteLine($"Warning: Could not load conversation history: {ex.Message}");
                conversationHistory = new List<Message>();
            }
        }

        // Method to save current conversation history to file
        static async Task SaveConversationHistory()
        {
            try
            {
                // Get the history file name from configuration, default to "conversation_history.json"
                string historyFile = configuration!["App:ConversationHistoryFile"] ?? "conversation_history.json";
                // Create ConversationHistory object with current messages
                var history = new ConversationHistory { Messages = conversationHistory };
                // Serialize history object to JSON with indented formatting
                string jsonContent = JsonConvert.SerializeObject(history, Formatting.Indented);
                // Write JSON content to the history file
                await File.WriteAllTextAsync(historyFile, jsonContent);
            }
            catch (Exception ex)
            {
                // If saving fails, show warning but don't crash the application
                Console.WriteLine($"Warning: Could not save conversation history: {ex.Message}");
            }
        }
    }
}
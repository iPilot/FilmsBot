using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IResult = Discord.Interactions.IResult;

namespace FilmsBot.Commands
{
    public class FilmsInteractionService : InteractionService
    {
        private readonly IServiceProvider _serviceProvider;

        public FilmsInteractionService(DiscordSocketClient client, IServiceProvider serviceProvider) : base(client, new InteractionServiceConfig
        {
            EnableAutocompleteHandlers = true,
            AutoServiceScopes = true,
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Verbose,
            UseCompiledLambda = true
        })
        {
            _serviceProvider = serviceProvider;
            SlashCommandExecuted += PostExecution;
        }

        private static async Task PostExecution(SlashCommandInfo command, IInteractionContext context, IResult result)
        {
            if (context.Interaction.HasResponded)
                return;

            await context.Interaction.RespondAsync(result.ToString(), ephemeral: !result.IsSuccess);
        }

        public async Task Initialize()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                await AddModulesAsync(assembly, _serviceProvider);
            }

            await RegisterCommandsGloballyAsync();
        }
    }
}
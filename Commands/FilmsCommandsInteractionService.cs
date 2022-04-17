using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
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
            if (context.Interaction.HasResponded || result.Equals(CommandResult.DefaultSuccess))
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

        public async Task ProcessButtonExecution(SocketMessageComponent component)
        {
            if (component.Data.Type != ComponentType.Button)
            {
                await component.RespondAsync("invalid component");
                return;
            }

            using var module = _serviceProvider.GetRequiredService<FilmsInteractionModule>();
            module.BeforeExecute(null);

            await module.ProcessButtonExecution(component);
        }
    }
}
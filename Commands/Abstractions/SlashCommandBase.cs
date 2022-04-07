using Discord;
using Discord.WebSocket;

namespace FilmsBot.Commands.Abstractions
{
    public abstract class SlashCommandBase : ISlashCommandHandler
    {
        private SlashCommandProperties? _command;
        public SlashCommandProperties Command => _command ??= BuildCommand();

        public abstract string Name { get; }
        protected abstract string Description { get; }

        public abstract Task HandleCommand(SocketSlashCommand command);

        private SlashCommandProperties BuildCommand()
        {
            var builder = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description);

            foreach (var subCommand in GetSubCommands())
            {
                builder.AddOption(subCommand);
            }

            return builder.Build();
        }

        protected abstract IEnumerable<SlashCommandOptionBuilder> GetSubCommands();
    }
}
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace FilmsBot.Commands.Abstractions
{
    public abstract class SlashSubCommandBase<TCommand> : InteractionModuleBase<SocketInteractionContext>, ISlashSubCommandHandler<TCommand>
        where TCommand : SlashCommandBase
    {
        public abstract string Name { get; }
        public abstract Task HandleCommand(SocketSlashCommand command, SocketSlashCommandDataOption options);

        public SlashCommandOptionBuilder GetOptionsBuilder()
        {
            return ConfigureSubCommand(
                new SlashCommandOptionBuilder()
                    .WithName(Name)
                    .WithDescription(Description)
                    .WithType(Type));
        }

        protected abstract string Description { get; }
        protected virtual ApplicationCommandOptionType Type => ApplicationCommandOptionType.SubCommand;
        protected virtual SlashCommandOptionBuilder ConfigureSubCommand(SlashCommandOptionBuilder builder) => builder;
    }
}
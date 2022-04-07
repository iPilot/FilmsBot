using Discord;
using Discord.WebSocket;

namespace FilmsBot.Commands.Abstractions
{
    public interface ISlashSubCommandHandler<TCommand>
        where TCommand : SlashCommandBase
    {
        string Name { get; }
        Task HandleCommand(SocketSlashCommand command, SocketSlashCommandDataOption options);
        SlashCommandOptionBuilder GetOptionsBuilder();
    }
}
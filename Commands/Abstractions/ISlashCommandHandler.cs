using Discord;
using Discord.WebSocket;

namespace FilmsBot.Commands.Abstractions
{
    public interface ISlashCommandHandler
    {
        SlashCommandProperties Command { get; }
        string Name { get; }
        Task HandleCommand(SocketSlashCommand command);
    }
}
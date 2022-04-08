using Discord.Interactions;

namespace FilmsBot.Commands.Abstractions
{
    public class CommandResult : RuntimeResult
    {
        public CommandResult(InteractionCommandError? error, string reason) : base(error, reason)
        {
        }

        public static readonly CommandResult Success = new(null, string.Empty);
    }
}
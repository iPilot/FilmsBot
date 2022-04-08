using Discord.Interactions;

namespace FilmsBot.Commands.Abstractions
{
    public class CommandResult : RuntimeResult
    {
        public CommandResult(InteractionCommandError? error, string response) : base(error, response)
        {
        }

        public CommandResult(string response) : base(null, response)
        {
        }

        public override string ToString() => IsSuccess ? ErrorReason : $"Error: {ErrorReason}";
        public static readonly CommandResult DefaultSuccess = new("");
    }
}
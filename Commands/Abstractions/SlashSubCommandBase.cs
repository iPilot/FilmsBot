using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;

namespace FilmsBot.Commands.Abstractions
{
    public abstract class SlashSubCommandBase : InteractionModuleBase<SocketInteractionContext>, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        protected ulong UserId => Context.User.Id;
        protected IServiceScope? Scope;

        protected SlashSubCommandBase(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected bool ValidateIfGuild([NotNullWhen(true)] out IGuildChannel? guildChannel, [NotNullWhen(false)] out RuntimeResult? result)
        {
            if (Context.Channel is IGuildChannel g)
            {
                guildChannel = g;
                result = null;
                return true;
            }

            guildChannel = null;
            result = new CommandResult(InteractionCommandError.UnmetPrecondition, "Not in a guild");
            return false;
        }

        public override void BeforeExecute(ICommandInfo command)
        {
            Scope = _scopeFactory.CreateScope();
        }

        public override async Task AfterExecuteAsync(ICommandInfo command)
        {
            Scope?.Dispose();
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            Scope?.Dispose();
            GC.SuppressFinalize(this);
        }

        ~SlashSubCommandBase()
        {
            Dispose();
        }
    }
}
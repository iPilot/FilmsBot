using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands.Abstractions
{
    public abstract class DbInteractionSubCommand<TContext> : SlashSubCommandBase
        where TContext : DbContext
    {
        protected TContext DbContext = null!;

        protected DbInteractionSubCommand(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
        }

        public override void BeforeExecute(ICommandInfo command)
        {
            base.BeforeExecute(command);
            DbContext = Scope!.ServiceProvider.GetRequiredService<TContext>();
        }
    }
}
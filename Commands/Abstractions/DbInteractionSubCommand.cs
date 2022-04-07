using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace FilmsBot.Commands.Abstractions
{
    public abstract class DbInteractionSubCommand<TCommand, TContext> : SlashSubCommandBase<TCommand>
        where TContext : DbContext
        where TCommand : SlashCommandBase
    {
        private readonly IServiceScopeFactory _scopeFactory;

        protected DbInteractionSubCommand(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override async Task HandleCommand(SocketSlashCommand command, SocketSlashCommandDataOption options)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            
            string? r;
            try
            {
                r = await HandleCommandInternal(scope.ServiceProvider, db, command, options);
            }
            catch (Exception)
            {
                await command.RespondAsync("Error");
                return;
            }

            if (!string.IsNullOrWhiteSpace(r))
            {
                await command.RespondAsync(r);
                return;
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                await command.RespondAsync("DB ERROR");
                return;
            }

            if (!command.HasResponded)
                await command.RespondAsync("ОК");
        }

        protected abstract Task<string?> HandleCommandInternal(IServiceProvider serviceProvider, TContext db, SocketSlashCommand command, SocketSlashCommandDataOption options);
    }
}
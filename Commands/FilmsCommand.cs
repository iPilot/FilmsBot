using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;

namespace FilmsBot.Commands
{
    public class FilmsCommand : SlashCommandBase
    {
        private readonly Dictionary<string, ISlashSubCommandHandler<FilmsCommand>> _filmsSubCommand;
        public override string Name => "фильмы";
        protected override string Description => "Команды для управления сеансами просмотра фильмов";

        public FilmsCommand(IEnumerable<ISlashSubCommandHandler<FilmsCommand>> filmsSubCommand)
        {
            _filmsSubCommand = filmsSubCommand.ToDictionary(s => s.Name);
        }

        public override async Task HandleCommand(SocketSlashCommand command)
        {
            var sub = command.Data.Options.FirstOrDefault();
            if (sub is { Type: ApplicationCommandOptionType.SubCommand } && _filmsSubCommand.TryGetValue(sub.Name, out var subCommand))
            {
                await subCommand.HandleCommand(command, sub);
            }
            else
            { 
                await command.RespondAsync("Error");
            }
        }

        protected override IEnumerable<SlashCommandOptionBuilder> GetSubCommands()
        {
            return _filmsSubCommand.Select(c => c.Value.GetOptionsBuilder());
        }
    }
}
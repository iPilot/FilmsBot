using Discord;
using Discord.WebSocket;
using FilmsBot.Extensions;

namespace FilmsBot.Commands.Abstractions
{
    public class CommandOptionHandler<T>
    {
        private readonly string _name;
        private readonly string _description;
        private readonly ApplicationCommandOptionType _type;

        public CommandOptionHandler(string name, string description, ApplicationCommandOptionType type)
        {
            _name = name;
            _description = description;
            _type = type;
        }

        public SlashCommandOptionBuilder GetBuilder(Action<SlashCommandOptionBuilder> configure)
        {
            var b = new SlashCommandOptionBuilder()
                .WithName(_name)
                .WithDescription(_description)
                .WithType(_type);

            configure(b);

            return b;
        }

        public T GetOptionValue(SocketSlashCommandDataOption option) => option.GetOptionValue<T>(_name);
    }
}
using Discord.Interactions;
using FilmsBot.Database;

namespace FilmsBot.Commands
{
    [Group("фильмы", "Менеджмент совместных просмотров фильмов")]
    public class FilmsInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly FilmsBotDbContext _filmsDb;

        public FilmsInteractionModule(FilmsBotDbContext filmsDb)
        {
            _filmsDb = filmsDb;
        }

        [SlashCommand("все", "Список всех фильмов")]
        public async Task AllFilms()
        {
            await Context.Interaction.RespondAsync("ВСЕ");
        }
    }
}
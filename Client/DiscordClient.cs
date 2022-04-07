using Discord;
using Discord.WebSocket;
using FilmsBot.Commands.Abstractions;
using ILogger = Serilog.ILogger;

namespace FilmsBot.Client
{
    public partial class DiscordClient : IDisposable
    {
        private readonly Dictionary<string, ISlashCommandHandler> _slashCommands;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _tokenSource = new(-1);
        
        public DiscordClient(
            ILogger logger,
            IEnumerable<ISlashCommandHandler> slashCommands,
            IConfiguration configuration,
            DiscordSocketClient client)
        {
            _slashCommands = slashCommands.ToDictionary(c => c.Name);
            _logger = logger;
            _configuration = configuration;
            _client = client;
            _thread = new Thread(_ => WaitForShutdown(_tokenSource.Token));
        }

        public void Run()
        {
            _thread.Start();
        }

        public async void WaitForShutdown(CancellationToken cancellationToken)
        {
            await using var x = _client;
            
            ConfigureEvents();

            await _client.LoginAsync(TokenType.Bot, _configuration["Token"]);
            await _client.StartAsync();

            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void ConfigureEvents()
        {
            _client.Log += msg =>
            {
                var logger = _logger.ForContext("Source", msg.Source);
                switch (msg.Severity)
                {
                    case LogSeverity.Critical:
                        logger.Fatal(msg.Exception, msg.Message);
                        break;
                    case LogSeverity.Error:
                        logger.Error(msg.Exception, msg.Message);
                        break;
                    case LogSeverity.Warning:
                        logger.Warning(msg.Exception, msg.Message);
                        break;
                    case LogSeverity.Info:
                        logger.Information(msg.Exception, msg.Message);
                        break;
                    case LogSeverity.Verbose:
                        logger.Verbose(msg.Exception, msg.Message);
                        break;
                    case LogSeverity.Debug:
                        logger.Debug(msg.Exception, msg.Message);
                        break;
                }
                return Task.CompletedTask;
            };
            //_client.Ready += RegisterCommands;
            //_client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (!_slashCommands.TryGetValue(command.CommandName, out var slashCommand))
            {
                await command.RespondAsync("Error");
                return;
            }

            await slashCommand.HandleCommand(command);
        }

        private async Task RegisterCommands()
        {
            var registered = await _client.GetGlobalApplicationCommandsAsync();
            foreach (var command in registered)
            {
#if DEBUG
                await command.DeleteAsync();
#else
                if (!_slashCommands.ContainsKey(command.Name))
                    await command.DeleteAsync();
#endif
            }

            //await _client.BulkOverwriteGlobalApplicationCommandsAsync(_slashCommands.Select(c => c.Value.Command).Cast<ApplicationCommandProperties>().ToArray());
        }

        private void ReleaseUnmanagedResources()
        {
            _tokenSource.Cancel();
            _thread.Join();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DiscordClient()
        {
            Dispose(false);
        }
    }
}
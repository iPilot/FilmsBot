using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FilmsBot.Commands;
using ILogger = Serilog.ILogger;

namespace FilmsBot.Client
{
    public class DiscordClient : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FilmsInteractionService _filmsInteractionService;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _tokenSource = new(-1);
        
        public DiscordClient(
            IServiceProvider serviceProvider,
            FilmsInteractionService filmsInteractionService,
            ILogger logger,
            IConfiguration configuration,
            DiscordSocketClient client)
        {
            _serviceProvider = serviceProvider;
            _filmsInteractionService = filmsInteractionService;
            _logger = logger;
            _configuration = configuration;
            _client = client;
            _thread = new Thread(_ => WaitForShutdown(_tokenSource.Token));
        }

        public void Run()
        {
            _thread.Start();
        }

        private async void WaitForShutdown(CancellationToken cancellationToken)
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
            _client.Ready += RegisterCommands;
            _client.InteractionCreated += HandleInteraction;
            _client.ButtonExecuted += ButtonExecuted;
        }

        private Task ButtonExecuted(SocketMessageComponent arg)
        {
            return arg.Data.Type switch
            {
                ComponentType.Button => _filmsInteractionService.ProcessButtonExecution(arg),
                _ => arg.RespondAsync("invalid component")
            };
        }

        private Task HandleInteraction(SocketInteraction arg)
        {
            return _filmsInteractionService.ExecuteCommandAsync(new SocketInteractionContext(_client, arg), _serviceProvider);
        }

        private Task RegisterCommands()
        {
            return _filmsInteractionService.Initialize();
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
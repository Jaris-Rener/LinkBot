namespace CalendarBot;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

internal class Program
{
    private static AppSecrets _secrets { get; set; } = null!;
    private static AppConfig _config { get; set; } = null!;
    private static DiscordClient _client { get; set; } = null!;

    private static CommandsNextExtension _commands { get; set; } = null!;

    private static async Task Main(string[] args)
    {
        LoadConfigs();

        // Initialise
        var discordConfig = new DiscordConfiguration
        {
            Intents = DiscordIntents.All,
            Token = _secrets.DiscordToken,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        };

        // Create client
        _client = new DiscordClient(discordConfig);

        // Setup Google services
        var credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "config", "credentials.json");
        GoogleCredential credentials = GoogleCredential.FromFile(credentialsPath)
            .CreateScoped([CalendarService.Scope.CalendarEvents]);

        var calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credentials,
            ApplicationName = "Discord Calendar Bot"
        });

        CalendarSync.Init(calendarService, _config);

        // Setup hooks
        _client.Ready += OnClientReady;
        _client.ScheduledGuildEventCreated += OnGuildEventCreated;
        _client.ScheduledGuildEventUpdated += OnGuildEventUpdated;
        _client.ScheduledGuildEventDeleted += OnGuildEventDeleted;

        // Setup commands
        var commandsConfig = new CommandsNextConfiguration
        {
            StringPrefixes = new[] { "!" },
            EnableMentionPrefix = true,
            EnableDms = false,
            EnableDefaultHelp = false
        };

        _commands = _client.UseCommandsNext(commandsConfig);
        _commands.RegisterCommands<CalendarCommands>();

        // Connect
        await _client.ConnectAsync();
        await Task.Delay(-1);
    }

    private static void LoadConfigs()
    {
        // Load config
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config/config.json", optional: false);

        IConfiguration config = builder.Build();

        _config = config.GetSection("AppConfig").Get<AppConfig>();
        _secrets = config.GetSection("AppSecrets").Get<AppSecrets>();
    }

    private static Task OnGuildEventCreated(DiscordClient sender, ScheduledGuildEventCreateEventArgs args)
        =>
            CalendarSync.CreateOrUpdateEvent(args.Event);

    private static Task OnGuildEventUpdated(DiscordClient sender, ScheduledGuildEventUpdateEventArgs args)
        =>
            CalendarSync.CreateOrUpdateEvent(args.EventAfter);

    private static Task OnGuildEventDeleted(DiscordClient sender, ScheduledGuildEventDeleteEventArgs args)
        =>
            CalendarSync.DeleteEvent(args.Event);

    private static Task OnClientReady(DiscordClient sender, ReadyEventArgs args)
    {
        return Task.CompletedTask;
    }
}
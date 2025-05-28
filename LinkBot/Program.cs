namespace Howl.LinkBot;

using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;

internal class Program
{
    public static Config Config { get; private set; }
    public static DiscordClient Client { get; private set; }

    private const DiscordIntents _discordIntents = DiscordIntents.AllUnprivileged |
                                                   DiscordIntents.MessageContents |
                                                   DiscordIntents.GuildEmojisAndStickers;

    private static async Task Main(string[] args)
    {
        Config = await LoadConfig();
        await LinkHandler.LoadReplacements();

        DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(Config.DiscordToken, _discordIntents);

        builder.ConfigureEventHandlers(b => { b.HandleMessageCreated(LinkHandler.HandleMessage); });

        builder.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(1)
        });

        // Setup the commands extension
        builder.UseCommands((IServiceProvider serviceProvider, CommandsExtension extension) =>
        {
            extension.AddCommands([typeof(LinkCommands)]);
            SlashCommandProcessor slashCommandProcessor = new();

            // Add text commands with a custom prefix (?ping)
            extension.AddProcessor(slashCommandProcessor);
        }, new CommandsConfiguration()
        {
            // The default value is true, however it's shown here for clarity
            RegisterDefaultCommandProcessors = true
        });

        Client = builder.Build();
        await Client.ConnectAsync();
        await Task.Delay(-1);
    }

    private static async Task<Config> LoadConfig()
    {
        var json = await File.ReadAllTextAsync("config/config.json");
        var config = JsonConvert.DeserializeObject<Config>(json);
        return config ?? new Config();
    }
}
namespace Howl.LinkBot;

using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;

internal class Program
{
    public static Config Config { get; private set; }
    public static DiscordClient Client { get; private set; }

    private const DiscordIntents _discordIntents = DiscordIntents.AllUnprivileged |
                                                   DiscordIntents.MessageContents;

    private static async Task Main(string[] args)
    {
        Config = await LoadConfig();
        DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(Config.DiscordToken, _discordIntents);

        builder.ConfigureEventHandlers(b => { b.HandleMessageCreated(LinkHandler.HandleMessage); });

        builder.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(1)
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
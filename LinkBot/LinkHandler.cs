namespace Howl.LinkBot;

using System.ComponentModel;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;

public class LinkCommands
{
    [Command("list")]
    [Description("Lists registered host replacements")]
    public static async ValueTask ListReplacementsCommands(CommandContext ctx)
    {
        await ReplyWithList(ctx);
    }

    private static async Task ReplyWithList(CommandContext ctx)
    {
        var description = string.Empty;
        foreach (var pair in LinkHandler.ReplacementLookup)
            description += $"🔗**{pair.Key}**{Environment.NewLine}✨{pair.Value}{Environment.NewLine}{Environment.NewLine}";

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Replacements")
            .WithColor(DiscordColor.Blurple)
            .WithDescription(description);

        await ctx.RespondAsync(embed);
    }

    [Command("add")]
    [Description("Register or update a host replacement")]
    public static async ValueTask AddReplacement(CommandContext ctx,
        string host,
        string replacement)
    {
        await LinkHandler.AddReplacement(host, replacement);
        await ReplyWithList(ctx);
    }

    [Command("remove")]
    [Description("Remove a registered host replacement")]
    public static async ValueTask RemoveReplacement(CommandContext ctx,
        string host)
    {
        await LinkHandler.RemoveReplacement(host);
        await ReplyWithList(ctx);
    }
}

public static class LinkHandler
{
    private static readonly DiscordEmoji _replaceEmoji = DiscordEmoji.FromUnicode("\ud83d\udce4");
    private static readonly DiscordEmoji _addEmoji = DiscordEmoji.FromUnicode("\ud83d\udce5");
    private static readonly DiscordEmoji _deleteEmoji = DiscordEmoji.FromUnicode("\ud83d\uddd1\ufe0f");

    private static readonly Regex _uriRegex = new(@"(http|ftp|https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])");

    private static readonly Dictionary<string, string> _fallbackReplacementLookup = new()
    {
        { "instagram.com", "instagramez.com" },
        { "x.com", "fixupx.com" },
        { "bsky.app", "bskyx.app" },
        { "vt.tiktok.com", "vt.vxtiktok.com" },
    };

    public static Dictionary<string, string> ReplacementLookup { get; private set; } = new();

    public static async Task HandleMessage(DiscordClient c, MessageCreatedEventArgs e)
    {
        var match = _uriRegex.Match(e.Message.Content);
        if (!match.Success)
            return;

        bool replace = false;
        foreach (var pair in ReplacementLookup)
        {
            if (match.ToString().Contains(pair.Value))
                return;

            if (match.ToString().Contains(pair.Key))
            {
                replace = true;
                break;
            }
        }

        if (!replace)
            return;

        await e.Message.CreateReactionAsync(_replaceEmoji);
        await e.Message.CreateReactionAsync(_addEmoji);
        await WaitForReactionAsync(c, e);
    }

    private static async Task WaitForReactionAsync(DiscordClient c, MessageCreatedEventArgs e)
    {
        var result = await e.Message.WaitForReactionAsync(e.Author);
        if (result.TimedOut)
            return;

        if (result.Result.Emoji.Name == _replaceEmoji.Name)
        {
            await ReplaceMessage(c, e);
            return;
        }

        if (result.Result.Emoji.Name == _addEmoji.Name)
        {
            await AddMessage(c, e);
            return;
        }

        await WaitForReactionAsync(c, e);

        await e.Message.DeleteReactionsEmojiAsync(_addEmoji);
        await e.Message.DeleteReactionsEmojiAsync(_replaceEmoji);
    }

    private static async Task AddMessage(DiscordClient c, MessageCreatedEventArgs e)
    {
        await e.Message.ModifyEmbedSuppressionAsync(true);
        await SendFixedMessage(c, e, false);
    }

    private static async Task ReplaceMessage(DiscordClient c, MessageCreatedEventArgs e)
    {
        await SendFixedMessage(c, e, true);
    }

    private static async Task SendFixedMessage(DiscordClient c, MessageCreatedEventArgs e, bool deleteOriginal)
    {
        var match = _uriRegex.Match(e.Message.Content);
        if (!match.Success)
            return;

        await e.Message.DeleteReactionsEmojiAsync(_addEmoji);
        await e.Message.DeleteReactionsEmojiAsync(_replaceEmoji);

        var oldUri = match.ToString();
        var newHost = LookupHostReplacement(oldUri);
        if (string.IsNullOrEmpty(newHost))
            return;

        var uri = new UriBuilder(oldUri);
        var oldHost = uri.Host;
        uri.Host = newHost;

        var description = e.Message.Content.Replace(oldHost, newHost);
        description = $"**{e.Message.Author?.Mention}:**{Environment.NewLine}> {description}";

        if (deleteOriginal)
            await e.Message.DeleteAsync("Replaced with fixed link");

        var message = new DiscordMessageBuilder()
            .WithContent(description);

        var reply = await Program.Client.SendMessageAsync(e.Channel, message);
        await reply.CreateReactionAsync(_deleteEmoji);
        var result = await reply.WaitForReactionAsync(e.Author, TimeSpan.FromMinutes(3));
        if (result.TimedOut)
        {
            await reply.DeleteReactionsEmojiAsync(_deleteEmoji);
            return;
        }

        if (result.Result.Emoji.Name == _deleteEmoji.Name)
        {
            await reply.DeleteAsync();
            return;
        }

        await reply.DeleteReactionsEmojiAsync(_deleteEmoji);
    }

    private static string LookupHostReplacement(string uri)
    {
        foreach (var pair in ReplacementLookup)
        {
            if (uri.Contains(pair.Key))
                return pair.Value;
        }

        return string.Empty;
    }

    public static async Task LoadReplacements()
    {
        var json = await File.ReadAllTextAsync("config/replacements.json");
        var lookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        ReplacementLookup = lookup ?? _fallbackReplacementLookup;
    }

    public static async Task AddReplacement(string host, string replacement)
    {
        ReplacementLookup[host] = replacement;
        var json = JsonConvert.SerializeObject(ReplacementLookup);
        await File.WriteAllTextAsync("config/replacements.json", json);
    }

    public static async Task RemoveReplacement(string host)
    {
        ReplacementLookup.Remove(host);
        var json = JsonConvert.SerializeObject(ReplacementLookup);
        await File.WriteAllTextAsync("config/replacements.json", json);
    }
}
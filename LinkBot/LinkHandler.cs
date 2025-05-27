namespace Howl.LinkBot;

using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;

public static class LinkHandler
{
    private static readonly DiscordEmoji _replaceEmoji = DiscordEmoji.FromUnicode("\ud83d\udce4");
    private static readonly DiscordEmoji _addEmoji = DiscordEmoji.FromUnicode("\ud83d\udce5");

    private static readonly Regex _uriRegex = new(@"(http|ftp|https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])");

    // TODO: Add link handling for
    private static readonly Dictionary<string, string> _replacementLookup = new()
    {
        { "instagram.com", "instagramez.com" },
        { "x.com", "fixupx.com" },
        { "bsky.app", "bskyx.app" },
        { "vt.tiktok.com", "vt.vxtiktok.com" },
    };

    public static async Task HandleMessage(DiscordClient c, MessageCreatedEventArgs e)
    {
        var match = _uriRegex.Match(e.Message.Content);
        if (!match.Success)
            return;

        foreach (var replacement in _replacementLookup.Values)
        {
            if (match.ToString().Contains(replacement))
                return;
        }

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
        await SendFixedMessage(c, e);
    }

    private static async Task ReplaceMessage(DiscordClient c, MessageCreatedEventArgs e)
    {
        await SendFixedMessage(c, e);
        await e.Message.DeleteAsync("Replaced with fixed link");
    }

    private static async Task SendFixedMessage(DiscordClient c, MessageCreatedEventArgs e)
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

        var message = new DiscordMessageBuilder()
            .WithContent(description);

        await Program.Client.SendMessageAsync(e.Channel, message);
    }

    private static string LookupHostReplacement(string uri)
    {
        foreach (var pair in _replacementLookup)
        {
            if (uri.Contains(pair.Key))
                return pair.Value;
        }

        return string.Empty;
    }
}
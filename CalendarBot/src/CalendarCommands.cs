namespace CalendarBot;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Google.Apis.Calendar.v3.Data;

public class CalendarCommands : BaseCommandModule
{
    [Command("list")]
    public async Task ListEvents(CommandContext ctx)
    {
        var startMessage = new DiscordMessageBuilder();
        startMessage.Content = "Fetching calendar events...";
        await ctx.Channel.SendMessageAsync(startMessage);
        var events = await CalendarSync.GetAllEvents();
        var listMessage = new DiscordMessageBuilder();
        listMessage.Content = string.Join("\n", events.Select(x => x.Summary));
        await ctx.Channel.SendMessageAsync(listMessage);
    }

    [Command("sync")]
    public async Task SyncEventsToCalendar(CommandContext ctx)
    {
        var startMessage = new DiscordMessageBuilder();
        startMessage.Content = "Synchronising events";
        await ctx.Channel.SendMessageAsync(startMessage);

        var guildEvents = await ctx.Guild.GetEventsAsync();

        foreach (var guildEvent in guildEvents)
        {
            await CalendarSync.CreateOrUpdateEvent(guildEvent);
        }

        var finishedMessage = new DiscordMessageBuilder();
        finishedMessage.Content = $"Finished synchronising {guildEvents.Count} event(s)";
        await ctx.Channel.SendMessageAsync(finishedMessage);
    }

    [Command("cleanup")]
    public async Task RemoveUnlinkedCalendarEvents(CommandContext ctx)
    {
        var startMessage = new DiscordMessageBuilder();
        startMessage.Content = "Removing unlinked events";
        await ctx.Channel.SendMessageAsync(startMessage);

        var guildEvents = await ctx.Guild.GetEventsAsync();
        var calendarEvents = await CalendarSync.GetAllEvents();

        // Find dangling calendar events
        var eventsToRemove = new List<Event>();
        foreach (var calendarEvent in calendarEvents)
        {
            if (calendarEvent.TryGetEventId(out var id))
            {
                if (guildEvents.Any(x => x.Id.ToString() == id))
                    continue;
            }

            eventsToRemove.Add(calendarEvent);
        }

        // Delete dangling calendar events
        foreach (var calendarEvent in eventsToRemove)
            await CalendarSync.DeleteEvent(calendarEvent);

        var finishMessage = new DiscordMessageBuilder();
        finishMessage.Content = $"Removed {eventsToRemove.Count} unlinked events";
        await ctx.Channel.SendMessageAsync(finishMessage);
    }
}
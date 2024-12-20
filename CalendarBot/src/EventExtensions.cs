namespace CalendarBot;

using DSharpPlus.Entities;
using Google.Apis.Calendar.v3.Data;

public static class EventExtensions
{
    internal static bool TryGetEventId(this Event calendarEvent, out string id)
    {
        id = string.Empty;

        if (calendarEvent.ExtendedProperties?.Shared == null)
            return false;

        return calendarEvent.ExtendedProperties.Shared.TryGetValue("discord_id", out id);
    }

    internal static Event PopulateCalendarEvent(this Event calendarEvent, DiscordScheduledGuildEvent discordEvent)
    {
        var startTime = new EventDateTime();
        startTime.DateTimeDateTimeOffset = discordEvent.StartTime;

        var endTime = new EventDateTime();
        if (discordEvent.EndTime != null)
            endTime.DateTimeDateTimeOffset = discordEvent.EndTime;
        else
            endTime.DateTimeDateTimeOffset = discordEvent.StartTime + TimeSpan.FromHours(1);

        calendarEvent.Summary = $"[EventSync] {discordEvent.Name}";
        calendarEvent.Description = discordEvent.Description;
        calendarEvent.Start = startTime;
        calendarEvent.End = endTime;

        if (discordEvent.Type is ScheduledGuildEventType.External)
            calendarEvent.Location = discordEvent.Metadata?.Location;

        calendarEvent.ExtendedProperties ??= new Event.ExtendedPropertiesData();
        calendarEvent.ExtendedProperties.Shared ??= new Dictionary<string, string>();
        calendarEvent.ExtendedProperties.Shared["discord_id"] = discordEvent.Id.ToString();

        return calendarEvent;
    }

    public static Event CreateCalendarEvent(DiscordScheduledGuildEvent guildEvent)
    {
        return new Event().PopulateCalendarEvent(guildEvent);
    }
}
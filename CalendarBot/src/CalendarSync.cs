namespace CalendarBot;

using DSharpPlus.Entities;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

public static class CalendarSync
{
    private static CalendarService _calendarService { get; set; } = null!;
    private static AppConfig _appConfig { get; set; } = null!;

    public static void Init(CalendarService calendarService, AppConfig config)
    {
        _calendarService = calendarService;
        _appConfig = config;
    }

    public static async Task CreateOrUpdateEvent(DiscordScheduledGuildEvent discordEvent)
    {
        var eventId = discordEvent.Id.ToString();

        var existingEvent = await GetExistingEvent(eventId);
        if (existingEvent != null)
        {
            UpdateCalendarEvent(existingEvent, discordEvent);
            return;
        }

        CreateCalendarEvent(discordEvent);
    }

    public static async Task DeleteEvent(DiscordScheduledGuildEvent discordEvent)
    {
        var eventId = discordEvent.Id.ToString();

        var existingEvent = await GetExistingEvent(eventId);
        if (existingEvent == null)
            return;

        await DeleteEvent(existingEvent);
    }

    public static async Task DeleteEvent(Event calendarEvent)
    {
        var createRequest = _calendarService.Events.Delete(_appConfig.CalendarId, calendarEvent!.Id);
        await createRequest.ExecuteAsync();
    }

    private static async Task<Event?> GetExistingEvent(string eventId)
    {
        var request = _calendarService.Events.List(_appConfig.CalendarId);
        var events = await request.ExecuteAsync();
        var existingEvent = events.Items.FirstOrDefault(x => x.TryGetEventId(out var id) && id == eventId);

        return existingEvent;
    }

    private static async Task CreateCalendarEvent(DiscordScheduledGuildEvent guildEvent)
    {
        var calendarEvent = EventExtensions.CreateCalendarEvent(guildEvent);
        await InsertCalendarEvent(calendarEvent);
    }

    private static async void UpdateCalendarEvent(Event existingEvent, DiscordScheduledGuildEvent discordEvent)
    {
        existingEvent.PopulateCalendarEvent(discordEvent);
        await UpdateCalendarEvent(existingEvent);
    }

    private static async Task InsertCalendarEvent(Event calendarEvent)
    {
        var createRequest = _calendarService.Events.Insert(calendarEvent, _appConfig.CalendarId);
        await createRequest.ExecuteAsync();
    }

    private static async Task UpdateCalendarEvent(Event calendarEvent)
    {
        var createRequest = _calendarService.Events.Update(calendarEvent, _appConfig.CalendarId, calendarEvent.Id);
        await createRequest.ExecuteAsync();
    }

    public static async Task<IList<Event>> GetAllEvents()
    {
        var request = _calendarService.Events.List(_appConfig.CalendarId);
        var events = await request.ExecuteAsync();
        return events.Items;
    }
}
namespace SEBlueprint.App.Services;

public static class RatePrompt
{
    const double ChancePerSession = 0.2;
    const double MinDelaySeconds = 40;
    const double MaxDelaySeconds = 360;

    public static bool ShouldShow(AppSettings s) => !s.RatePromptDone && Random.Shared.NextDouble() < ChancePerSession;

    public static TimeSpan RandomDelay() =>
        TimeSpan.FromSeconds(MinDelaySeconds + Random.Shared.NextDouble() * (MaxDelaySeconds - MinDelaySeconds));

    public static void Finish(AppSettings s)
    {
        s.RatePromptDone = true;
        s.Save();
    }
}

namespace VikingMobile.Application.Communication;

public class AttendanceModeManager
{
    private const string PreferenceKey = "viking_attendance_mode";

    public AttendanceMode CurrentMode { get; private set; }

    public AttendanceModeManager()
    {
        string valor = Preferences.Get(
            PreferenceKey,
            AttendanceMode.Automatic.ToString());

        if (!Enum.TryParse(
                valor,
                out AttendanceMode modo))
        {
            modo = AttendanceMode.Automatic;
        }

        CurrentMode = modo;
    }

    public void SetMode(AttendanceMode mode)
    {
        CurrentMode = mode;

        Preferences.Set(
            PreferenceKey,
            mode.ToString());
    }

    public string GetDisplayText()
    {
        return CurrentMode switch
        {
            AttendanceMode.Online =>
                "🟢 ONLINE",

            AttendanceMode.Offline =>
                "🟠 OFFLINE",

            AttendanceMode.Automatic =>
                "🔵 AUTOMÁTICO",

            _ =>
                "🔵 AUTOMÁTICO"
        };
    }
}
namespace easySkillsCrosshair.Core.Sensitivity;

/// <summary>
/// 360°-distance conversion. A game's <i>yaw</i> is the turn in degrees produced by one mouse
/// count at in-game sensitivity 1 (e.g. Source engine m_yaw = 0.022). Keeping the physical mouse
/// travel for a full turn identical preserves muscle memory across games:
/// <code>sens_to = sens_from · yaw_from · dpi_from / (yaw_to · dpi_to)</code>
/// </summary>
public static class SensitivityMath
{
    private const double CentimetresPerInch = 2.54;

    public static double CountsPer360(double sensitivity, double yaw) =>
        IsValid(sensitivity, yaw) ? 360.0 / (sensitivity * yaw) : 0;

    public static double InchesPer360(double sensitivity, double yaw, double dpi) =>
        dpi > 0 && IsValid(sensitivity, yaw) ? CountsPer360(sensitivity, yaw) / dpi : 0;

    public static double CentimetresPer360(double sensitivity, double yaw, double dpi) =>
        InchesPer360(sensitivity, yaw, dpi) * CentimetresPerInch;

    public static double EffectiveDpi(double sensitivity, double dpi) => sensitivity * dpi;

    public static double Convert(double sensitivityFrom, double yawFrom, double dpiFrom, double yawTo, double dpiTo)
    {
        if (!IsValid(sensitivityFrom, yawFrom) || yawTo <= 0 || dpiFrom <= 0 || dpiTo <= 0)
        {
            return 0;
        }

        return sensitivityFrom * yawFrom * dpiFrom / (yawTo * dpiTo);
    }

    private static bool IsValid(double sensitivity, double yaw) =>
        sensitivity > 0 && yaw > 0 && double.IsFinite(sensitivity) && double.IsFinite(yaw);
}

namespace easySkillsCrosshair.Core.Sensitivity;

/// <param name="Id">Stable key, persisted in the user's last selection.</param>
/// <param name="Yaw">Degrees turned per mouse count at in-game sensitivity 1.</param>
/// <param name="Decimals">Precision the game's settings accept; the result is rounded to this.</param>
/// <param name="Note">Where the value comes from — shown in the details so users can judge it.</param>
public sealed record GameSensitivityProfile(string Id, string Name, double Yaw, int Decimals, string Note)
{
    public const string CustomId = "custom";

    public bool IsCustom => Id == CustomId;
}

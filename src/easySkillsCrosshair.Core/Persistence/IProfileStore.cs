namespace easySkillsCrosshair.Core.Persistence;

public interface IProfileStore
{
    IReadOnlyList<GameProfile> LoadAll();
    void SaveAll(IReadOnlyList<GameProfile> profiles);
}

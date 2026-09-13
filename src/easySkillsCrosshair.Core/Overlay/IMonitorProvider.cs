namespace easySkillsCrosshair.Core.Overlay;

public interface IMonitorProvider
{
    MonitorDescriptor GetPrimary();
    IReadOnlyList<MonitorDescriptor> GetAll();
}

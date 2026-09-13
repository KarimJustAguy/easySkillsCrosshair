using System.Windows.Media;
using easySkillsCrosshair.App.Icons;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class NavigationItemViewModel : ViewModelBase
{
    public NavigationItemViewModel(string title, IconKind icon, object content)
    {
        Title = title;
        Icon = NavigationIcons.Get(icon);
        Content = content;
    }

    public string Title { get; }
    public Geometry Icon { get; }
    public object Content { get; }
}

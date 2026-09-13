using System.Collections.ObjectModel;
using System.Windows.Input;
using easySkillsCrosshair.Core.Community;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class CommunityViewModel : ViewModelBase
{
    private readonly ICommunityService _communityService;
    private readonly CrosshairEditorViewModel _editor;
    private string _searchText = "";
    private bool _isLoading;

    public CommunityViewModel(ICommunityService communityService, CrosshairEditorViewModel editor)
    {
        _communityService = communityService;
        _editor = editor;

        Listings = [];
        SearchCommand = new RelayCommand(async _ => await SearchAsync());
        UseCommand = new RelayCommand(p =>
        {
            if (p is CommunityCrosshairListing listing)
            {
                _editor.ApplyProfile(listing.Profile);
            }
        });

        _ = SearchAsync();
    }

    public ObservableCollection<CommunityCrosshairListing> Listings { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand UseCommand { get; }

    private async Task SearchAsync()
    {
        IsLoading = true;
        try
        {
            var results = string.IsNullOrWhiteSpace(SearchText)
                ? await _communityService.GetFeaturedAsync()
                : await _communityService.SearchAsync(SearchText);

            Listings.Clear();
            foreach (var listing in results)
            {
                Listings.Add(listing);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}

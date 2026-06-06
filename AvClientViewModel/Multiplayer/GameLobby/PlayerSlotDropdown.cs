using System;
using System.Collections.ObjectModel;

using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public partial class PlayerSlotDropdown<T> : ObservableObject, IPlayerSlotDropdown<T>
{
    private readonly ObservableCollection<T> _options = new();
    public ObservableCollection<T> Options
    {
        get => _options;
        set
        {
            // Modify existing collection in-place instead of replacing the reference.
            // Replacing the reference fires PropertyChanged for "Options", which causes
            // Avalonia to call SelectionModel.SetSource. If this setter is called from
            // inside a PropertyChanged handler triggered by an Avalonia binding update
            // (e.g. SelectedOption change), SetSource throws:
            // "Cannot change source while update is in progress."
            // In-place modification only fires CollectionChanged, which Avalonia
            // handles without touching SelectionModel.SetSource.
            if (ReferenceEquals(_options, value))
                return;
            _options.Clear();
            foreach (var item in value)
                _options.Add(item);
        }
    }

    private readonly ObservableCollection<bool> _selectable = new();
    public ObservableCollection<bool> Selectable
    {
        get => _selectable;
        set
        {
            // Same in-place modification pattern as Options (see comment above).
            if (ReferenceEquals(_selectable, value))
                return;
            _selectable.Clear();
            foreach (var item in value)
                _selectable.Add(item);
        }
    }

    [ObservableProperty]
    public partial T? SelectedOption { get; set; } = default;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
}

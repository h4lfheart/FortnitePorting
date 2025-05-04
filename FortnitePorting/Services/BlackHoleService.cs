using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Services;
using FortnitePorting.Views;

namespace FortnitePorting.Services;

public partial class BlackHoleService : ObservableObject, IService
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsActive))] private TimeWasterView? _content;
    public bool IsActive => Content is not null;
    
    private readonly List<Key> _konamiKeyPresses = [];
    private readonly List<Key> _konamiSequence = [Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A];

    public void HandleKey(Key key)
    {
        if (IsActive)
        {
            if (key == Key.Escape)
            {
                Close();
                _konamiKeyPresses.Clear();
                return;
            }
            
            return;
        }
        
        if (!_konamiSequence.Contains(key)) return; // im not keylogging you smh
        
        _konamiKeyPresses.Add(key);

        if (_konamiKeyPresses[^Math.Min(_konamiKeyPresses.Count, _konamiSequence.Count)..].SequenceEqual(_konamiSequence))
        {
            Open(isMinigame: true);
            _konamiKeyPresses.Clear();
        }

    }
    
    public void Open(bool isMinigame)
    { 
        TaskService.RunDispatcher(() =>
        {
            Content = new TimeWasterView(isMinigame);
        });
    }
    
    public void Close()
    {
        Content = null; // TODO check that this disposes properly
    }
}
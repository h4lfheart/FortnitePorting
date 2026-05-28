using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls.WrapPanel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class FilesView : ViewBase<FilesViewModel>
{
    public FilesView() : base(FilesVM, initializeViewModel: false)
    {
        InitializeComponent();
    }
    
    private void OnFileItemTapped(TreeItem item)
    {
        if (item.Type != ENodeType.File) return;
        
        TaskService.RunDispatcher(async () => await ViewModel.Preview());
    }
}
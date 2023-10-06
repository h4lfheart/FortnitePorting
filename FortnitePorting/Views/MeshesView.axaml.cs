using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MeshesView : ViewBase<MeshesViewModel>
{
    public MeshesView() : base(waitInit: true)
    {
        InitializeComponent();
    }
}
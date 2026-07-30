using FortnitePorting.Application;
using FortnitePorting.Models.Nodes.Material;
using FortnitePorting.Services;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class MaterialPreviewWindowModel(SettingsService settings) : NodeGraphPreviewWindowModelBase<MaterialNodeTree>(settings);

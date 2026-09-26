using FortnitePorting.Application;
using FortnitePorting.Models.Nodes.SoundCue;
using FortnitePorting.Services;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class SoundCuePreviewWindowModel(SettingsService settings) : NodeGraphPreviewWindowModelBase<SoundCueNodeTree>(settings);

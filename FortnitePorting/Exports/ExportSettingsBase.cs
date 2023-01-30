using FortnitePorting.AppUtils;

namespace FortnitePorting.Exports;

public class ExportSettingsBase
{
    // GENERAL
    public bool ScaleDown = true;

    // ANIM
    public bool LobbyPoses;
    public bool ImportSounds;

    // MESH
    public int LevelOfDetail = 0;

    // MATERIAL
    public bool ImportMaterials = true;
    public EImageType ImageType => AppSettings.Current.ImageType;
    public double AmbientOcclusion;
    public double Cavity;
    public double Subsurface;
}
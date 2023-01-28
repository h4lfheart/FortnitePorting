using FortnitePorting.AppUtils;

namespace FortnitePorting.Exports.Blender;

public class BlenderExportSettings
{
    // GENERAL
    public bool IntoCollection = true;
    public bool ScaleDown = true;

    // RIGGING
    public ERigType RigType;
    public bool MergeSkeletons = true;
    public bool ReorientBones;
    public bool HideFaceBones;
    public float BoneLengthRatio = 0.4f;

    // ANIM
    public bool UpdateTimeline;
    public bool LobbyPoses;
    public bool LoopAnim;
    public bool ImportSounds;

    // MESH
    public bool QuadTopo;
    public bool PoseFixes;
    public int LevelOfDetail = 0;

    // MATERIAL
    public bool ImportMaterials = true;
    public EImageType ImageType => AppSettings.Current.ImageType;
    public double AmbientOcclusion;
    public double Cavity;
    public double Subsurface;
}
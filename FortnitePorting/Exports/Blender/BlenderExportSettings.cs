namespace FortnitePorting.Exports.Blender;

public class BlenderExportSettings
{
    // GENERAL
    public bool IntoCollection = true;
    public bool ScaleDown = true;

    // RIGGING
    public ERigType RigType;
    public bool MergeSkeletons;
    public bool ReorientBones;

    // ANIM
    public bool UpdateTimeline;
    public bool LobbyPoses;

    // MESH
    public bool QuadTopo;
    public bool PoseFixes;
    public int LevelOfDetail = 0;

    // MATERIAL
    public bool ImportMaterials = true;
    public double AmbientOcclusion;
    public double Cavity;
    public double Subsurface;
}
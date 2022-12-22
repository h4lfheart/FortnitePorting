namespace FortnitePorting.Exports.Blender;

public class BlenderExportSettings
{
    // RIGGING
    public ERigType RigType;
    public bool MergeSkeletons;
    public bool ReorientBones;
    
    // MESH
    public bool QuadTopo;
    public bool PoseFixes;
    
    // MATERIAL
    public bool ImportMaterials = true;
    public double AmbientOcclusion;
    public double Cavity;
    public double Subsurface;
}
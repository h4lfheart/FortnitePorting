using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using FortnitePorting.Rendering.Animation.Pose;
using FortnitePorting.Rendering.Components.Rendering;
using FortnitePorting.Rendering.Data.Buffers;
using FortnitePorting.Rendering.Data.Programs;
using FortnitePorting.Rendering.Exceptions;
using FortnitePorting.Rendering.Materials;

namespace FortnitePorting.Rendering.Renderers;

public class SkeletalMeshRenderer : MeshRenderer
{
    private const int InfluenceSlots = 8;

    public List<Section> Sections = [];
    public Material[] Materials = [];
    public SkeletalPoseEvaluator Pose;
    public event Action<float>? AfterAnimationUpdate;

    private SSBO<Matrix4> _boneBuffer = new();
    private Matrix4[] _uploadBones = [];

    public SkeletalMeshRenderer(USkeletalMesh skeletalMesh, UAnimationAsset? animation = null, int lodLevel = 0)
        : base(new ShaderProgram("skinned", "shader"))
    {
        using var convertedMesh = new SkeletalMeshDto(skeletalMesh);
        if (convertedMesh.LODs.Count == 0)
        {
            throw new RenderingXException("Failed to convert skeletal mesh.");
        }

        BoundingBox = convertedMesh.Bounds;

        var refPose = skeletalMesh.ReferenceSkeleton.FinalRefBonePose;
        Pose = new SkeletalPoseEvaluator(convertedMesh.Bones, refPose);
        _uploadBones = new Matrix4[Pose.BoneCount];
        Array.Fill(_uploadBones, Matrix4.Identity);

        var lod = convertedMesh.LODs[Math.Min(lodLevel, convertedMesh.LODs.Count - 1)];

        Indices = lod.Indices;

        var vertices = lod.Vertices;
        var extraUVs = lod.ExtraUvs;
        
        var buildVertices = new List<float>(vertices.Length * 28);

        for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            var vertex = vertices[vertexIndex];
            var position = vertex.Position * 0.01f;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var uv = vertex.Uv;
            var materialLayer = extraUVs.Length > 0 ? extraUVs[0][vertexIndex].U : 0;

            buildVertices.AddRange([
                position.X, position.Z, position.Y,
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y,
                uv.U, uv.V,
                materialLayer
            ]);

            PackInfluences(vertex, buildVertices);
        }

        Vertices = buildVertices.ToArray();

        var sections = lod.Sections;
        Materials = new Material[sections.Length];

        for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            var section = sections[sectionIndex];
            Sections.Add(new Section(section.MaterialIndex, section.NumFaces * 3, section.FirstIndex));

            if (skeletalMesh.Materials[section.MaterialIndex]?.TryLoad(out var sectionMaterial) ?? false)
            {
                Materials[sectionIndex] = sectionMaterial switch
                {
                    UMaterialInstanceConstant materialInstance => new Material(materialInstance),
                    UMaterial material => new Material(material),
                    _ => new Material()
                };
            }
            else
            {
                Materials[sectionIndex] = new Material();
            }
        }

        if (animation is not null)
        {
            Play(animation, skeletalMesh);
        }
    }

    public void Play(UAnimationAsset animation, USkeletalMesh mesh, bool loop = true, float speed = 1f)
    { 
        var animationSkeleton = animation.Skeleton.Load<USkeleton>()
                                ?? mesh.Skeleton.Load<USkeleton>()
                                ?? throw new RenderingXException("Could not resolve a skeleton for animation playback.");

        Pose.Play(animation, animationSkeleton, loop, speed);
        UploadBoneMatrices(Pose.SkinMatrices);
    }

    public void Play(CAnimSequence sequence, bool loop = true, float speed = 1f)
    {
        Pose.Play(sequence, loop, speed);
        UploadBoneMatrices(Pose.SkinMatrices);
    }

    public void Stop()
    {
        Pose.Stop();
        UploadBoneMatrices(Pose.SkinMatrices);
    }

    public void Pause() => Pose.Pause();

    public void Resume() => Pose.Resume();

    public void Seek(float timeSeconds)
    {
        Pose.Seek(timeSeconds);
        UploadBoneMatrices(Pose.SkinMatrices);
    }

    public void JumpToSection(int index)
    {
        Pose.JumpToSection(index);
        UploadBoneMatrices(Pose.SkinMatrices);
    }

    public void JumpToSection(string name)
    {
        Pose.JumpToSection(name);
        UploadBoneMatrices(Pose.SkinMatrices);
    }

    public override void Initialize()
    {
        base.Initialize();

        _boneBuffer.Generate();
        UploadBoneMatrices(_uploadBones);

        foreach (var material in Materials)
        {
            Shader.Use();
            material.SetUniforms(Shader);
        }
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (Pose.Sequence is null)
        {
            AfterAnimationUpdate?.Invoke(deltaTime);
            return;
        }

        if (Pose.IsPlaying)
            Pose.Update(deltaTime);

        // Always upload after sampling so scrub / section jumps (while paused) stay visible.
        UploadBoneMatrices(Pose.SkinMatrices);

        AfterAnimationUpdate?.Invoke(deltaTime);
    }

    private void UploadBoneMatrices(Matrix4[] skinMatrices)
    {
        for (var i = 0; i < skinMatrices.Length; i++)
            _uploadBones[i] = Matrix4.Transpose(skinMatrices[i]);

        _boneBuffer.Fill(_uploadBones);
    }

    protected override void BuildMesh()
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("MaterialLayer", 1, VertexAttribPointerType.Float);
        RegisterAttribute("BoneIndices0", 4, VertexAttribPointerType.Float);
        RegisterAttribute("BoneIndices1", 4, VertexAttribPointerType.Float);
        RegisterAttribute("BoneWeights0", 4, VertexAttribPointerType.Float);
        RegisterAttribute("BoneWeights1", 4, VertexAttribPointerType.Float);

        base.BuildMesh();
    }

    protected override void RenderShader(CameraComponent camera)
    {
        base.RenderShader(camera);

        _boneBuffer.BindBufferBase();

        foreach (var section in Sections)
        {
            Materials[section.MaterialIndex].Bind();
        }
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();

        foreach (var section in Sections)
        {
            GL.DrawElements(PrimitiveType.Triangles, section.FaceCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr);
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        _boneBuffer.Delete();
    }

    private static void PackInfluences(SkinnedMeshVertex vertex, List<float> buildVertices)
    {
        Span<float> indices = stackalloc float[InfluenceSlots];
        Span<float> weights = stackalloc float[InfluenceSlots];

        var influences = vertex.Influences;
        var sources = influences.Length <= InfluenceSlots
            ? influences
            : influences.OrderByDescending(i => i.Weight).Take(InfluenceSlots).ToArray();

        var weightSum = 0f;
        for (var i = 0; i < sources.Length; i++)
        {
            indices[i] = sources[i].Bone;
            weights[i] = sources[i].Weight;
            weightSum += sources[i].Weight;
        }

        if (weightSum > 0f)
        {
            for (var i = 0; i < sources.Length; i++)
                weights[i] /= weightSum;
        }
        else
        {
            indices[0] = 0;
            weights[0] = 1f;
        }

        buildVertices.AddRange([
            indices[0], indices[1], indices[2], indices[3],
            indices[4], indices[5], indices[6], indices[7],
            weights[0], weights[1], weights[2], weights[3],
            weights[4], weights[5], weights[6], weights[7]
        ]);
    }
}

using System.IO;
using UnrealBuildTool;

public class UEFormat : ModuleRules
{
	public UEFormat(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicIncludePaths.AddRange(
			new string[]
			{
				Path.Combine(ModuleDirectory, "ThirdParty/zstd"),
				Path.Combine(ModuleDirectory, "ThirdParty/zstd/common"),
				Path.Combine(ModuleDirectory, "ThirdParty/zstd/compress"),
				Path.Combine(ModuleDirectory, "ThirdParty/zstd/decompress"),
				Path.Combine(ModuleDirectory, "ThirdParty/zstd/deprecated"),
				Path.Combine(ModuleDirectory, "ThirdParty/zstd/dictBuilder"),
				Path.Combine(ModuleDirectory, "ThirdParty/zstd/legacy")
			}
		);
        
		PublicDependencyModuleNames.AddRange(
			new string[]
			{
				"Core",
				"CoreUObject",
				"Engine",
				"MeshDescription",
				"StaticMeshDescription",
				"SkeletalMeshDescription",
				"AssetTools",
				"UnrealEd",
				"SkeletalMeshUtilitiesCommon"
			}
		);

		PrivateDependencyModuleNames.AddRange(
			new string[]
			{
				"Slate",
				"SlateCore",
				"EditorWidgets",
				"MainFrame",
				"ToolWidgets"
			}
		);
	}
}
#pragma once
#include "Components/SkeletalMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "EditorAssetLibrary.h"
#include "Engine/SCS_Node.h"
#include "Kismet/BlueprintFunctionLibrary.h"

class USceneComponent;
class USCS_Node;

class FBlueprintUtils
{
public:
	
	static USCS_Node* AddNodeToBlueprint(FString BlueprintPath, USceneComponent* ComponentTemplate, FString ParentNodeName);
	static USCS_Node* AddSceneComponentToBlueprint(FString BlueprintPath, FString ComponentName, FString ParentNodeName);
	static USCS_Node* AddSkeletalMeshComponentToBlueprint(FString BlueprintPath, FString ComponentName, FString ParentNodeName, FString SkeletalMeshAssetPath);
	static USCS_Node* AddStaticMeshComponentToBlueprint(FString BlueprintPath, FString ComponentName, FString ParentNodeName, FString StaticMeshAssetPath);

};

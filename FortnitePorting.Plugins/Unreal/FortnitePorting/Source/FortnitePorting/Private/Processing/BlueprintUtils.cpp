#include "Processing/BlueprintUtils.h"
#include "Kismet2/BlueprintEditorUtils.h"


USCS_Node* FBlueprintUtils::AddNodeToBlueprint(FString BlueprintPath, USceneComponent* ComponentTemplate, FString ParentNodeName)
{
	UBlueprint* Blueprint = Cast<UBlueprint>(StaticLoadObject(UBlueprint::StaticClass(), nullptr, *BlueprintPath));
	if(Blueprint==nullptr)
	{
		UE_LOG(LogTemp, Error, TEXT("Unable to load blueprint."));
		return nullptr;
	}

	USCS_Node* NewNode = Blueprint->SimpleConstructionScript->CreateNode(ComponentTemplate->GetClass(), *ComponentTemplate->GetName());
	USCS_Node* ParentNode = Blueprint->SimpleConstructionScript->FindSCSNode(*ParentNodeName);

	if(ParentNode!=nullptr)
	{
		ParentNode->AddChildNode(NewNode);
	}
	else
	{
		TArray<USCS_Node*> AllNodes = Blueprint->SimpleConstructionScript->GetAllNodes();

		if (AllNodes.Num() == 0 || AllNodes[0] == Blueprint->SimpleConstructionScript->GetDefaultSceneRootNode())
		{
			Blueprint->SimpleConstructionScript->AddNode(NewNode);
		}
		else
		{
			AllNodes[0]->AddChildNode(NewNode);
		}
	}

	UEditorEngine::CopyPropertiesForUnrelatedObjects(ComponentTemplate, NewNode->ComponentTemplate);

	FBlueprintEditorUtils::MarkBlueprintAsStructurallyModified(Blueprint);

	return NewNode;
}

USCS_Node* FBlueprintUtils::AddSceneComponentToBlueprint(FString BlueprintPath, FString ComponentName, FString ParentNodeName)
{
	USceneComponent* Component = NewObject<USceneComponent>(GetTransientPackage(), *ComponentName, RF_Transient);
	USCS_Node* NewNode = AddNodeToBlueprint(BlueprintPath, Component, ParentNodeName);

	return NewNode;
}

USCS_Node* FBlueprintUtils::AddSkeletalMeshComponentToBlueprint(FString BlueprintPath, FString ComponentName, FString ParentNodeName, FString SkeletalMeshAssetPath)
{
	USkeletalMeshComponent* Component = NewObject<USkeletalMeshComponent>(GetTransientPackage(), *ComponentName, RF_Transient);
	USkeletalMesh * SkeletalMesh = Cast<USkeletalMesh>(UEditorAssetLibrary::LoadAsset(SkeletalMeshAssetPath));
	if(SkeletalMesh == nullptr){
		UE_LOG(LogTemp, Error, TEXT("Invalid SkeletalMesh at path: %s"),*SkeletalMeshAssetPath);
	}
	Component->SetSkeletalMeshAsset(SkeletalMesh);

	USCS_Node* NewNode = AddNodeToBlueprint(BlueprintPath, Component, ParentNodeName);

	return NewNode;
}

USCS_Node* FBlueprintUtils::AddStaticMeshComponentToBlueprint(FString BlueprintPath, FString ComponentName, FString ParentNodeName, FString StaticMeshAssetPath)
{
	UStaticMeshComponent* Component = NewObject<UStaticMeshComponent>(GetTransientPackage(), *ComponentName, RF_Transient);
	UStaticMesh * StaticMesh = Cast<UStaticMesh>(UEditorAssetLibrary::LoadAsset(StaticMeshAssetPath));
	if(StaticMesh == nullptr){
		UE_LOG(LogTemp, Error, TEXT("Invalid StaticMesh at path: %s"),*StaticMeshAssetPath);
	}
	Component->SetStaticMesh(StaticMesh);
	
	USCS_Node* NewNode = AddNodeToBlueprint(BlueprintPath, Component, ParentNodeName);

	return NewNode;
}

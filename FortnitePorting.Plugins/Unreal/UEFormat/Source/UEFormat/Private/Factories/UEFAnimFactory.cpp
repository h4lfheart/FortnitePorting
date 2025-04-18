// Copyright © 2025 Marcel K. All rights reserved.

#include "Factories/UEFAnimFactory.h"
#include "ComponentReregisterContext.h"
#include "Animation/AnimSequence.h"
#include "Widgets/Anim/UEFAnimImportOptions.h"
#include "Widgets/Anim/UEFAnimWidget.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "Framework/Application/SlateApplication.h"
#include "Interfaces/IMainFrameModule.h"
#include "Misc/FeedbackContext.h"
#include "Misc/ScopedSlowTask.h"
#include "Readers/UEFAnimReader.h"
#include "Widgets/SWindow.h"

UEFAnimFactory::UEFAnimFactory(const FObjectInitializer & ObjectInitializer): Super(ObjectInitializer)
{
	Formats.Add(TEXT("ueanim; UEANIM Animation File"));
	SupportedClass = UAnimSequence::StaticClass();
	bCreateNew = false;
	bEditorImport = true;
	SettingsImporter = CreateDefaultSubobject<UEFAnimImportOptions>(TEXT("Anim Options"));
}

UObject* UEFAnimFactory::FactoryCreateFile(UClass* Class, UObject* Parent, FName Name, EObjectFlags Flags, const FString& Filename, const TCHAR* Params, FFeedbackContext* Warn, bool& bOutOperationCanceled)
{
	FScopedSlowTask SlowTask(5, NSLOCTEXT("UEFAnimFactory", "BeginReadUEAnimFile", "Reading UEAnim file"), true);
	if (Warn->GetScopeStack().Num() == 0)
		SlowTask.MakeDialog(true);

	SlowTask.EnterProgressFrame(0);

	UEFAnimReader Data = UEFAnimReader(Filename);
	if (!Data.Read())
		return nullptr;

	//Ui
	if (SettingsImporter->bInitialized == false)
	{
		TSharedPtr<UEFAnimWidget> ImportOptionsWindow;
		TSharedPtr<SWindow> ParentWindow;
		if (FModuleManager::Get().IsModuleLoaded("MainFrame"))
		{
			IMainFrameModule& MainFrame = FModuleManager::LoadModuleChecked<IMainFrameModule>("MainFrame");
			ParentWindow = MainFrame.GetParentWindow();
		}

		TSharedRef<SWindow> Window = SNew(SWindow).Title(FText::FromString(TEXT("Animation Import Options"))).SizingRule(ESizingRule::Autosized);
		Window->SetContent(SAssignNew(ImportOptionsWindow, UEFAnimWidget).WidgetWindow(Window));
		SettingsImporter = ImportOptionsWindow.Get()->Stun;
		FSlateApplication::Get().AddModalWindow(Window, ParentWindow, false);
		bImport = ImportOptionsWindow.Get()->ShouldImport();
		bImportAll = ImportOptionsWindow.Get()->ShouldImportAll();
		SettingsImporter->bInitialized = true;
	}

	UAnimSequence* AnimSequence = NewObject<UAnimSequence>(Parent, Name, Flags);
	IAnimationDataController& Controller = AnimSequence->GetController();
	USkeleton* Skeleton = SettingsImporter->Skeleton;

	AnimSequence->SetSkeleton(Skeleton);
	Controller.OpenBracket(FText::FromString("Importing UEAnim Animation"));
	Controller.InitializeModel();
	AnimSequence->ResetAnimation();

	Controller.SetFrameRate(FFrameRate(Data.FramesPerSecond, 1));
	Controller.SetNumberOfFrames(FFrameNumber(Data.NumFrames));

	FScopedSlowTask ImportTask(Data.Tracks.Num(), FText::FromString("Importing UEAnim Animation"));
	ImportTask.MakeDialog(false);

	//Import Tracks
	for (const auto& Track : Data.Tracks)
	{
		ImportTask.EnterProgressFrame();

		FName BoneName = Track.TrackName.c_str();
		auto PosKeys = Track.TrackPosKeys;
		auto RotKeys = Track.TrackRotKeys;
		auto ScaleKeys = Track.TrackScaleKeys;

		TArray<FVector3f> FinalPosKeys;
		TArray<FQuat4f> FinalRotKeys;
		TArray<FVector3f> FinalScaleKeys;
		FinalPosKeys.SetNum(Data.NumFrames);
		FinalRotKeys.SetNum(Data.NumFrames);
		FinalScaleKeys.SetNum(Data.NumFrames);

		FVector3f PrevPos = FVector3f::ZeroVector;
		FQuat4f PrevRot = FQuat4f::Identity;
		FVector3f PrevScale = FVector3f::OneVector;

		int PosIndex = 0, RotIndex = 0, ScaleIndex = 0;
		for (auto j = 0; j < Data.NumFrames; j++)
		{
			//position keys
			if (PosIndex < PosKeys.Num() && PosKeys[PosIndex].Frame == j)
			{
				FinalPosKeys[j] = PosKeys[PosIndex].VectorValue;
				PrevPos = PosKeys[PosIndex].VectorValue;
				PosIndex++;
			}
			else
				FinalPosKeys[j] = PrevPos;
			
			//rotation keys
			if (RotIndex < RotKeys.Num() && RotKeys[RotIndex].Frame == j)
			{
				FinalRotKeys[j] = RotKeys[RotIndex].QuatValue;
				PrevRot = RotKeys[RotIndex].QuatValue;
				RotIndex++;
			}
			else
				FinalRotKeys[j] = PrevRot;

			//scale keys
			if (ScaleIndex < ScaleKeys.Num() && ScaleKeys[ScaleIndex].Frame == j)
			{
				FinalScaleKeys[j] = ScaleKeys[ScaleIndex].VectorValue;
				PrevScale = ScaleKeys[ScaleIndex].VectorValue;
				ScaleIndex++;
			}
			else
				FinalScaleKeys[j] = PrevScale;
		}

		Controller.AddBoneCurve(BoneName);
		Controller.SetBoneTrackKeys(BoneName, FinalPosKeys, FinalRotKeys, FinalScaleKeys);
	}

	//Import Curves
	for (auto i = 0; i < Data.Curves.Num(); i++)
	{
		ImportTask.EnterProgressFrame();

		TArray<FRichCurveKey> RichCurves;
		for (const auto& Key : Data.Curves[i].CurveKeys)
		{
			FRichCurveKey RichKey;
			RichKey.Time = Key.Frame / Data.FramesPerSecond; //Time is in seconds
			RichKey.Value = Key.FloatValue;
			RichCurves.Add(RichKey);
		}

		FAnimationCurveIdentifier CurveIdentifier(Data.Curves[i].CurveName.c_str(), ERawCurveTrackTypes::RCT_Float);
		Controller.AddCurve(CurveIdentifier);
		Controller.SetCurveKeys(CurveIdentifier, RichCurves, false);
	}
	
	if (!bImportAll)
		SettingsImporter->bInitialized = false;

	AnimSequence->GetController().NotifyPopulated();
	AnimSequence->GetController().CloseBracket();
	AnimSequence->PostEditChange();

	FAssetRegistryModule::AssetCreated(AnimSequence);
	FGlobalComponentReregisterContext RecreateComponents;

	return AnimSequence;
}

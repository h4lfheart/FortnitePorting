// Fill out your copyright notice in the Description page of Project Settings.

#include "Widgets/Anim/UEFAnimWidget.h"
#include "SPrimaryButton.h"
#include "SlateOptMacros.h"
#include "Widgets/Anim/UEFAnimImportOptions.h"

BEGIN_SLATE_FUNCTION_BUILD_OPTIMIZATION

void UEFAnimWidget::Construct(const FArguments& InArgs)
{
	WidgetWindow = InArgs._WidgetWindow;
	FPropertyEditorModule& EditModule = FModuleManager::Get().GetModuleChecked<FPropertyEditorModule>("PropertyEditor");
	FDetailsViewArgs DetailsViewArgs;
	DetailsViewArgs.bAllowSearch = false;
	DetailsViewArgs.NameAreaSettings = FDetailsViewArgs::HideNameArea;
	DetailsViewArgs.bHideSelectionTip = true;
	TSharedRef<IDetailsView> Details = EditModule.CreateDetailView(DetailsViewArgs);
	EditModule.CreatePropertyTable();
	UObject* Container = NewObject<UEFAnimImportOptions>();
	Stun = Cast<UEFAnimImportOptions>(Container);
	Details->SetObject(Container);
	Details->SetEnabled(true);

	this->ChildSlot
		[
			SNew(SBorder)
			.BorderImage(FAppStyle::Get().GetBrush(TEXT("Brushes.Panel")))
		.Padding(10)
		[
			SNew(SVerticalBox)
			+ SVerticalBox::Slot()
		.AutoHeight()
		[
			SNew(SBox)
			.Padding(FMargin(3))
		[
			SNew(SHorizontalBox)
			+ SHorizontalBox::Slot()
		]
		]

	// Data row struct
	// Curve interpolation
	// Details panel
	+ SVerticalBox::Slot()
		.AutoHeight()
		.Padding(2)
		[
			SNew(SBox)
			.WidthOverride(400)
		[
			Details
		]
		]
	+SVerticalBox::Slot()
		.AutoHeight()
		[
			SNew(SHorizontalBox)
			+ SHorizontalBox::Slot()
		    .HAlign(HAlign_Right)
		    .Padding(2)
		[
			SNew(SPrimaryButton)
			.Text(FText::FromString(TEXT("Apply")))
		    .OnClicked(this, &UEFAnimWidget::OnImport)
		]
	+ SHorizontalBox::Slot()
		.AutoWidth()
		.Padding(2)
		[
			SNew(SButton)
			.Text(FText::FromString(TEXT("Apply to All")))
		.OnClicked(this, &UEFAnimWidget::OnImportAll)
		]
	+ SHorizontalBox::Slot()
		.AutoWidth()
		.Padding(2)
		[
			SNew(SButton)
			.Text(FText::FromString(TEXT("Cancel")))
		.OnClicked(this, &UEFAnimWidget::OnCancel)
		]
		]
		]
	// Apply/Apply to All/Cancel
		];


}
bool UEFAnimWidget::ShouldImport()
{
	return UserDlgResponse == UEFAnimImportOptionDlgResponse::Import;
}
bool UEFAnimWidget::ShouldImportAll()
{
	return UserDlgResponse == UEFAnimImportOptionDlgResponse::ImportAll;
}
FReply UEFAnimWidget::OnImportAll()
{
	UserDlgResponse = UEFAnimImportOptionDlgResponse::ImportAll;
	return HandleImport();
}
FReply UEFAnimWidget::OnImport()
{
	UserDlgResponse = UEFAnimImportOptionDlgResponse::Import;
	return HandleImport();
}
FReply UEFAnimWidget::OnCancel()
{
	UserDlgResponse = UEFAnimImportOptionDlgResponse::Cancel;
	return FReply::Handled();
}
FReply UEFAnimWidget::HandleImport()
{
	if (WidgetWindow.IsValid())
	{
		WidgetWindow.Pin()->RequestDestroyWindow();
	}
	return FReply::Handled();
}
END_SLATE_FUNCTION_BUILD_OPTIMIZATION

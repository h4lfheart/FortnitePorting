// Fill out your copyright notice in the Description page of Project Settings.

#pragma once
#include "CoreMinimal.h"
#include "UEFAnimImportOptions.h"
#include "Widgets/SCompoundWidget.h"

enum class UEFAnimImportOptionDlgResponse : uint8
{
	Import,
	ImportAll,
	Cancel
};

class UEFORMAT_API UEFAnimWidget : public SCompoundWidget
{
public:
	SLATE_BEGIN_ARGS(UEFAnimWidget) : _WidgetWindow()
	{}

	SLATE_ARGUMENT(TSharedPtr<SWindow>, WidgetWindow)
	SLATE_END_ARGS()

	UEFAnimWidget() : UserDlgResponse(UEFAnimImportOptionDlgResponse::Cancel)
	{}
	/** Constructs this widget with InArgs */
	void Construct(const FArguments& InArgs);

	/** A property view to edit advanced options */
	TSharedPtr< class IDetailsView > PropertyView;

	UPROPERTY(Category = MapsAndSets, EditAnywhere)
	mutable UEFAnimImportOptions* Stun;

	bool bShouldImportAll;
	/** If we should import */
	bool ShouldImport();

	/** If the current settings should be applied to all items being imported */
	bool ShouldImportAll();


	FReply OnImportAll();
	/** Called when 'Apply' button is pressed */
	FReply OnImport();

	FReply OnCancel();
private:
	UEFAnimImportOptionDlgResponse	UserDlgResponse;
	FReply HandleImport();

	/** Window that owns us */
	TWeakPtr< SWindow >	WidgetWindow;
};

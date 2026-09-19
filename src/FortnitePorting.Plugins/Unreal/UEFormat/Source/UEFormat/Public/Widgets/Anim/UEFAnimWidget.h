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
	SLATE_BEGIN_ARGS(UEFAnimWidget) : _WidgetWindow(){}

	SLATE_ARGUMENT(TSharedPtr<SWindow>, WidgetWindow)
	SLATE_END_ARGS()

	UEFAnimWidget() : UserDlgResponse(UEFAnimImportOptionDlgResponse::Cancel)
	{}
	void Construct(const FArguments& InArgs);

	TSharedPtr< class IDetailsView > PropertyView;

	UPROPERTY(Category = MapsAndSets, EditAnywhere)
	mutable UEFAnimImportOptions* Stun;

	bool bShouldImportAll;
	bool ShouldImport();

	bool ShouldImportAll();


	FReply OnImportAll();
	FReply OnImport();

	FReply OnCancel();
private:
	UEFAnimImportOptionDlgResponse	UserDlgResponse;
	FReply HandleImport();

	TWeakPtr< SWindow >	WidgetWindow;
};

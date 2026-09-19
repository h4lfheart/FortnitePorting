#pragma once

#include "CoreMinimal.h"
#include "Data/UEFormatHeader.h"
#include "Math/Color.h"
#include "Math/Quat.h"
#include "Version/EUEFormatVersion.h"
#include <string>

class UEFORMAT_API FUEFormatReader
{
public:
	FUEFormatReader() = default;
	explicit FUEFormatReader(TArray<uint8> InData);

	EUEFormatVersion FileVersion = EUEFormatVersion::BeforeCustomVersionWasAdded;

	bool AtEnd() const;
	int32 Tell() const;
	int32 Remaining() const;
	bool CanRead(int32 Count) const;

	void Seek(int32 Position);
	void Skip(int32 Count);

	FUEFormatReader Chunk(int32 ByteSize);
	FUEFormatReader ReadRemaining();

	bool ReadBool();
	uint8 ReadByte();
	int32 ReadInt();
	int16 ReadShort();
	uint16 ReadUShort();
	float ReadFloat();
	FVector3f ReadVector();
	FQuat4f ReadQuat();
	FColor ReadColor();
	void ReadColorArray(int32 Count, TArray<FColor>& Out);
	std::string ReadFString();
	std::string ReadFixedString(int32 Size);
	TArray<uint8> ReadBytes(int32 Count);

	template<typename T>
	T Read()
	{
		if (!CanRead(static_cast<int32>(sizeof(T))))
		{
			UE_LOG(LogTemp, Error, TEXT("UEFormat: read past end of buffer"));
			return T{};
		}

		T Value;
		FMemory::Memcpy(&Value, Cursor(), sizeof(T));
		Offset += static_cast<int32>(sizeof(T));
		return Value;
	}

	template<typename T>
	void ReadArray(int32 Count, TArray<T>& Out)
	{
		Out.Reset();
		if (Count <= 0)
		{
			return;
		}

		const int32 ByteCount = Count * static_cast<int32>(sizeof(T));
		if (!CanRead(ByteCount))
		{
			UE_LOG(LogTemp, Error, TEXT("UEFormat: read past end of buffer"));
			return;
		}

		Out.SetNumUninitialized(Count);
		FMemory::Memcpy(Out.GetData(), Cursor(), ByteCount);
		Offset += ByteCount;
	}

private:
	TSharedPtr<TArray<uint8>> Storage;
	int32 Origin = 0;
	int32 Limit = 0;
	int32 Offset = 0;

	const uint8* Cursor() const;
};

namespace UEFormat
{
	bool LoadFile(const FString& Filename, FUEFormatHeader& OutHeader, FUEFormatReader& OutBody);
}

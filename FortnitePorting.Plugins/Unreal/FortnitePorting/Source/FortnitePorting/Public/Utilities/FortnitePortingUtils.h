#pragma once
#include <functional>
#include <optional>

#include "JsonObjectConverter.h"

class FFortnitePortingUtils
{
public:
	static FString BytesToString(TArray<uint8> Bytes)
	{
		/*auto BytesLength = Bytes.Num();
		if (BytesLength <= 0)
		{
			return FString("");
		}
		if (Bytes.Num() < BytesLength)
		{
			return FString("");
		}

		TArray<uint8> StringAsArray;
		StringAsArray.Reserve(BytesLength);

		for (int i = 0; i < BytesLength; i++)
		{
			StringAsArray.Add(Bytes[0]);
			Bytes.RemoveAt(0);
		}

		std::string cstr(reinterpret_cast<const char*>(StringAsArray.GetData()), StringAsArray.Num());
		return FString(UTF8_TO_TCHAR(cstr.c_str()));*/
		
		auto Data = Bytes.GetData();
		auto Count = Bytes.Num();

		FString Result;
		Result.Empty(Count);

		while (Count)
		{
			const int16 Value = *Data;

			Result += static_cast<TCHAR>(Value);

			++Data;
			Count--;
		}
		return Result;
	}
	
	template<typename T>
	static TOptional<T> FirstOrNull(TArray<T> Items, std::function<bool(const T&)> Predicate)
	{
		for (const auto& Item : Items)
		{
			if (Predicate(Item))
			{
				return Item;
			}
		}
		
		return T();
	}
	
	template<typename T>
	static bool Any(TArray<T> Items, std::function<bool(const T&)> Predicate)
	{
		for (const auto& Item : Items)
		{
			if (Predicate(Item))
			{
				return true;
			}
		}
		
		return false;
	}
};

#pragma once

#include "CoreMinimal.h"
#include "Dom/JsonObject.h"
#include "Dom/JsonValue.h"

class FJsonWrapper
{
public:
    FJsonWrapper() = default;
    explicit FJsonWrapper(const TSharedPtr<FJsonObject>& InObject) : JsonObject(InObject) {}
    explicit FJsonWrapper(const TSharedPtr<FJsonValue>& InValue) 
    {
        if (InValue.IsValid())
        {
            JsonObject = InValue->AsObject();
        }
    }

    bool IsValid() const { return JsonObject.IsValid(); }
    operator bool() const { return IsValid(); }

    TSharedPtr<FJsonObject> GetObject() const { return JsonObject; }

    // Primary template for Get - handles enums
    template<typename T>
    typename TEnableIf<TIsEnum<T>::Value, T>::Type
    Get(const FString& Field, const T& DefaultValue = T()) const
    {
        if (!JsonObject.IsValid()) return DefaultValue;
        if (!JsonObject->HasField(Field)) return DefaultValue;
        int32 IntValue = JsonObject->GetIntegerField(Field);
        return static_cast<T>(IntValue);
    }

    // Forward declaration for non-enum specialized types
    template<typename T>
    typename TEnableIf<!TIsEnum<T>::Value, T>::Type
    Get(const FString& Field, const T& DefaultValue = T()) const;
    
    FJsonWrapper operator[](const FString& Field) const;
    
    // Overload for string literals - handles both narrow and wide strings
    template<size_t N>
    FJsonWrapper operator[](const TCHAR (&Field)[N]) const
    {
        return Get<FJsonWrapper>(FString(Field));
    }
    
    template<size_t N>
    FJsonWrapper operator[](const char (&Field)[N]) const
    {
        return Get<FJsonWrapper>(FString(Field));
    }

    TArray<FJsonWrapper> GetArray(const FString& Field) const
    {
        TArray<FJsonWrapper> Result;
        if (!JsonObject.IsValid()) return Result;

        const TArray<TSharedPtr<FJsonValue>>* Array;
        if (JsonObject->TryGetArrayField(Field, Array))
        {
            for (const auto& Item : *Array)
            {
                Result.Add(FJsonWrapper(Item));
            }
        }
        return Result;
    }

    template<typename T>
    bool TryGet(const FString& Field, T& OutValue) const;

private:
    TSharedPtr<FJsonObject> JsonObject;
};

// Template specializations for non-enum types
template<>
inline FString FJsonWrapper::Get<FString>(const FString& Field, const FString& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    return JsonObject->HasField(Field) ? JsonObject->GetStringField(Field) : DefaultValue;
}

template<>
inline int32 FJsonWrapper::Get<int32>(const FString& Field, const int32& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    return JsonObject->HasField(Field) ? JsonObject->GetIntegerField(Field) : DefaultValue;
}

template<>
inline float FJsonWrapper::Get<float>(const FString& Field, const float& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    return JsonObject->HasField(Field) ? static_cast<float>(JsonObject->GetNumberField(Field)) : DefaultValue;
}

template<>
inline double FJsonWrapper::Get<double>(const FString& Field, const double& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    return JsonObject->HasField(Field) ? JsonObject->GetNumberField(Field) : DefaultValue;
}

template<>
inline bool FJsonWrapper::Get<bool>(const FString& Field, const bool& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    return JsonObject->HasField(Field) ? JsonObject->GetBoolField(Field) : DefaultValue;
}

template<>
inline FJsonWrapper FJsonWrapper::Get<FJsonWrapper>(const FString& Field, const FJsonWrapper& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    const TSharedPtr<FJsonObject>* Obj;
    return JsonObject->TryGetObjectField(Field, Obj) ? FJsonWrapper(*Obj) : DefaultValue;
}

template<>
inline FVector FJsonWrapper::Get<FVector>(const FString& Field, const FVector& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    const TSharedPtr<FJsonObject>* Obj;
    if (JsonObject->TryGetObjectField(Field, Obj))
    {
        return FVector(
            (*Obj)->GetNumberField(TEXT("X")),
            (*Obj)->GetNumberField(TEXT("Y")),
            (*Obj)->GetNumberField(TEXT("Z"))
        );
    }
    return DefaultValue;
}

template<>
inline FRotator FJsonWrapper::Get<FRotator>(const FString& Field, const FRotator& DefaultValue) const
{
    if (!JsonObject.IsValid()) return DefaultValue;
    const TSharedPtr<FJsonObject>* Obj;
    if (JsonObject->TryGetObjectField(Field, Obj))
    {
        return FRotator(
            (*Obj)->GetNumberField(TEXT("Pitch")),
            (*Obj)->GetNumberField(TEXT("Yaw")),
            (*Obj)->GetNumberField(TEXT("Roll"))
        );
    }
    return DefaultValue;
}

template<typename T>
inline bool FJsonWrapper::TryGet(const FString& Field, T& OutValue) const
{
    if (!JsonObject.IsValid() || !JsonObject->HasField(Field)) return false;
    OutValue = Get<T>(Field);
    return true;
}

inline FJsonWrapper FJsonWrapper::operator[](const FString& Field) const
{
    return Get<FJsonWrapper>(Field);
}
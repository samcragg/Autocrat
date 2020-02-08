#ifndef MANAGED_OBJECTS_H
#define MANAGED_OBJECTS_H

#include <cstddef>
#include <cstdint>

// Taken from CoreRT Runtime/inc/eetype.h
struct EEType
{
    std::uint16_t m_usComponentSize;
    std::uint16_t m_usFlags;
    std::uint32_t m_uBaseSize;
    EEType* m_RelatedType;
    std::uint16_t m_usNumVtableSlots;
    std::uint16_t m_usNumInterfaces;
    std::uint32_t m_uHashCode;
    void* m_ppTypeManager;
};

extern EEType object_type;
extern EEType value_type;
extern EEType boxed_int32_type;
extern EEType array_int32_type;
extern EEType SingleReferenceArrayType;

// Taken from Runtime/ObjectLayout.h
struct Object
{
    EEType* m_pEEType;
};

struct Array : Object
{
    std::uint32_t m_Length;
    std::uint32_t m_uAlignpad;
};

// Taken from gc/gcdesc.h
struct val_array_series
{
    std::size_t m_startOffset;
    std::size_t m_count;
};

struct CGCDescSeries
{
    val_array_series val_series[3];
    size_t seriessize;
};

struct BaseClass : Object
{
    void* BaseReference;
    std::int32_t BaseInteger;

    char padding[4];
};
static_assert(sizeof(BaseClass) == 24u);

struct BaseClassInfoType
{
    std::size_t CGCDescSeries[3]{ 0xffffffffffffffe8, 0x08, 0x01 };
    EEType Type{ 0u, 288u, 32u, &object_type, 0u, 0u, 0u, nullptr };
};
extern BaseClassInfoType BaseClassInfo;

// Can't use C++ inheritance due to padding
struct DerivedClass : Object
{
    void* BaseReference;
    std::int32_t BaseInteger;

    std::int32_t Integer;
    void* FirstReference;
    void* SecondReference;

    char padding[8];
};
static_assert(sizeof(DerivedClass) == 48u);

struct DerivedClassInfoType
{
    std::size_t CGCDescSeries[5]{ 0xffffffffffffffe0, 0x18, 0xffffffffffffffd8, 0x08, 0x02 };
    EEType Type{ 0u, 32u, 48u, &BaseClassInfo.Type, 0u, 0u, 0u, nullptr };
};
extern DerivedClassInfoType DerivedClassInfo;

struct SingleReference : Object
{
    void* Reference;

    char padding[8];
};
static_assert(sizeof(SingleReference) == 24u);

struct SingleReferenceInfoType
{
    std::size_t CGCDescSeries[3]{ 0xfffffffffffffff0, 0x08, 0x01 };
    EEType Type{ 0u, 32u, 24u, &object_type, 0u, 0u, 0u, nullptr };
};
extern SingleReferenceInfoType SingleReferenceInfo;

template <std::uint32_t Sz>
struct ManagedArray : Array
{
    ManagedArray() :
        references{}
    {
        m_Length = Sz;
    }

    void* references[Sz];
};

#endif

#ifndef MANAGED_OBJECTS_H
#define MANAGED_OBJECTS_H

#include <cstddef>
#include <cstdint>

struct EEType;
EEType* GetBoxedIntArrayType();
EEType* GetReferenceArrayType();

// Taken from Runtime/ObjectLayout.h
struct Object
{
    Object();

    EEType* m_pEEType;
};

struct Array : Object
{
    explicit Array(std::uint32_t size);

    std::uint32_t m_Length;
    std::uint32_t m_uAlignpad;
};

struct BaseClass : Object
{
    BaseClass();

    void* BaseReference;
    std::int32_t BaseInteger;

    char padding[12];
};

// Can't use C++ inheritance due to padding
struct DerivedClass : Object
{
    DerivedClass();

    void* BaseReference;
    std::int32_t BaseInteger;

    std::int32_t Integer;
    void* FirstReference;
    void* SecondReference;

    char padding[8];
};

template <std::uint32_t Sz>
struct Int32Array : Array
{
    Int32Array() :
        Array(Sz),
        elements{}
    {
        m_pEEType = GetBoxedIntArrayType();
    }

    std::int32_t elements[Sz];
};

template <class T>
class ManagedObject
{
public:
    T* operator->()
    {
        return get();
    }

    T* get()
    {
        return &_object;
    }
private:
    char _gc_info[8];
    T _object;
};

template <std::uint32_t Sz>
struct ReferenceArray : Array
{
    ReferenceArray() :
        Array(Sz),
        references{},
        padding{}
    {
        m_pEEType = GetReferenceArrayType();
    }

    void* references[Sz];
    char padding[8];
};

struct SingleReference : Object
{
    SingleReference();

    void* Reference;

    char padding[16];
};

#endif

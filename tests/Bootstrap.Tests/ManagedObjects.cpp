#include "ManagedObjects.h"

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

// These values were obtained from a 64-bit CoreRT project. The members are
// in the order the CLR puts them, not the C# declaration order
struct ManagedTypes
{
    ManagedTypes()
    {
        static_assert(sizeof(::ReferenceArray<2u>) == 40u);
        ArrayOfInt32 = { 4u, 258u, 24u, &BoxedInt32, 0u, 0u, 0u, nullptr };
        ArrayOfSingleReference = { 8u, 290u, 24u, &SingleReference, 0u, 0u, 0u, nullptr };

        static_assert(sizeof(::BaseClass) == 32u);
        BaseClass = { 0u, 288u, 32u, &Object, 0u, 0u, 0u, nullptr };

        BoxedInt32 = { 0u, 16648u, 24u, &ValueType, 0u, 0u, 0u, nullptr };

        static_assert(sizeof(::DerivedClass) == 48u);
        DerivedClass = { 0u, 32u, 48u, &BaseClass, 0u, 0u, 0u, nullptr };

        Object = { 0u, 256u, 24u, nullptr, 0u, 0u, 0u, nullptr };

        static_assert(sizeof(::SingleReference) == 32u);
        SingleReference = { 0u, 32u, 24u, &Object, 0u, 0u, 0u, nullptr };

        ValueType = { 0u, 256u, 24u, &Object, 0u, 0u, 0u, nullptr };
    }

    EEType ArrayOfInt32;
    EEType ArrayOfSingleReference;

    std::size_t BaseClassGCDescSeries[3]{ 0xffffffffffffffe8, 0x08, 0x01 };
    EEType BaseClass;

    EEType BoxedInt32;

    std::size_t DerivedClassGCDescSeries[5]{ 0xffffffffffffffe0, 0x18, 0xffffffffffffffd8, 0x08, 0x02 };
    EEType DerivedClass;

    EEType Object;

    std::size_t SingeReferenceGCDescSeries[3]{ 0xfffffffffffffff0, 0x08, 0x01 };
    EEType SingleReference;

    EEType ValueType;
};

ManagedTypes Types;

EEType* GetBoxedIntArrayType()
{
    return &Types.ArrayOfInt32;
}

EEType* GetReferenceArrayType()
{
    return &Types.ArrayOfSingleReference;
}

Array::Array(std::uint32_t size) :
    m_Length(size),
    m_uAlignpad(0)
{
}

Object::Object() :
    m_pEEType(&Types.Object)
{
}

BaseClass::BaseClass() :
    BaseReference(nullptr),
    BaseInteger(0),
    padding{}
{
    m_pEEType = &Types.BaseClass;
}

DerivedClass::DerivedClass() :
    BaseReference(nullptr),
    BaseInteger(0),
    Integer(0),
    FirstReference(nullptr),
    SecondReference(nullptr),
    padding{}
{
    m_pEEType = &Types.DerivedClass;
}

SingleReference::SingleReference() :
    Reference(nullptr),
    padding{}
{
    m_pEEType = &Types.SingleReference;
}

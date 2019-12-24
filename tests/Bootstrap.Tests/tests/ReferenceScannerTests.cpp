#include "managed_interop.h"

#include <gtest/gtest.h>

namespace
{
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
        val_array_series val_serie[3];
        size_t seriessize;
    };

    // These values were obtained from a 64-bit CoreRT project
    EEType object_type = { 0u, 256u, 24u, nullptr, 0u, 0u, 0u, nullptr };
    EEType value_type = { 0u, 256u, 24u, &object_type, 0u, 0u, 0u, nullptr };
    EEType boxed_int32_type = { 0u, 16648u, 24u, &value_type, 0u, 0u, 0u, nullptr };
    EEType array_int32_type = { 4u, 258u, 24u, &boxed_int32_type, 0u, 0u, 0u, nullptr };

    struct BaseClass : Object
    {
        void* BaseReference;
        std::int32_t BaseInteger;
    };

    struct
    {
        std::size_t CGCDescSeries[3]{ 0xffffffffffffffe8, 0x08, 0x01 };
        EEType Type{ 0u, 288u, 32u, &object_type, 0u, 0u, 0u, nullptr };
    } BaseClassInfo;

    struct DerivedClass : BaseClass
    {
        void* FirstReference;
        std::int32_t Integer;
        void* SecondReference;
    };

    struct
    {
        std::size_t CGCDescSeries[5]{ 0xffffffffffffffe0, 0x18, 0xffffffffffffffd8, 0x08, 0x02 };
        EEType Type{ 0u, 32u, 48u, &BaseClassInfo.Type, 0u, 0u, 0u, nullptr };
    } DerivedClassInfo;

    struct SingleReference : Object
    {
        void* Reference;
    };

    struct
    {
        std::size_t CGCDescSeries[3]{ 0xfffffffffffffff0, 0x08, 0x01 };
        EEType Type{ 0u, 32u, 24u, &object_type, 0u, 0u, 0u, nullptr };
    } SingleReferenceInfo;

    EEType SingleReferenceArrayType = { 8u, 290u, 24u, &SingleReferenceInfo.Type, 0u, 0u, 0u, nullptr };
}

class ReferenceScannerTests : public testing::Test
{
protected:
    autocrat::reference_scanner _scanner;
};

TEST_F(ReferenceScannerTests, ScanShouldHandleNullObjects)
{
    _scanner.scan(static_cast<void*>(nullptr));

    EXPECT_EQ(0u, _scanner.bytes());
}

TEST_F(ReferenceScannerTests, ShouldCalculateTheSizeOfEmptyObjects)
{
    Object value = {};
    value.m_pEEType = &object_type;

    _scanner.scan(&value);

    EXPECT_EQ(24u, _scanner.bytes());
}

TEST_F(ReferenceScannerTests, ShouldCalculateTheSizeWithCircularReferences)
{
    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &object;

    _scanner.scan(&object);

    EXPECT_EQ(24u, _scanner.bytes());
}

TEST_F(ReferenceScannerTests, ShouldCalculateTheSizeWithReferenceArrays)
{
    struct
    {
        Array header;
        void* references[5];
    } array = {};
    array.header.m_pEEType = &SingleReferenceArrayType;
    array.header.m_Length = 5u;

    SingleReference element = {};
    element.m_pEEType = &SingleReferenceInfo.Type;
    array.references[2] = &element;

    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &array;

    _scanner.scan(&object);

    EXPECT_EQ(24u + 24u + 40u + 24u, _scanner.bytes());
}

TEST_F(ReferenceScannerTests, ShouldCalculateTheSizeWithInheritedReferences)
{
    Object empty = {};
    empty.m_pEEType = &object_type;

    DerivedClass derived = {};
    derived.m_pEEType = &DerivedClassInfo.Type;
    derived.BaseReference = &empty;

    _scanner.scan(&derived);

    EXPECT_EQ(48u + 24u, _scanner.bytes());
}

TEST_F(ReferenceScannerTests, ShouldCalculateTheSizeWithValueArrays)
{
    Array array = {};
    array.m_pEEType = &array_int32_type;
    array.m_Length = 3u;

    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &array;

    _scanner.scan(&object);

    EXPECT_EQ(24u + 24u + 12u, _scanner.bytes());
}

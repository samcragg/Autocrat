#include "managed_interop.h"

#include <algorithm>
#include <array>
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

    // These values were obtained from a 64-bit CoreRT project. The members are
    // in the order the CLR puts them, not the C# declaration order
    EEType object_type = { 0u, 256u, 24u, nullptr, 0u, 0u, 0u, nullptr };
    EEType value_type = { 0u, 256u, 24u, &object_type, 0u, 0u, 0u, nullptr };
    EEType boxed_int32_type = { 0u, 16648u, 24u, &value_type, 0u, 0u, 0u, nullptr };
    EEType array_int32_type = { 4u, 258u, 24u, &boxed_int32_type, 0u, 0u, 0u, nullptr };

    struct BaseClass : Object
    {
        void* BaseReference;
        std::int32_t BaseInteger;

        char padding[4];
    };
    static_assert(sizeof(BaseClass) == 24u);

    struct
    {
        std::size_t CGCDescSeries[3]{ 0xffffffffffffffe8, 0x08, 0x01 };
        EEType Type{ 0u, 288u, 32u, &object_type, 0u, 0u, 0u, nullptr };
    } BaseClassInfo;

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

    struct
    {
        std::size_t CGCDescSeries[5]{ 0xffffffffffffffe0, 0x18, 0xffffffffffffffd8, 0x08, 0x02 };
        EEType Type{ 0u, 32u, 48u, &BaseClassInfo.Type, 0u, 0u, 0u, nullptr };
    } DerivedClassInfo;

    struct SingleReference : Object
    {
        void* Reference;

        char padding[8];
    };
    static_assert(sizeof(SingleReference) == 24u);

    struct
    {
        std::size_t CGCDescSeries[3]{ 0xfffffffffffffff0, 0x08, 0x01 };
        EEType Type{ 0u, 32u, 24u, &object_type, 0u, 0u, 0u, nullptr };
    } SingleReferenceInfo;

    EEType SingleReferenceArrayType = { 8u, 290u, 24u, &SingleReferenceInfo.Type, 0u, 0u, 0u, nullptr };

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
}

struct ObjectCounter : autocrat::object_mover
{
    void* move_object(const std::byte* object, std::size_t size) override
    {
        bytes += size;
        references++;
        return const_cast<std::byte*>(object);
    }

    void set_reference(std::byte*, std::size_t, void*) override
    {
    }

    std::size_t bytes = 0;
    std::size_t references = 0;
};

struct ObjectMover : autocrat::object_mover
{
    void* move_object(const std::byte* object, std::size_t size) override
    {
        EXPECT_LT(size, buffer.size() - allocated);

        std::byte* new_object = buffer.data() + allocated;
        std::copy_n(object, size, new_object);
        allocated += size;
        return new_object;
    }

    void set_reference(std::byte* object, std::size_t offset, void* reference) override
    {
        EXPECT_GE(object, buffer.data());
        EXPECT_LT(object + offset + sizeof(void*), buffer.data() + buffer.size());

        std::copy_n(reinterpret_cast<std::byte*>(&reference), sizeof(void*), object + offset);
    }

    std::size_t allocated = 0;
    std::array<std::byte, 1024u> buffer = {};
};

class ReferenceScannerTests : public testing::Test
{
protected:
    ReferenceScannerTests() :
        _scanner(_counter)
    {
    }

    ObjectCounter _counter;
    autocrat::reference_scanner _scanner;
};

TEST_F(ReferenceScannerTests, MoveShouldHandleNullObjects)
{
    _scanner.move(static_cast<void*>(nullptr));

    EXPECT_EQ(0u, _counter.bytes);
    EXPECT_EQ(0u, _counter.references);
}

TEST_F(ReferenceScannerTests, ShouldMoveTheReferencesAndData)
{
    SingleReference element = {};
    element.m_pEEType = &SingleReferenceInfo.Type;

    ManagedArray<3u> array;
    array.m_pEEType = &SingleReferenceArrayType;
    array.references[0] = &element;
    array.references[2] = &element;

    DerivedClass original = {};
    original.m_pEEType = &DerivedClassInfo.Type;
    original.BaseInteger = 123;
    original.Integer = 456;
    original.SecondReference = &array;

    ObjectMover mover;
    _scanner = autocrat::reference_scanner(mover);
    _scanner.move(&original);

    // Clear the originals to prove the copy is independent
    array = {};
    element = {};
    original = {};

    auto copy = reinterpret_cast<DerivedClass*>(mover.buffer.data());
    EXPECT_EQ(copy->BaseInteger, 123);
    EXPECT_EQ(copy->Integer, 456);
    
    auto array_copy = static_cast<ManagedArray<3u>*>(copy->SecondReference);
    EXPECT_EQ(array_copy->references[0], array_copy->references[2]);
    EXPECT_GE(array_copy->references[0], mover.buffer.data());
    EXPECT_LT(array_copy->references[0], mover.buffer.data() + mover.allocated);
    EXPECT_EQ(nullptr, array_copy->references[1]);
}

TEST_F(ReferenceScannerTests, ShouldScanEmptyObjects)
{
    Object value = {};
    value.m_pEEType = &object_type;

    _scanner.move(&value);

    EXPECT_EQ(24u, _counter.bytes);
    EXPECT_EQ(1u, _counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanCircularReferences)
{
    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &object;

    _scanner.move(&object);

    EXPECT_EQ(24u, _counter.bytes);
    EXPECT_EQ(1u, _counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanReferenceArrays)
{
    SingleReference element = {};
    element.m_pEEType = &SingleReferenceInfo.Type;

    ManagedArray<5u> array;
    array.m_pEEType = &SingleReferenceArrayType;
    array.references[2] = &element;

    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &array;

    _scanner.move(&object);

    EXPECT_EQ(24u + 24u + 40u + 24u, _counter.bytes);
    EXPECT_EQ(3u, _counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanInheritedReferences)
{
    Object empty = {};
    empty.m_pEEType = &object_type;

    DerivedClass derived = {};
    derived.m_pEEType = &DerivedClassInfo.Type;
    derived.BaseReference = &empty;

    _scanner.move(&derived);

    EXPECT_EQ(48u + 24u, _counter.bytes);
    EXPECT_EQ(2u, _counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanTypesWithValueArrays)
{
    Array array = {};
    array.m_pEEType = &array_int32_type;
    array.m_Length = 3u;

    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &array;

    _scanner.move(&object);

    EXPECT_EQ(24u + 24u + 12u, _counter.bytes);
    EXPECT_EQ(2u, _counter.references);
}

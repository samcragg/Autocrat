#include "managed_interop.h"

#include <algorithm>
#include <array>
#include <unordered_map>
#include <gtest/gtest.h>
#include "ManagedObjects.h"

struct ObjectCounter : autocrat::detail::reference_scanner
{
    std::optional<void*> get_moved_location(void* object) override
    {
        auto it = moved_objects.find(object);
        if (it == moved_objects.end())
        {
            return std::nullopt;
        }
        else
        {
            return it->second;
        }
    }

    void* get_reference(void* object, std::size_t offset) override
    {
        void* address_of_field = static_cast<std::byte*>(object) + offset;
        return *static_cast<void**>(address_of_field);
    }

    void* move_object(void* object, std::size_t size) override
    {
        allocated_bytes += size;
        references++;
        return object;
    }

    void set_moved_location(void* object, void* new_location) override
    {
        moved_objects[object] = new_location;
    }

    void set_reference(void*, std::size_t, void*) override
    {
    }

    std::size_t allocated_bytes = 0;
    std::size_t references = 0;
    std::unordered_map<void*, void*> moved_objects;
};

struct ObjectMover : ObjectCounter
{
    void* move_object(void* object, std::size_t size) override
    {
        EXPECT_LT(size, buffer.size() - allocated_bytes);

        std::byte* new_object = buffer.data() + allocated_bytes;
        std::copy_n(static_cast<std::byte*>(object), size, new_object);
        allocated_bytes += size;
        return new_object;
    }

    void set_reference(void* object, std::size_t offset, void* reference) override
    {
        if (reference != nullptr)
        {
            auto data = static_cast<std::byte*>(object);
            EXPECT_GE(data, buffer.data());
            EXPECT_LT(data + offset + sizeof(void*), buffer.data() + buffer.size());
            std::copy_n(reinterpret_cast<std::byte*>(&reference), sizeof(void*), data + offset);
        }
    }

    std::array<std::byte, 1024u> buffer = {};
};

class ReferenceScannerTests : public testing::Test
{
protected:
};

TEST_F(ReferenceScannerTests, MoveShouldHandleNullObjects)
{
    ObjectCounter counter;
    counter.move(static_cast<void*>(nullptr));

    EXPECT_EQ(0u, counter.allocated_bytes);
    EXPECT_EQ(0u, counter.references);
}

TEST_F(ReferenceScannerTests, ShouldMoveTheReferencesAndData)
{
    ManagedObject<SingleReference> element;

    ManagedObject<ReferenceArray<3u>> array;
    array->references[0] = element.get();
    array->references[2] = element.get();

    ManagedObject<DerivedClass> original;
    original->BaseInteger = 123;
    original->Integer = 456;
    original->SecondReference = array.get();

    ObjectMover mover;
    mover.move(original.get());

    // Clear the originals to prove the copy is independent
    array = {};
    element = {};
    original = {};

    auto copy = reinterpret_cast<DerivedClass*>(mover.buffer.data());
    EXPECT_EQ(copy->BaseInteger, 123);
    EXPECT_EQ(copy->Integer, 456);
    
    auto array_copy = static_cast<ReferenceArray<3u>*>(copy->SecondReference);
    EXPECT_EQ(array_copy->references[0], array_copy->references[2]);
    EXPECT_GE(array_copy->references[0], mover.buffer.data());
    EXPECT_LT(array_copy->references[0], mover.buffer.data() + mover.allocated_bytes);
    EXPECT_EQ(nullptr, array_copy->references[1]);
}

TEST_F(ReferenceScannerTests, ShouldScanEmptyObjects)
{
    ManagedObject<Object> value;

    ObjectCounter counter;
    counter.move(value.get());

    EXPECT_EQ(24u, counter.allocated_bytes);
    EXPECT_EQ(1u, counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanCircularReferences)
{
    ManagedObject<SingleReference> object;
    object->Reference = object.get();

    ObjectCounter counter;
    counter.move(object.get());

    EXPECT_EQ(24u, counter.allocated_bytes);
    EXPECT_EQ(1u, counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanReferenceArrays)
{
    ManagedObject<SingleReference> element;

    ManagedObject<ReferenceArray<5u>> array;
    array->references[2] = element.get();

    ManagedObject<SingleReference> object;
    object->Reference = array.get();

    ObjectCounter counter;
    counter.move(object.get());

    EXPECT_EQ(24u + 24u + 40u + 24u, counter.allocated_bytes);
    EXPECT_EQ(3u, counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanInheritedReferences)
{
    ManagedObject<Object> empty;

    ManagedObject<DerivedClass> derived;
    derived->BaseReference = empty.get();

    ObjectCounter counter;
    counter.move(derived.get());

    EXPECT_EQ(48u + 24u, counter.allocated_bytes);
    EXPECT_EQ(2u, counter.references);
}

TEST_F(ReferenceScannerTests, ShouldScanTypesWithValueArrays)
{
    ManagedObject<Int32Array<3u>> array;

    ManagedObject<SingleReference> object;
    object->Reference = array.get();

    ObjectCounter counter;
    counter.move(object.get());

    EXPECT_EQ(24u + 24u + 12u, counter.allocated_bytes);
    EXPECT_EQ(2u, counter.references);
}

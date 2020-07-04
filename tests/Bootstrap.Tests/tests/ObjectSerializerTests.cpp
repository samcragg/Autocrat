#include "managed_interop.h"

#include <gtest/gtest.h>
#include "ManagedObjects.h"
#include "mock_services.h"

class ObjectSerializerTests : public testing::Test
{
protected:
    autocrat::object_serializer _serializer;
};

TEST_F(ObjectSerializerTests, ShouldRoundtripObjectState)
{
    // Simulate the following object graph:
    //
    // new SingleReferenceType[]
    // {
    //     null,
    //     new SingleReferenceType
    //     {
    //         Reference = new BaseClass { BaseInteger = 123 },
    //     },
    // }
    ManagedObject<BaseClass> base_class;
    base_class->BaseInteger = 123;

    ManagedObject<SingleReference> element;
    element->Reference = base_class.get();

    ManagedObject<ReferenceArray<2u>> array;
    array->references[1] = element.get();

    _serializer.save(array.get());

    std::array<std::byte, 1024> buffer;
    When(mock_global_services.gc_service().allocate)
        .Do([&](std::size_t sz)
            {
                EXPECT_LT(sz, buffer.size());
                return buffer.data();
            });

    // Clear the original to prove the serialization worked
    array->references[1] = nullptr;
    base_class->BaseInteger = 0;

    void* result = _serializer.restore();
    EXPECT_EQ(buffer.data(), result);

    auto copy_array = static_cast<ReferenceArray<2u>*>(result);
    EXPECT_NE(nullptr, copy_array->m_pEEType);
    EXPECT_EQ(2u, copy_array->m_Length);
    EXPECT_EQ(nullptr, copy_array->references[0]);
    ASSERT_NE(nullptr, copy_array->references[1]);

    auto copy_element = static_cast<SingleReference*>(copy_array->references[1]);
    EXPECT_NE(nullptr, copy_element->m_pEEType);
    ASSERT_NE(nullptr, copy_element->Reference);

    auto copy_base = static_cast<BaseClass*>(copy_element->Reference);
    EXPECT_NE(nullptr, copy_base->m_pEEType);
    EXPECT_EQ(123, copy_base->BaseInteger);
}

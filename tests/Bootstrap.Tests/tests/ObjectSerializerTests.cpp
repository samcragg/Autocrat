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
    BaseClass base_class = {};
    base_class.m_pEEType = &BaseClassInfo.Type;
    base_class.BaseInteger = 123;

    SingleReference element = {};
    element.m_pEEType = &SingleReferenceInfo.Type;
    element.Reference = &base_class;

    ManagedArray<2u> array;
    array.m_pEEType = &SingleReferenceArrayType;
    array.references[1] = &element;

    _serializer.save(&array);

    std::array<std::byte, 1024> buffer;
    When(mock_global_services.gc_service().allocate)
        .Do([&](std::size_t sz)
            {
                EXPECT_LT(sz, buffer.size());
                return buffer.data();
            });

    // Clear the original to prove the serialization worked
    array.references[1] = nullptr;
    base_class.BaseInteger = 0;

    void* result = _serializer.restore();
    EXPECT_EQ(buffer.data(), result);

    auto copy_array = static_cast<decltype(array)*>(result);
    EXPECT_EQ(&SingleReferenceArrayType, copy_array->m_pEEType);
    EXPECT_EQ(2u, copy_array->m_Length);
    EXPECT_EQ(nullptr, copy_array->references[0]);
    ASSERT_NE(nullptr, copy_array->references[1]);

    auto copy_element = static_cast<decltype(element)*>(copy_array->references[1]);
    EXPECT_EQ(&SingleReferenceInfo.Type, copy_element->m_pEEType);
    ASSERT_NE(nullptr, copy_element->Reference);

    auto copy_base = static_cast<decltype(base_class)*>(copy_element->Reference);
    EXPECT_EQ(&BaseClassInfo.Type, copy_base->m_pEEType);
    EXPECT_EQ(123, copy_base->BaseInteger);
}

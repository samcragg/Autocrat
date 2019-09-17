#include "array_pool.h"
#include <gtest/gtest.h>
#include <algorithm>

namespace
{
    void* managed_byte_array_type = reinterpret_cast<void*>(0x12345678);

    extern "C" void* __cdecl GetByteArrayType()
    {
        return managed_byte_array_type;
    }
}

class ManagedArrayTests : public testing::Test
{
public:
protected:
    autocrat::managed_byte_array _array;
};

TEST_F(ManagedArrayTests, ConstructorShouldSetTheEEType)
{
    // For the array to be used in the managed code, the first member *must* be
    // the EEType
    void** members = reinterpret_cast<void**>(&_array);

    EXPECT_EQ(managed_byte_array_type, members[0]);
}

TEST_F(ManagedArrayTests, ClearShouldChangeTheSize)
{
    _array.resize(10);
    EXPECT_EQ(10, _array.size());

    _array.clear();
    EXPECT_EQ(0, _array.size());
}

TEST_F(ManagedArrayTests, ClearShouldZeroTheMemory)
{
    _array.resize(10);
    std::fill_n(_array.data(), 10, std::uint8_t{ 0x12 });

    _array.clear();

    EXPECT_TRUE(std::all_of(_array.data(), _array.data() + 10, [](std::uint8_t b) { return b == 0; }));
}


TEST_F(ManagedArrayTests, ResizeShouldZeroTheMemory)
{
    _array.resize(10);
    std::fill_n(_array.data(), 10, std::uint8_t{ 0x12 });

    _array.resize(5);

    EXPECT_EQ(5, _array.size());
    EXPECT_TRUE(std::all_of(_array.data() + 5, _array.data() + 10, [](std::uint8_t b) { return b == 0; }));
}

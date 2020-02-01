#include "memory_pool.h"

#include <algorithm>
#include <numeric>
#include <gtest/gtest.h>

namespace
{
    std::byte& operator++(std::byte& b)
    {
        b = static_cast<std::byte>(std::to_integer<unsigned>(b) + 1u);
        return b;
    }
}

class MemoryPoolTests : public testing::Test
{
protected:
    autocrat::memory_pool_buffer _buffer;

    template <std::size_t Sz>
    void AssertCopy(const std::array<std::byte, Sz>& source)
    {
        std::array<std::byte, Sz> copy;
        _buffer.move_to(copy.data(), copy.size());

        EXPECT_TRUE(std::equal(source.begin(), source.end(), copy.begin()));
    }

    template <std::size_t Sz>
    std::array<std::byte, Sz> CreateData()
    {
        std::array<std::byte, Sz> array;
        std::iota(array.begin(), array.end(), std::byte(1));
        return array;
    }
};

TEST_F(MemoryPoolTests, ShouldOverwriteTheSpecifiedData)
{
    // Force multiple nodes to be used via a large array
    auto array = CreateData<2000u>();
    _buffer.append(array.data(), array.size());

    std::fill_n(&array[1500], 10u, std::byte{ 1u });
    _buffer.replace(1500, &array[1500], 10u);

    AssertCopy(array);
}

TEST_F(MemoryPoolTests, ShouldRoundTripData)
{
    auto array = CreateData<16u>();

    _buffer.append(array.data(), array.size());

    AssertCopy(array);
}

TEST_F(MemoryPoolTests, ShouldRoundTripLargeData)
{
    auto array = CreateData<3000u>();

    _buffer.append(array.data(), array.size());

    AssertCopy(array);
}

TEST_F(MemoryPoolTests, ShouldRoundTripMultipleData)
{
    auto array = CreateData<48u>();

    _buffer.append(&array[0], 16u);
    _buffer.append(&array[16], 16u);
    _buffer.append(&array[32], 16u);

    AssertCopy(array);
}

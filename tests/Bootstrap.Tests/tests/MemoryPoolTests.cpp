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
};

TEST_F(MemoryPoolTests, ShouldRoundTripData)
{
    std::array<std::byte, 16u> array;
    std::iota(array.begin(), array.end(), std::byte(1));

    _buffer.append(array.data(), array.size());

    AssertCopy(array);
}

TEST_F(MemoryPoolTests, ShouldRoundTripLargeData)
{
    std::array<std::byte, 3000u> array;
    std::iota(array.begin(), array.end(), std::byte(1));

    _buffer.append(array.data(), array.size());

    AssertCopy(array);
}

TEST_F(MemoryPoolTests, ShouldRoundTripMultipleData)
{
    std::array<std::byte, 30u> array;
    std::iota(array.begin(), array.end(), std::byte(1));

    _buffer.append(&array[0], 10u);
    _buffer.append(&array[10], 10u);
    _buffer.append(&array[20], 10u);

    AssertCopy(array);
}

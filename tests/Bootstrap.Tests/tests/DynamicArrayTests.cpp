#include "collections.h"

#include <gtest/gtest.h>
#include "TestUtils.h"

class DynamicArrayTests : public testing::Test
{
public:
protected:
    autocrat::dynamic_array<MovableItem> _array;
};

TEST_F(DynamicArrayTests, ShouldCreateTheSpecifiedNumberOfElements)
{
    _array = autocrat::dynamic_array<MovableItem>(3u);

    EXPECT_EQ(3u, _array.size());
    for (std::size_t i = 0; i != 3u; ++i)
    {
        EXPECT_EQ(MoveType::None, _array[i].moved);
    }
}

TEST_F(DynamicArrayTests, ShouldIterateOverAllTheElements)
{
    _array = autocrat::dynamic_array<MovableItem>(5u);

    std::size_t iterations = 0;
    for (auto& x : _array)
    {
        ((void)x);
        iterations++;
    }

    EXPECT_EQ(5u, iterations);
}

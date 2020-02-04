#include "collections.h"

#include <gtest/gtest.h>

class FixedHashmapTests : public testing::Test
{
public:
protected:
    autocrat::fixed_hashmap<int, int> _map;
};

TEST_F(FixedHashmapTests, ShouldFindExistingItems)
{
    _map.emplace(1, 2);

    auto result = _map.find(1);

    EXPECT_EQ(1, result->first);
    EXPECT_EQ(2, result->second);
}

TEST_F(FixedHashmapTests, ShouldHandleHashCollisions)
{
    int collision = 1 + static_cast<int>(decltype(_map)::maximum_capacity);
    _map.emplace(1, 2);
    _map.emplace(collision, 3);

    auto result = _map.find(collision);

    EXPECT_EQ(collision, result->first);
    EXPECT_EQ(3, result->second);
}

TEST_F(FixedHashmapTests, ShouldNotAllowDuplicateKeys)
{
    _map.emplace(1, 2);
    _map.emplace(1, 3);

    auto result = _map.find(1);

    EXPECT_EQ(2, result->second);
}

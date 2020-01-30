#include "memory_pool.h"

#include <gtest/gtest.h>
#include "MemoryMonitor.h"

class NodePoolTests : public testing::Test
{
protected:
    autocrat::node_pool _pool;
};

TEST_F(NodePoolTests, AcquireShouldReuseNodes)
{
    auto first = _pool.acquire();
    first->next = first; // Need to check this is reset when the node is reused
    _pool.release(first);

    auto second = _pool.acquire();
    EXPECT_EQ(first, second);
    EXPECT_EQ(nullptr, second->next);
}

TEST_F(NodePoolTests, DestructorShouldReleaseAllTheNodes)
{
    std::size_t before_bytes = allocated_bytes();
    {
        autocrat::node_pool pool;
        EXPECT_NE(nullptr, pool.acquire());
        EXPECT_NE(nullptr, pool.acquire());
    }
    std::size_t after_bytes = allocated_bytes();
    EXPECT_EQ(before_bytes, after_bytes);
}

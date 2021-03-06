#include "array_pool.h"
#include <gtest/gtest.h>

class ArrayPoolTests : public testing::Test
{
public:
protected:
    autocrat::array_pool _pool;
};

TEST_F(ArrayPoolTests, ShouldReuseReleasedItems)
{
    autocrat::detail::array_pool_block* first;
    {
        autocrat::managed_byte_array_ptr ptr = _pool.aquire();
        first = ptr.get();
    }

    autocrat::detail::array_pool_block* second;
    {
        autocrat::managed_byte_array_ptr ptr = _pool.aquire();
        second = ptr.get();
    }
    
    EXPECT_EQ(first, second);
}

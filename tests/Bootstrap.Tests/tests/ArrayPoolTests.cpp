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
    autocrat::managed_byte_array& original = _pool.aquire();
    
    _pool.release(original);
    autocrat::managed_byte_array& result = _pool.aquire();

    EXPECT_EQ(&original, &result);
}

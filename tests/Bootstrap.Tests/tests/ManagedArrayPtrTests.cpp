#include "array_pool.h"
#include <gtest/gtest.h>

class ManagedArrayPtrTests : public testing::Test
{
public:
protected:
    autocrat::array_pool _pool;
};

TEST_F(ManagedArrayPtrTests, CopyAssignmentShouldBeEqualToTheOriginal)
{
    autocrat::managed_byte_array_ptr original = _pool.aquire();

    autocrat::managed_byte_array_ptr copy;
    copy = original;

    EXPECT_EQ(original.get(), copy.get());
}

TEST_F(ManagedArrayPtrTests, CopyConstructorShouldBeEqualToTheOriginal)
{
    autocrat::managed_byte_array_ptr original = _pool.aquire();

    autocrat::managed_byte_array_ptr copy(original);

    EXPECT_EQ(original.get(), copy.get());
}

TEST_F(ManagedArrayPtrTests, CopyShouldNotReturnTheArrayToThePool)
{
    autocrat::managed_byte_array_ptr original = _pool.aquire();

    {
        autocrat::managed_byte_array_ptr copy = original;
        EXPECT_EQ(1u, _pool.size());
    }

    EXPECT_EQ(1u, _pool.size());
}

TEST_F(ManagedArrayPtrTests, DestructorShouldReturnTheArrayToThePool)
{
    {
        autocrat::managed_byte_array_ptr ptr = _pool.aquire();
        EXPECT_EQ(1u, _pool.size());
    }

    EXPECT_EQ(0u, _pool.size());
}

TEST_F(ManagedArrayPtrTests, MoveAssignmentShouldClearTheOriginal)
{
    autocrat::managed_byte_array_ptr original = _pool.aquire();

    autocrat::managed_byte_array_ptr moved;
    moved = std::move(original);

    EXPECT_EQ(nullptr, original.get());
    EXPECT_NE(nullptr, moved.get());
}

TEST_F(ManagedArrayPtrTests, MoveConstructorShouldClearTheOriginal)
{
    autocrat::managed_byte_array_ptr original = _pool.aquire();

    autocrat::managed_byte_array_ptr moved(std::move(original));

    EXPECT_EQ(nullptr, original.get());
    EXPECT_NE(nullptr, moved.get());
}

TEST_F(ManagedArrayPtrTests, MoveShouldReturnTheArrayToThePool)
{
    autocrat::managed_byte_array_ptr original = _pool.aquire();

    {
        autocrat::managed_byte_array_ptr move = std::move(original);
        EXPECT_EQ(1u, _pool.size());
    }

    EXPECT_EQ(0u, _pool.size());
}

TEST_F(ManagedArrayPtrTests, ShouldCompareToFalseForEmptyPointers)
{
    autocrat::managed_byte_array_ptr instance(nullptr);

    EXPECT_FALSE(instance);
}

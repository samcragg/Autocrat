#include "collections.h"

#include <gtest/gtest.h>
#include "TestUtils.h"

class SmallVectorTests : public testing::Test
{
public:
protected:
    autocrat::small_vector<MovableItem> _vector;
};

TEST_F(SmallVectorTests, ShouldStoreSmallNumbersOfElements)
{
    MovableItem original(1);
    _vector.emplace_back(original);
    _vector.emplace_back(MovableItem(2));

    EXPECT_EQ(2u, _vector.size());

    int index = 0;
    for (auto& item : _vector)
    {
        ++index;
        EXPECT_EQ(index, item.value);
    }
}

TEST_F(SmallVectorTests, ShouldStoreLargeNumbersOfElements)
{
    for (int i = 1; i <= 10; ++i)
    {
        _vector.emplace_back(MovableItem(i));
    }

    EXPECT_EQ(10u, _vector.size());

    int index = 0;
    for (auto& item : _vector)
    {
        ++index;
        EXPECT_EQ(index, item.value);
    }
}

TEST_F(SmallVectorTests, ShouldRetainTheMemoryWhenCleared)
{
    for (int i = 1; i <= 10; ++i)
    {
        _vector.emplace_back(i);
    }

    // Prove that it moved from the fixed storage to the dynamic storage
    EXPECT_EQ(MoveType::Assignment, _vector.begin()->moved);

    _vector.clear();
    EXPECT_EQ(0u, _vector.size());

    // Adding the ten items now should put them in the same memory, so no need
    // to move them
    for (int i = 11; i <= 20; ++i)
    {
        _vector.emplace_back(i);
    }

    EXPECT_EQ(10u, _vector.size());
    for (auto& item : _vector)
    {
        EXPECT_GT(item.value, 10);
        EXPECT_EQ(MoveType::None, item.moved);
    }
}

TEST_F(SmallVectorTests, MoveAssignmentShouldMoveAssignTheElements)
{
    _vector.emplace_back(MovableItem(1));

    autocrat::small_vector<MovableItem> moved;
    moved = std::move(_vector);

    EXPECT_EQ(0u, _vector.size());
    EXPECT_EQ(1u, moved.size());
    EXPECT_EQ(1, moved.begin()->value);
    EXPECT_EQ(MoveType::Assignment, moved.begin()->moved);
}

TEST_F(SmallVectorTests, MoveConstructorShouldMoveConstructTheElements)
{
    _vector.emplace_back(MovableItem(1));

    autocrat::small_vector<MovableItem> moved(std::move(_vector));

    EXPECT_EQ(0u, _vector.size());
    EXPECT_EQ(1u, moved.size());
    EXPECT_EQ(1, moved.begin()->value);
    EXPECT_EQ(MoveType::Constructor, moved.begin()->moved);
}

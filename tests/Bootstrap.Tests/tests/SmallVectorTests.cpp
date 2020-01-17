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
    _vector.push_back(original);
    _vector.push_back(MovableItem(2));

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
        _vector.push_back(MovableItem(i));
    }

    EXPECT_EQ(10u, _vector.size());

    int index = 0;
    for (auto& item : _vector)
    {
        ++index;
        EXPECT_EQ(index, item.value);
    }
}

TEST_F(SmallVectorTests, MoveAssignmentShouldMoveAssignTheElements)
{
    _vector.push_back(MovableItem(1));

    autocrat::small_vector<MovableItem> moved;
    moved = std::move(_vector);

    EXPECT_EQ(0u, _vector.size());
    EXPECT_EQ(1u, moved.size());
    EXPECT_EQ(1, moved.begin()->value);
    EXPECT_EQ(MoveType::Assignment, moved.begin()->moved);
}

TEST_F(SmallVectorTests, MoveConstructorShouldMoveConstructTheElements)
{
    _vector.push_back(MovableItem(1));

    autocrat::small_vector<MovableItem> moved(std::move(_vector));

    EXPECT_EQ(0u, _vector.size());
    EXPECT_EQ(1u, moved.size());
    EXPECT_EQ(1, moved.begin()->value);
    EXPECT_EQ(MoveType::Constructor, moved.begin()->moved);
}

#include "collections.h"
#include <gtest/gtest.h>

enum class MoveType
{
    None,
    Constructor,
    Assignment,
};

struct MovableItem
{
    explicit MovableItem(int v = 0) : value(v), moved(MoveType::None)
    {
    }

    ~MovableItem() noexcept
    {
        value = -1;
    }

    MovableItem(const MovableItem&) = default;

    MovableItem(MovableItem&& other) noexcept
    {
        value = other.value;
        moved = MoveType::Constructor;
    }

    MovableItem& operator=(const MovableItem&) = default;

    MovableItem& operator=(MovableItem&& other) noexcept
    {
        value = other.value;
        moved = MoveType::Assignment;
        return *this;
    }

    int value;
    MoveType moved;
};


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

    EXPECT_EQ(2, _vector.size());

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

    EXPECT_EQ(10, _vector.size());

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

    EXPECT_EQ(0, _vector.size());
    EXPECT_EQ(1, moved.size());
    EXPECT_EQ(1, moved.begin()->value);
    EXPECT_EQ(MoveType::Assignment, moved.begin()->moved);
}

TEST_F(SmallVectorTests, MoveConstructorShouldMoveConstructTheElements)
{
    _vector.push_back(MovableItem(1));

    autocrat::small_vector<MovableItem> moved(std::move(_vector));

    EXPECT_EQ(0, _vector.size());
    EXPECT_EQ(1, moved.size());
    EXPECT_EQ(1, moved.begin()->value);
    EXPECT_EQ(MoveType::Constructor, moved.begin()->moved);
}

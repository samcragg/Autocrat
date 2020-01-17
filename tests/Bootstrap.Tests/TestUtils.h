#ifndef TEST_UTILS_H
#define TEST_UTILS_H

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

#endif

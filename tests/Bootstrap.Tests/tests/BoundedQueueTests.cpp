#include "collections.h"
#include <gtest/gtest.h>

struct QueueItem
{
    explicit QueueItem(int v = 0) : value(v)
    {
    }

    ~QueueItem() noexcept
    {
        value = -1;
    }

    QueueItem(QueueItem&& other) noexcept
    {
        value = other.value;
        other.value = 0;
    }

    QueueItem& operator=(QueueItem&& other) noexcept
    {
        value = other.value;
        other.value = 0;
        return *this;
    }

    int value;
};


class BoundedQueueTests : public testing::Test
{
public:
protected:
    static constexpr std::size_t ArraySize = 8;
    autocrat::bounded_queue<QueueItem, ArraySize> _queue;
};

TEST_F(BoundedQueueTests, PopShouldReturnFalseWhenEmpty)
{
    QueueItem item(1);

    bool result = _queue.pop(&item);

    EXPECT_FALSE(result);
    EXPECT_EQ(1, item.value);
}

TEST_F(BoundedQueueTests, ShouldReturnTheItemsInOrder)
{
    _queue.emplace(QueueItem(1));
    _queue.emplace(QueueItem(2));
    _queue.emplace(QueueItem(3));

    QueueItem item;
    EXPECT_TRUE(_queue.pop(&item));
    EXPECT_EQ(1, item.value);

    EXPECT_TRUE(_queue.pop(&item));
    EXPECT_EQ(2, item.value);

    EXPECT_TRUE(_queue.pop(&item));
    EXPECT_EQ(3, item.value);
}

TEST_F(BoundedQueueTests, ShouldThrowWhenFull)
{
    for (unsigned i = 0; i != ArraySize; ++i)
    {
        _queue.emplace();
    }

    EXPECT_THROW(_queue.emplace(), std::bad_alloc);
}

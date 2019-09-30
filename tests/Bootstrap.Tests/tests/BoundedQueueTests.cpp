#include "collections.h"
#include <future>
#include <gtest/gtest.h>

using namespace std::chrono_literals;

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

TEST_F(BoundedQueueTests, ShouldNotWaitForNewDataIfTheFlagIsFalse)
{
    auto pop_result = std::async(std::launch::async, [this] { return _queue.pop(false); });

    std::future_status status = pop_result.wait_for(1ms);

    EXPECT_EQ(std::future_status::ready, status);
}


TEST_F(BoundedQueueTests, ShouldReturnTheItemsInOrder)
{
    _queue.emplace(QueueItem(1));
    _queue.emplace(QueueItem(2));
    _queue.emplace(QueueItem(3));

    EXPECT_EQ(1, _queue.pop(false).value);
    EXPECT_EQ(2, _queue.pop(false).value);
    EXPECT_EQ(3, _queue.pop(false).value);
}

TEST_F(BoundedQueueTests, ShouldThrowWhenFull)
{
    for (unsigned i = 0; i != ArraySize; ++i)
    {
        _queue.emplace();
    }

    EXPECT_THROW(_queue.emplace(), std::bad_alloc);
}

TEST_F(BoundedQueueTests, ShouldWaitForNewDataIfTheFlagIsTrue)
{
    auto pop_result = std::async(std::launch::async, [this] { return _queue.pop(true); });

    std::future_status status = pop_result.wait_for(1ms);
    EXPECT_EQ(std::future_status::timeout, status);

    _queue.emplace();
    status = pop_result.wait_for(1ms);
    EXPECT_EQ(std::future_status::ready, status);
}

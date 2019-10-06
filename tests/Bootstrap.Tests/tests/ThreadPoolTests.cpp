#include "thread_pool.h"
#include <chrono>
#include <future>
#include <memory>
#include <thread>
#include <gtest/gtest.h>

using namespace std::chrono_literals;

class ThreadPoolTests : public testing::Test
{
public:
    ThreadPoolTests() :
        _pool(0, 2)
    {
    }

    ~ThreadPoolTests()
    {
    }
protected:
    autocrat::thread_pool _pool;
};

namespace
{
    using promise_thread_id = std::promise<std::thread::id>;

    void SetThreadId(std::any& arg)
    {
        auto thread_id = std::any_cast<std::shared_ptr<promise_thread_id>>(arg);
        thread_id->set_value(std::this_thread::get_id());
    }
}

TEST_F(ThreadPoolTests, ShouldPerformTheWorkOnASeparateThread)
{
    auto worker_promise = std::make_shared<promise_thread_id>();
    auto worker_future = worker_promise->get_future();

    _pool.enqueue(&SetThreadId, worker_promise);
    std::future_status wait_result = worker_future.wait_for(20ms);

    ASSERT_EQ(std::future_status::ready, wait_result);
    EXPECT_NE(std::this_thread::get_id(), worker_future.get());
}

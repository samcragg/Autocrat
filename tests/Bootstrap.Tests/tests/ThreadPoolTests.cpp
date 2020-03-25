#include "thread_pool.h"
#include <chrono>
#include <condition_variable>
#include <future>
#include <memory>
#include <mutex>
#include <thread>
#include <tuple>
#include <gtest/gtest.h>
#include <cpp_mock.h>

using namespace std::chrono_literals;

namespace
{
    std::size_t initialized_called_count;

    void initialize_method()
    {
        initialized_called_count++;
    }
}

class MockLifetimeService : public autocrat::lifetime_service
{
public:
    MockMethod(void, on_begin_work, (std::size_t))
    MockMethod(void, on_end_work, (std::size_t))
};

class ThreadPoolTests : public testing::Test
{
public:
    static constexpr std::size_t thread_count = 2;

    ThreadPoolTests() :
        _pool(0, thread_count)
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
    using lifetime_condition_tuple = std::tuple<MockLifetimeService*, std::condition_variable*>;
    using promise_thread_id = std::promise<std::thread::id>;
    std::mutex condition_mutex;

    void CheckLifetimeService(std::any& arg)
    {
        auto [service, condition] = std::any_cast<lifetime_condition_tuple>(arg);
        Verify(service->on_begin_work).Times(1);
        Verify(service->on_end_work).Times(0);

        std::unique_lock<std::mutex> lock(condition_mutex);
        condition->notify_all();
    }

    void SetThreadId(std::any& arg)
    {
        auto thread_id = std::any_cast<std::shared_ptr<promise_thread_id>>(arg);
        thread_id->set_value(std::this_thread::get_id());
    }
}

TEST_F(ThreadPoolTests, ShouldCallLifetimeServiceBeforeAndAfterWork)
{
    MockLifetimeService service;
    std::condition_variable condition;
    std::unique_lock<std::mutex> lock(condition_mutex);

    _pool.add_observer(&service);
    _pool.enqueue(&CheckLifetimeService, std::make_tuple(&service, &condition));
    _pool.start(&initialize_method);
    auto result = condition.wait_for(lock, 20ms);

    EXPECT_EQ(std::cv_status::no_timeout, result);

    // At this point we know the other work item has been executed, however, we
    // don't know that it has completed fully (i.e. all we know is it has
    // notified the condition_variable, it could still be running the rest of
    // the code). Just give it a tiny bit of time to know it's finished.
    std::this_thread::sleep_for(10ms);
    Verify(service.on_end_work).Times(1);
}

TEST_F(ThreadPoolTests, ShouldPerformTheWorkOnASeparateThread)
{
    auto worker_promise = std::make_shared<promise_thread_id>();
    auto worker_future = worker_promise->get_future();

    _pool.enqueue(&SetThreadId, worker_promise);
    _pool.start(&initialize_method);
    std::future_status wait_result = worker_future.wait_for(20ms);

    ASSERT_EQ(std::future_status::ready, wait_result);
    EXPECT_NE(std::this_thread::get_id(), worker_future.get());
}

TEST_F(ThreadPoolTests, StartShouldInitializeManagedThreads)
{
    initialized_called_count = 0;

    _pool.start(&initialize_method);

    EXPECT_EQ(initialized_called_count, initialized_called_count);
}

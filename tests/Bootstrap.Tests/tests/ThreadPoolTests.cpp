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

class MockLifetimeService : public autocrat::lifetime_service
{
public:
    MockMethod(void, begin_work, (std::size_t))
    MockMethod(void, end_work, (std::size_t))
    MockMethod(void, pool_created, (std::size_t))
};

class ThreadPoolTests : public testing::Test
{
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
        Verify(service->begin_work).Times(1);
        Verify(service->end_work).Times(0);

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
    _pool.start(0, 1, [](std::size_t) {});
    auto result = condition.wait_for(lock, 20ms);

    EXPECT_EQ(std::cv_status::no_timeout, result);

    // At this point we know the other work item has been executed, however, we
    // don't know that it has completed fully (i.e. all we know is it has
    // notified the condition_variable, it could still be running the rest of
    // the code). Just give it a tiny bit of time to know it's finished.
    std::this_thread::sleep_for(10ms);
    Verify(service.end_work).Times(1);
}

TEST_F(ThreadPoolTests, ShouldCallLifetimeServiceInOrder)
{
    std::atomic_int order = 0;
    MockLifetimeService service1;
    MockLifetimeService service2;

    // Check that the end is called in reverse order to begin
    When(service1.begin_work).Do([&](std::size_t) { EXPECT_EQ(0, order); order++; });
    When(service2.begin_work).Do([&](std::size_t) { EXPECT_EQ(1, order); order++; });
    When(service2.end_work).Do([&](std::size_t) { EXPECT_EQ(2, order); order++; });
    When(service1.end_work).Do([&](std::size_t) { EXPECT_EQ(3, order); order++; });

    _pool.add_observer(&service1);
    _pool.add_observer(&service2);

    _pool.enqueue([](std::any&) {}, 0);
    _pool.start(0, 1, [](std::size_t) {});

    while (order != 4)
    {
        std::this_thread::yield();
    }
}

TEST_F(ThreadPoolTests, ShouldCallPoolCreatedWithThreadCount)
{
    MockLifetimeService service;
    _pool.add_observer(&service);

    _pool.start(-1, 3, [](std::size_t) {});

    Verify(service.pool_created).With(3u);
}
TEST_F(ThreadPoolTests, ShouldPerformTheWorkOnASeparateThread)
{
    auto worker_promise = std::make_shared<promise_thread_id>();
    auto worker_future = worker_promise->get_future();

    _pool.enqueue(&SetThreadId, worker_promise);
    _pool.start(0, 1, [](std::size_t) {});
    std::future_status wait_result = worker_future.wait_for(20ms);

    ASSERT_EQ(std::future_status::ready, wait_result);
    EXPECT_NE(std::this_thread::get_id(), worker_future.get());
}

TEST_F(ThreadPoolTests, StartShouldInitializeEachThreadPoolThread)
{
    std::size_t initialized_called_count = 0;

    _pool.start(0, 2, [&](std::size_t) { initialized_called_count++; });

    EXPECT_EQ(2, initialized_called_count);
}

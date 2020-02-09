#include "timer_service.h"

#include <chrono>
#include <gtest/gtest.h>
#include <cpp_mock.h>
#include "TestMocks.h"
#include "pal.h"
#include "pal_mock.h"

using namespace std::chrono_literals;

class MockPalService : public pal_service
{
public:
    MockMethod(std::chrono::microseconds, current_time, ())
};

namespace
{
    std::function<void(std::int32_t)> on_timer_callback;

    void timer_callback(std::int32_t handle)
    {
        on_timer_callback(handle);
    }
}

class TimerServiceTests : public testing::Test
{
protected:
    TimerServiceTests() :
        _service(&_thread_pool)
    {
        active_service_mock = &_pal;
    }

    ~TimerServiceTests()
    {
        active_service_mock = nullptr;
        on_timer_callback = nullptr;
    }

    autocrat::timer_service _service;
    MockPalService _pal;
    MockThreadPool _thread_pool;
};

TEST_F(TimerServiceTests, ShouldInvokeTheCallbackAfterTheInitialDelay)
{
    When(_pal.current_time).Return({ 0us, 5us, 10us });
    bool timer_called = false;
    on_timer_callback = [&](auto) { timer_called = true; };

    _service.add_timer_callback(10us, 0us, &timer_callback);

    _service.check_and_dispatch();
    EXPECT_FALSE(timer_called);

    _service.check_and_dispatch();
    EXPECT_TRUE(timer_called);
}

TEST_F(TimerServiceTests, ShouldInvokeTheCallbackAfterTheRepeat)
{
    // Add an initial 0 for when we add it to the service
    When(_pal.current_time).Return({ 0us, 0us, 5us, 10us, 15us, 20us });
    int timer_called_count = 0;
    on_timer_callback = [&](auto) { timer_called_count++; };

    _service.add_timer_callback(0us, 10us, &timer_callback);

    // 0
    _service.check_and_dispatch();
    EXPECT_EQ(1, timer_called_count);


    // 5
    _service.check_and_dispatch();
    EXPECT_EQ(1, timer_called_count);

    // 10
    _service.check_and_dispatch();
    EXPECT_EQ(2, timer_called_count);

    // 15
    _service.check_and_dispatch();
    EXPECT_EQ(2, timer_called_count);

    // 20
    _service.check_and_dispatch();
    EXPECT_EQ(3, timer_called_count);
}

TEST_F(TimerServiceTests, ShouldInvokeTheCallbackWithTheUniqueHandle)
{
    When(_pal.current_time).Return({ 0us, 0us, 3us, 5us });
    std::uint32_t called_handle = 0u;
    on_timer_callback = [&](std::uint32_t handle) { called_handle = static_cast<std::uint32_t>(handle); };

    std::uint32_t five_handle = _service.add_timer_callback(0us, 5us, &timer_callback);
    std::uint32_t three_handle = _service.add_timer_callback(0us, 3us, &timer_callback);

    _service.check_and_dispatch();
    EXPECT_EQ(three_handle, called_handle);

    _service.check_and_dispatch();
    EXPECT_EQ(five_handle, called_handle);
}

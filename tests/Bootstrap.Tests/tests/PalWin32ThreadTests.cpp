#define PAL_MOCK_H
#include "pal.h"
#include "pal_win32.h"
#include <chrono>
#include <functional>
#include <mutex>
#include <thread>
#include <gtest/gtest.h>

using std::chrono::high_resolution_clock;
using namespace std::chrono_literals;

class PalWin32ThreadTests : public testing::Test
{
public:
protected:
    std::thread run_in_background(std::function<void(std::thread&)> setup, std::function<void()> background)
    {
        // Grab the lock before the background thread is created to ensure it
        // doesn't run the code before we've had chance to execute the callback
        std::shared_ptr<std::mutex> mutex = std::make_shared<std::mutex>();
        mutex->lock();

        std::thread background_thread([=]
            {
                // Wait for the setup to complete
                mutex->lock();
                background();
                mutex->unlock();
            });

        setup(background_thread);
        mutex->unlock();
        return background_thread;
    }
};

TEST_F(PalWin32ThreadTests, ShouldSetTheThreadAffinity)
{
    std::size_t cpu = 0;
    std::thread thread = run_in_background(
        [](std::thread& thread) { pal::set_affinity(thread, 1); },
        [&] { cpu = pal::get_current_processor(); });

    thread.join();

    EXPECT_EQ(1, cpu);
}

TEST_F(PalWin32ThreadTests, ShouldWakeWaitingThreads)
{
    high_resolution_clock::time_point after_wait = {};
    high_resolution_clock::time_point before_wake = {};
    std::uint32_t handle = {};

    std::thread thread = run_in_background(
        [](std::thread&) {},
        [&]
        {
            pal::wait_on(&handle);
            after_wait = high_resolution_clock::now();
        });

    std::this_thread::sleep_for(10ms);
    before_wake = high_resolution_clock::now();
    pal::wake_all(&handle);
    thread.join();

    EXPECT_LT(before_wake, after_wait);
}

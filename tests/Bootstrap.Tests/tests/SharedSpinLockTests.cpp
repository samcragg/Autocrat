#include "locks.h"

#include <array>
#include <atomic>
#include <chrono>
#include <mutex>
#include <shared_mutex>
#include <thread>
#include <gtest/gtest.h>

using namespace std::chrono_literals;

namespace
{
    template <typename Pred>
    void wait_for(Pred predicate)
    {
        auto maximum_wait = std::chrono::steady_clock::now() + 20ms;
        if (!predicate())
        {
            ASSERT_LT(std::chrono::steady_clock::now(), maximum_wait);
            std::this_thread::yield();
        }
    }
}

class SharedSpinLockTests : public testing::Test
{
protected:
    autocrat::shared_spin_lock _lock;
    using shared_lock = std::shared_lock<decltype(_lock)>;
    using unique_lock = std::unique_lock<decltype(_lock)>;
};

TEST_F(SharedSpinLockTests, ShouldAllowMultipleSharedLocks)
{
    std::atomic_uint32_t locked_readers = 0;
    std::atomic_bool keep_lock = true;

    std::array<std::thread, 16u> reader_threads;
    for (auto& thread : reader_threads)
    {
        thread = std::thread([&]()
            {
                shared_lock lock(_lock);
                ++locked_readers;
                wait_for([&]() { return !keep_lock; });
            });
    }

    wait_for([&]() { return locked_readers == reader_threads.size(); });
    keep_lock = false;

    for (auto& thread : reader_threads)
    {
        thread.join();
    }
}

TEST_F(SharedSpinLockTests, ShouldOnlyAllowSingleUniqueLocks)
{
    std::atomic_uint32_t waiting_writers = 0;
    std::atomic_uint32_t locked_writers = 0;

    std::array<std::thread, 16u> writer_threads;
    for (auto& thread : writer_threads)
    {
        thread = std::thread([&]()
            {
                ++waiting_writers;
                wait_for([&]() { return waiting_writers == writer_threads.size(); });

                unique_lock lock(_lock);
                ++locked_writers;
                std::this_thread::yield();
                EXPECT_EQ(1u, locked_writers);
                --locked_writers;
            });
    }

    for (auto& thread : writer_threads)
    {
        thread.join();
    }
}

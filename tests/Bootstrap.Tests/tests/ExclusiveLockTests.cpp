#include "locks.h"

#include <thread>
#include <gtest/gtest.h>

class ExclusiveLockTests : public testing::Test
{
protected:
    autocrat::exclusive_lock _lock;

    void AssertIsLocked(bool isLocked)
    {
        std::thread thread([&]()
            {
                EXPECT_EQ(!isLocked, _lock.try_lock());
            });
        thread.join();
    }
};

TEST_F(ExclusiveLockTests, TryLockShouldReturnFalseForDifferentThreads)
{
    EXPECT_TRUE(_lock.try_lock());

    AssertIsLocked(true);
}

TEST_F(ExclusiveLockTests, TryLockShouldReturnTrueForTheSameThread)
{
    EXPECT_TRUE(_lock.try_lock());
    EXPECT_TRUE(_lock.try_lock());
}

TEST_F(ExclusiveLockTests, UnlockShouldReleaseTheLockOnLastCall)
{
    _lock.try_lock();
    _lock.try_lock();

    _lock.unlock();
    AssertIsLocked(true);

    _lock.unlock();
    AssertIsLocked(false);
}

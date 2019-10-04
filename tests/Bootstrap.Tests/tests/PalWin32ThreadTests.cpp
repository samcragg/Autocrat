#include "pal.h"
#include "pal_win32.h"
#include <any>
#include <condition_variable>
#include <mutex>
#include <gtest/gtest.h>

class PalWin32ThreadTests : public testing::Test
{
public:
protected:
    std::condition_variable _cv;
    std::any _data;
    std::mutex _mutex;
};

TEST_F(PalWin32ThreadTests, ShouldSetTheThreadAffinity)
{
    // Grab the lock before the background thread is started
    std::unique_lock<std::mutex> lock(_mutex);

    std::thread background([this]
        {
            std::unique_lock<std::mutex> lock(_mutex);
            _data = pal::get_current_processor();

            lock.unlock();
            _cv.notify_one();
        });

    pal::set_affinity(background, 1);
    _cv.wait(lock);

    EXPECT_EQ(1, std::any_cast<std::size_t>(_data));
    background.detach();
}

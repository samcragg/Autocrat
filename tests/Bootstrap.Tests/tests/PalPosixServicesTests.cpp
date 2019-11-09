#define PAL_MOCK_H
#include "pal.h"
#include "pal_posix.h"

#include <atomic>
#include <chrono>
#include <thread>
#include <gtest/gtest.h>
#include <signal.h>

using namespace std::chrono_literals;
using std::chrono::system_clock;

class PalPosixServicesTests : public testing::Test
{
protected:
};

using PalPosixServicesDeathTest = PalPosixServicesTests;

namespace
{
    std::atomic_bool is_running;

    void signal_handler()
    {
        is_running = false;
    }
}

TEST_F(PalPosixServicesDeathTest, ShouldHandleConsoleCloseSignals)
{
    EXPECT_EXIT(
        {
            is_running = true;
            pal::set_close_signal_handler(&signal_handler);

            raise(SIGINT);
            auto end = system_clock::now() + 1s;
            while (is_running)
            {
                std::this_thread::yield();
                if (system_clock::now() > end)
                {
                    std::exit(-1);
                }
            }

            std::exit(1);
        },
        ::testing::ExitedWithCode(1),
        "");
}

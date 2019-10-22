#define PAL_MOCK_H
#include "pal.h"
#include "pal_win32.h"

#include <atomic>
#include <chrono>
#include <thread>
#include <gtest/gtest.h>

using namespace std::chrono_literals;
using std::chrono::system_clock;

class PalWin32ServicesTests : public testing::Test
{
protected:
};

using PalWin32ServicesDeathTest = PalWin32ServicesTests;

namespace
{
    std::atomic_bool is_running;

    void signal_handler()
    {
        is_running = false;
    }
}

TEST_F(PalWin32ServicesDeathTest, ShouldHandleConsoleCloseSignals)
{
    // Disable ctrl-c handling for our process (the child process inherits our
    // console and this setting, so when we fire a ctrl-c signal to the console
    // window we'd also receive it)
    SetConsoleCtrlHandler(nullptr, TRUE);
    EXPECT_EXIT(
        {
            // Restore ctrl-c handling now we're in a separate process
            SetConsoleCtrlHandler(nullptr, FALSE);

            is_running = true;
            pal::set_close_signal_handler(&signal_handler);

            GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
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

    SetConsoleCtrlHandler(nullptr, FALSE);
}

#undef UNIT_TESTS
#include "pal.h"

#include <atomic>
#include <chrono>
#include <thread>
#include <gtest/gtest.h>

#if defined(__linux__)
#include <signal.h>
#endif

using namespace std::chrono_literals;
using std::chrono::system_clock;

class PalServicesTests : public testing::Test
{
protected:
};

using PalServicesDeathTest = PalServicesTests;

namespace
{
    std::atomic_bool is_running;

    void enable_automatic_ctrl_c_handling(bool enabled)
    {
#if defined(_WIN32)
        SetConsoleCtrlHandler(nullptr, !enabled);
#else
        ((void)enabled);
#endif
    }

    void raise_ctrl_c()
    {
#if defined(_WIN32)
        GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
#else
        raise(SIGINT);
#endif
    }

    void signal_handler()
    {
        is_running = false;
    }
}

TEST_F(PalServicesDeathTest, ShouldHandleConsoleCloseSignals)
{
    // Disable ctrl-c handling for our process (the child process inherits our
    // console and this setting, so when we fire a ctrl-c signal to the console
    // window we'd also receive it)
    enable_automatic_ctrl_c_handling(false);

    EXPECT_EXIT(
        {
            // Restore ctrl-c handling now we're in a separate process
            enable_automatic_ctrl_c_handling(true);

            is_running = true;
            pal::set_close_signal_handler(&signal_handler);

            raise_ctrl_c();
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

    enable_automatic_ctrl_c_handling(false);
}

#undef UNIT_TESTS
#include "pal.h"
#include "PalTests.h"

#include <atomic>
#include <chrono>
#include <cstdlib>
#include <filesystem>
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
    std::string ctrl_c_argument = "ctrl_c_test_child_process";

#if defined(_WIN32)
    std::string get_current_process()
    {
        char executable_path[_MAX_PATH + 1] = {};
        GetModuleFileName(
            nullptr,
            executable_path,
            _MAX_PATH);
        return executable_path;
    }

    void raise_ctrl_c()
    {
        GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
    }

    int run_process(const std::string& name, const std::string& arg)
    {
        STARTUPINFO si = {};
        si.cb = sizeof(si);

        PROCESS_INFORMATION pi = {};
        std::string cmd = name + " " + arg;
        CreateProcess(
            nullptr,
            const_cast<char*>(cmd.c_str()),
            nullptr,
            nullptr,
            FALSE,
            CREATE_NEW_CONSOLE,
            nullptr,
            nullptr,
            &si,
            &pi);
        WaitForSingleObject(pi.hProcess, INFINITE);
        DWORD exit_code;
        GetExitCodeProcess(pi.hProcess, &exit_code);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        return static_cast<int>(exit_code);
    }

#else
    std::string get_current_process()
    {
        return std::filesystem::read_symlink("/proc/self/exe").string();
    }

    void raise_ctrl_c()
    {
        raise(SIGINT);
    }

    int run_process(const std::string& name, const std::string& arg)
    {
        std::string cmd = name + " " + arg;
        return WEXITSTATUS(std::system(cmd.c_str()));
    }
#endif

    void signal_handler()
    {
        is_running = false;
    }
}

bool is_ctrl_c_test(const char* argument)
{
    return ctrl_c_argument == argument;
}

int run_ctrl_c_test()
{
    is_running = true;
    pal::set_close_signal_handler(&signal_handler);

    raise_ctrl_c();
    auto end = system_clock::now() + 1s;
    while (is_running)
    {
        std::this_thread::yield();
        if (system_clock::now() > end)
        {
            return -123;
        }
    }

    return 123;
}

TEST_F(PalServicesTests, ShouldGetTheCurrentTimeStamp)
{
    std::chrono::microseconds start = pal::get_current_time();
    std::this_thread::yield();
    std::chrono::microseconds end = pal::get_current_time();

    EXPECT_NE(start, end);
    EXPECT_GT(1000, (end - start).count());
}

TEST_F(PalServicesDeathTest, ShouldHandleConsoleCloseSignals)
{
    int result = run_process(get_current_process(), ctrl_c_argument);

    EXPECT_EQ(123, result);
}

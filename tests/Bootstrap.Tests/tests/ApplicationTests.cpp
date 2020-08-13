#include "application.h"
#include "mock_services.h"
#include <chrono>
#include <thread>
#include <gtest/gtest.h>
#include <cpp_mock.h>

using namespace std::chrono_literals;

namespace
{
    std::size_t initialize_managed_thread_call_count;
    std::size_t load_configuration_call_count;
    void* load_configuration_source;
    std::size_t on_configuration_loaded_call_count;
    std::size_t register_worker_types_call_count;

    extern "C" void CDECL InitializeManagedThread()
    {
        initialize_managed_thread_call_count++;
    }

    extern "C" bool CDECL LoadConfiguration(void* source)
    {
        load_configuration_call_count++;
        load_configuration_source = source;
        return true;
    }

    extern "C" void CDECL OnConfigurationLoaded()
    {
        on_configuration_loaded_call_count++;
    }

    extern "C" void CDECL RegisterWorkerTypes()
    {
        register_worker_types_call_count++;
    }

}

class ApplicationTests : public testing::Test
{
protected:
    autocrat::application _application;
};

TEST_F(ApplicationTests, InitializeShouldInitializeTheGlobalServices)
{
    _application.initialize();

    Verify(mock_global_services.initialize);
}

TEST_F(ApplicationTests, InitializeShouldInitializeTheManagedThreads)
{
    When(mock_global_services.thread_pool().start)
        .Do([](auto initialize)
            {
                initialize(0);
            });
    initialize_managed_thread_call_count = 0;

    _application.initialize();

    // Should initialize the global thread and the thread pool threads (which
    // we invoke once in the above lambda)
    EXPECT_EQ(2u, initialize_managed_thread_call_count);
}

TEST_F(ApplicationTests, InitializeShouldRegisterTheWorkerTypes)
{
    register_worker_types_call_count = 0;

    _application.initialize();

    EXPECT_EQ(1u, register_worker_types_call_count);
}

TEST_F(ApplicationTests, RunShouldDispatchWorkUntilStopIsCalled)
{
    std::thread stop_after_10ms([this]()
        {
            std::this_thread::sleep_for(10ms);
            _application.stop();
        });

    auto start = std::chrono::steady_clock::now();
    _application.run();
    auto end = std::chrono::steady_clock::now();

    EXPECT_GE(end - start, 10ms);
    Verify(mock_global_services.check_and_dispatch);

    stop_after_10ms.join();
}

#include "application.h"
#include "ManagedObjects.h"
#include "mock_services.h"
#include <chrono>
#include <thread>
#include <fstream>
#include <functional>
#include <gtest/gtest.h>
#include <cpp_mock.h>

using namespace std::chrono_literals;

namespace
{
    std::size_t initialize_managed_thread_call_count;
    std::size_t load_configuration_call_count;
    std::size_t on_configuration_loaded_call_count;
    std::size_t register_worker_types_call_count;
    std::function<bool(void*)> WhenLoadConfiguration;

    extern "C" void CDECL InitializeManagedThread()
    {
        initialize_managed_thread_call_count++;
    }

    extern "C" bool CDECL LoadConfiguration(void* source)
    {
        load_configuration_call_count++;
        return WhenLoadConfiguration(source);
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

class ConfigFile
{
public:
    explicit ConfigFile(const char* text) :
        _path(autocrat::get_config_file())
    {
        std::ofstream file(_path);
        file << text;
    }

    ~ConfigFile()
    {
        std::filesystem::remove(_path);
    }
private:
    std::filesystem::path _path;
};

class ApplicationTests : public testing::Test
{
protected:
    ApplicationTests()
    {
        WhenLoadConfiguration = [](void*) { return true; };
    }

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

TEST_F(ApplicationTests, InitializeShouldLoadTheConfigFile)
{
    auto config = ConfigFile("123456");
    load_configuration_call_count = 0;
    WhenLoadConfiguration = [](void* source)
    {
        auto array = static_cast<Array*>(source);
        EXPECT_EQ(6u, array->m_Length);
        EXPECT_EQ("123456", std::string(static_cast<char*>(source) + sizeof(Array), 6u));
        return true;
    };

    _application.initialize();

    EXPECT_EQ(1u, load_configuration_call_count);
}

TEST_F(ApplicationTests, InitializeShouldRegisterTheWorkerTypes)
{
    register_worker_types_call_count = 0;

    _application.initialize();

    EXPECT_EQ(1u, register_worker_types_call_count);
}

TEST_F(ApplicationTests, InitializeShouldThrowIfLoadingTheConfigFails)
{
    auto config = ConfigFile("");
    WhenLoadConfiguration = [](void*) {return false; };

    EXPECT_THROW(_application.initialize(), std::runtime_error);
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

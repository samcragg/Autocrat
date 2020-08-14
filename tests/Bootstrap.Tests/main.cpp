#include "ConsoleTestPrinter.h"
#include "mock_services.h"
#include "tests/PalTests.h"
#include <gtest/gtest.h>
#include <spdlog/sinks/null_sink.h>
#include <spdlog/spdlog.h>

mock_services mock_global_services;

namespace autocrat
{
    global_services_type& global_services = mock_global_services;
}

class Environment : public testing::Environment
{
public:
    void SetUp() override
    {
        mock_global_services.create_services();
    }

    void TearDown() override
    {
        mock_global_services.release_services();
    }
};

int main(int argc, char** argv)
{
    if ((argc == 2) && is_ctrl_c_test(argv[1]))
    {
        return run_ctrl_c_test();
    }
    else
    {
        spdlog::set_default_logger(spdlog::create<spdlog::sinks::null_sink_st>("disable_logging"));

        testing::InitGoogleTest(&argc, argv);
        testing::AddGlobalTestEnvironment(new Environment());
        testing::TestEventListeners& listeners = testing::UnitTest::GetInstance()->listeners();
        delete listeners.Release(listeners.default_result_printer());
        listeners.Append(new ConsoleTestPrinter());
        return RUN_ALL_TESTS();
    }
}

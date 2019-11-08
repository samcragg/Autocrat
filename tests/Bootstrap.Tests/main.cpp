#include <gtest/gtest.h>
#include "mock_services.h"

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
    testing::InitGoogleTest(&argc, argv);
    testing::AddGlobalTestEnvironment(new Environment());
    return RUN_ALL_TESTS();
}

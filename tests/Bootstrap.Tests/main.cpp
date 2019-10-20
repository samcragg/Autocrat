#include <gtest/gtest.h>
#include "mock_services.h"

mock_services mock_global_services;

namespace autocrat
{
    global_services_type& global_services = mock_global_services;
}

int main(int argc, char** argv)
{
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}

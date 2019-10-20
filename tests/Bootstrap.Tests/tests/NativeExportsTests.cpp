#include "native_exports.h"

#include <gmock/gmock.h>
#include <gtest/gtest.h>
#include "mock_method_handles.h"
#include "mock_services.h"

class NativeExportsTests : public testing::Test
{
protected:
};

TEST_F(NativeExportsTests, RegisterUdpDataReceivedShouldAddTheMethodHandle)
{
    udp_register_method method = [](std::int32_t, void*) {};
    method_registration registration = method_registration::register_method(method);

    EXPECT_CALL(mock_global_services.network_service(), add_udp_callback(123, method));

    register_udp_data_received(123, registration.index());
}

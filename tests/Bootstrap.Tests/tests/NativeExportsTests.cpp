#include "native_exports.h"

#include <chrono>
#include <gtest/gtest.h>
#include <cpp_mock.h>
#include "mock_method_handles.h"
#include "mock_services.h"

struct typed_reference
{
    void* value;
    void* type;
};

using namespace std::chrono_literals;

class NativeExportsTests : public testing::Test
{
protected:
};

TEST_F(NativeExportsTests, LoadObjectShouldReturnTheValue)
{
    int type = 0;
    int worker = 0;
    When(mock_global_services.worker_service().get_worker)
        .With(&type)
        .Return(&worker);

    void* result;
    typed_reference tr = {};
    tr.value = &result;

    load_object(&type, &tr);

    EXPECT_EQ(&worker, result);
}

TEST_F(NativeExportsTests, RegisterConstructorShouldAddTheMethodHandle)
{
    construct_worker method = []() -> void* { return nullptr; };
    method_registration registration = method_registration::register_method(method);
    int type = 0;

    register_constructor(&type, registration.index());

    Verify(mock_global_services.worker_service().register_type)
        .With(&type, method);
}
TEST_F(NativeExportsTests, RegisterTimerShouldAddTheMethodHandle)
{
    timer_method method = [](std::int32_t) {};
    method_registration registration = method_registration::register_method(method);

    When(mock_global_services.timer_service().add_timer_callback)
        .With(2us, 3us, method)
        .Return(123u);

    std::int32_t result = register_timer(2, 3, registration.index());

    EXPECT_EQ(123, result);
}

TEST_F(NativeExportsTests, RegisterUdpDataReceivedShouldAddTheMethodHandle)
{
    udp_data_received_method method = [](std::int32_t, const void*) {};
    method_registration registration = method_registration::register_method(method);

    register_udp_data_received(123, registration.index());

    Verify(mock_global_services.network_service().add_udp_callback)
        .With(static_cast<std::uint16_t>(123), method);
}

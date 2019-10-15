#define _CRT_SECURE_NO_WARNINGS
#include "network_service.h"
#include "pal_mock.h"
#include <cstring>
#include <functional>
#include <gmock/gmock.h>
#include <gtest/gtest.h>

using testing::_;
using testing::InvokeArgument;
using testing::SizeIs;

class MockSocket : public pal_socket
{
public:
    MOCK_METHOD(void, bind, (const pal::socket_handle&, const pal::socket_address&), (override));
    MOCK_METHOD(pal::socket_handle, create_udp_socket, (), (override));
    MOCK_METHOD(void, poll, (const pal::socket_list&, std::function<void(const pal::socket_handle&, pal::poll_event)>), (override));
    MOCK_METHOD(int, recv_from, (const pal::socket_handle&, char*, std::size_t, pal::socket_address*), (override));
};

namespace
{
    void invoke_callback(void(*callback)(std::any&), std::any&& data)
    {
        callback(data);
    }

    std::function<void(std::int32_t, void*)> on_udp_callback;

    void udp_callback(std::int32_t port, void* data)
    {
        on_udp_callback(port, data);
    }
}

class NetworkServiceTests : public testing::Test
{
protected:
    NetworkServiceTests() :
        _service(invoke_callback)
    {
        active_mock = &_socket;
    }

    ~NetworkServiceTests()
    {
        active_mock = nullptr;
        on_udp_callback = nullptr;
    }

    autocrat::network_service _service;
    testing::NiceMock<MockSocket> _socket;
};

TEST_F(NetworkServiceTests, ShouldInvokeTheHandlerWithTheReadData)
{
    pal::socket_handle handle;
    ON_CALL(_socket, poll)
        .WillByDefault(InvokeArgument<1>(handle, pal::poll_event::read));

    ON_CALL(_socket, recv_from(_, _, _, _))
        .WillByDefault([](auto, char* buffer, auto, pal::socket_address* address)
            {
                address->port(123);
                std::strcpy(buffer, "test");
                return 4;
            });

    autocrat::managed_byte_array* array_data = nullptr;
    on_udp_callback = [&](auto, void* data)
    {
        array_data = reinterpret_cast<autocrat::managed_byte_array*>(data);
    };

    _service.add_udp_callback(123, &udp_callback);
    _service.check_and_dispatch();

    ASSERT_NE(nullptr, array_data);
    EXPECT_EQ(4, array_data->size());
    EXPECT_STREQ("test", reinterpret_cast<const char*>(array_data->data()));
}

TEST_F(NetworkServiceTests, ShouldListenOnASingleSocketForTheSamePort)
{
    EXPECT_CALL(_socket, bind(_, _)).Times(1);
    EXPECT_CALL(_socket, poll(SizeIs(1), _));

    _service.add_udp_callback(123, &udp_callback);
    _service.add_udp_callback(123, &udp_callback);

    _service.check_and_dispatch();
}

TEST_F(NetworkServiceTests, ShouldNotReadFromSocketErrors)
{
    pal::socket_handle handle;
    ON_CALL(_socket, poll)
        .WillByDefault(InvokeArgument<1>(handle, pal::poll_event::error));

    EXPECT_CALL(_socket, recv_from(_, _, _, _)).Times(0);

    int call_count = 0;
    on_udp_callback = [&](auto, auto)
    {
        call_count++;
    };

    _service.add_udp_callback(123, &udp_callback);
    _service.check_and_dispatch();

    EXPECT_EQ(0, call_count);
}

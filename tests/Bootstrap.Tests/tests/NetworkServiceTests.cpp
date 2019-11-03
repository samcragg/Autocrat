#define _CRT_SECURE_NO_WARNINGS

#include "network_service.h"

#include <cstring>
#include <functional>
#include <gmock/gmock.h>
#include <gtest/gtest.h>
#include "pal_mock.h"
#include "thread_pool.h"

using testing::_;
using testing::Return;
using testing::SizeIs;

class MockSocket : public pal_socket
{
public:
    MOCK_METHOD(void, bind, (const pal::socket_handle&, const pal::socket_address&), (override));
    MOCK_METHOD(pal::socket_handle, create_udp_socket, (), (override));
    MOCK_METHOD(std::optional<pal::poll_event>, get_poll_event, (const pal::socket_handle&), (override));
    MOCK_METHOD(int, recv_from, (const pal::socket_handle&, char*, std::size_t, pal::socket_address*), (override));
};

class MockThreadPool : public autocrat::thread_pool
{
public:
    void enqueue(callback_function callback, std::any&& data) override
    {
        callback(data);
    }
};

namespace
{
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
        _service(&_thread_pool)
    {
        active_socket_mock = &_socket;
    }

    ~NetworkServiceTests()
    {
        active_socket_mock = nullptr;
        on_udp_callback = nullptr;
    }

    autocrat::network_service _service;
    testing::NiceMock<MockSocket> _socket;
    MockThreadPool _thread_pool;
};

TEST_F(NetworkServiceTests, ShouldInvokeTheHandlerWithTheReadData)
{
    ON_CALL(_socket, get_poll_event)
        .WillByDefault(Return(pal::poll_event::read));

    ON_CALL(_socket, recv_from(_, _, _, _))
        .WillByDefault([](auto, char* buffer, auto, pal::socket_address* address)
            {
                address->port(456);
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

    _service.add_udp_callback(123, &udp_callback);
    _service.add_udp_callback(123, &udp_callback);
}

TEST_F(NetworkServiceTests, ShouldNotReadFromSocketErrors)
{
    pal::socket_handle handle;
    ON_CALL(_socket, get_poll_event)
        .WillByDefault(Return(pal::poll_event::error));

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
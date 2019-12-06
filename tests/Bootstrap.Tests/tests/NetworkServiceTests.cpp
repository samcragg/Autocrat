#define _CRT_SECURE_NO_WARNINGS

#include "network_service.h"

#include <cstring>
#include <functional>
#include <gtest/gtest.h>
#include <cpp_mock.h>
#include "pal_mock.h"
#include "thread_pool.h"


class MockSocket : public pal_socket
{
public:
    MockMethod(void, bind, (const pal::socket_handle&, const pal::socket_address&));
    MockMethod(pal::socket_handle, create_udp_socket, ());
    MockMethod(std::optional<pal::poll_event>, get_poll_event, (const pal::socket_handle&));
    MockMethod(int, recv_from, (const pal::socket_handle&, char*, std::size_t, pal::socket_address*));
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
    std::function<void(std::int32_t, const void*)> on_udp_callback;

    void udp_callback(std::int32_t port, const void* data)
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
    MockSocket _socket;
    MockThreadPool _thread_pool;
};

TEST_F(NetworkServiceTests, ShouldInvokeTheHandlerWithTheReadData)
{
    When(_socket.get_poll_event).Return(pal::poll_event::read);

    When(_socket.recv_from).Do([](auto, char* buffer, auto, pal::socket_address* address)
        {
            address->port(456);
            std::strcpy(buffer, "test");
            return 4;
        });

    const autocrat::managed_byte_array* array_data = nullptr;
    on_udp_callback = [&](auto, const void* data)
    {
        array_data = reinterpret_cast<const autocrat::managed_byte_array*>(data);
    };

    _service.add_udp_callback(123, &udp_callback);
    _service.check_and_dispatch();

    ASSERT_NE(nullptr, array_data);
    EXPECT_EQ(4, array_data->size());
    EXPECT_STREQ("test", reinterpret_cast<const char*>(array_data->data()));
}

TEST_F(NetworkServiceTests, ShouldListenOnASingleSocketForTheSamePort)
{
    _service.add_udp_callback(123, &udp_callback);
    _service.add_udp_callback(123, &udp_callback);

    Verify(_socket.bind).Times(1);
}

TEST_F(NetworkServiceTests, ShouldNotReadFromSocketErrors)
{
    pal::socket_handle handle;
    When(_socket.get_poll_event).Return(pal::poll_event::error);

    int call_count = 0;
    on_udp_callback = [&](auto, auto)
    {
        call_count++;
    };

    _service.add_udp_callback(123, &udp_callback);
    _service.check_and_dispatch();

    Verify(_socket.recv_from).Times(0);
    EXPECT_EQ(0, call_count);
}

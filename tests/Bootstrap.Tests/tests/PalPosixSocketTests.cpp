#define PAL_MOCK_H
#include "pal.h"
#include "pal_posix.h"

#include <string_view>
#include <system_error>
#include <gtest/gtest.h>
#include <arpa/inet.h>
#include <sys/socket.h>

class PalPosixSocketTests : public testing::Test
{
public:
protected:
    std::uint16_t GetPort(const pal::socket_handle& handle)
    {
        sockaddr_in address = {};
        socklen_t length = sizeof(address);
        getsockname(handle.handle(), reinterpret_cast<sockaddr*>(&address), &length);
        return ntohs(address.sin_port);
    }

    void SendData(std::uint16_t port, std::string_view data)
    {
        pal::socket_handle client = pal::create_udp_socket();
        sockaddr_in address = {};
        address.sin_family = AF_INET;
        address.sin_port = htons(port);
        inet_pton(AF_INET, "127.0.0.1", &address.sin_addr.s_addr);
        sendto(client.handle(), data.data(), static_cast<int>(data.size()), 0, reinterpret_cast<sockaddr*>(&address), sizeof(address));
    }
};

TEST_F(PalPosixSocketTests, SocketAddressShouldHandleIP4Addresses)
{
    const char ip[] = "1.2.3.4";

    pal::socket_address address = pal::socket_address::from_string(ip);

    EXPECT_STREQ(ip, address.to_string().c_str());
}

TEST_F(PalPosixSocketTests, SocketAddressShouldHandleIP6Addresses)
{
    const char ip[] = "1:2:3:4:5:6:7:8";

    pal::socket_address address = pal::socket_address::from_string(ip);

    EXPECT_STREQ(ip, address.to_string().c_str());
}

TEST_F(PalPosixSocketTests, SocketAddressShouldGetAndSetThePort)
{
    const std::uint16_t port = 12345;
    pal::socket_address address;

    address.port(port);

    EXPECT_EQ(port, address.port());
}

TEST_F(PalPosixSocketTests, SocketHandleConstructorShouldThrowForSocketErrors)
{
    EXPECT_THROW(pal::socket_handle(-1, -1), std::system_error);
}

TEST_F(PalPosixSocketTests, SocketHandleMoveAssignmentShouldMoveTheResource)
{
    pal::socket_handle original = pal::create_udp_socket();
    int original_handle = original.handle();

    pal::socket_handle moved = std::move(original);

    EXPECT_EQ(original_handle, moved.handle());
    EXPECT_EQ(-1, original.handle());
}

TEST_F(PalPosixSocketTests, SocketHandleMoveConstructorShouldMoveTheResource)
{
    pal::socket_handle original = pal::create_udp_socket();
    int original_handle = original.handle();

    pal::socket_handle moved(std::move(original));

    EXPECT_EQ(original_handle, moved.handle());
    EXPECT_EQ(-1, original.handle());
}

TEST_F(PalPosixSocketTests, BindShouldThrowForErrors)
{
    pal::socket_handle closed = pal::create_udp_socket();
    closed.~socket_handle();

    EXPECT_THROW(pal::bind(closed, pal::socket_address::any_ipv4()), std::system_error);
}

TEST_F(PalPosixSocketTests, PollShouldHandleEmptyLists)
{
    pal::socket_map<int> list;
    bool called = false;

    pal::poll(list, [&](auto&, auto, auto) { called = true; });

    EXPECT_FALSE(called);
}

TEST_F(PalPosixSocketTests, PollShouldInvokeTheCallbackIfDataIsAvailable)
{
    pal::socket_handle server = pal::create_udp_socket();
    pal::bind(server, pal::socket_address::any_ipv4());
    std::uint16_t port = GetPort(server);

    SendData(port, "test");

    pal::socket_map<int> list;
    list.insert({ std::move(server), 123 });

    int called_value = 0;
    pal::poll(list, [&](auto&, int value, auto) { called_value = value; });

    EXPECT_EQ(123, called_value);
}

TEST_F(PalPosixSocketTests, RecvFromShouldReturnZeroIfThereIsNoDataReady)
{
    pal::socket_handle socket = pal::create_udp_socket();
    pal::bind(socket, pal::socket_address::any_ipv4());
 
    char buffer[8];
    int result = pal::recv_from(socket, buffer, sizeof(buffer), nullptr);

    EXPECT_EQ(0, result);
}

TEST_F(PalPosixSocketTests, RecvFromShouldThrowForErrors)
{
    pal::socket_handle closed = pal::create_udp_socket();
    closed.~socket_handle();

    char buffer[8];
    EXPECT_THROW(pal::recv_from(closed, buffer, sizeof(buffer), nullptr), std::system_error);
}

TEST_F(PalPosixSocketTests, RecvFromShouldReturnTheSentData)
{
    pal::socket_handle server = pal::create_udp_socket();
    pal::bind(server, pal::socket_address::any_ipv4());
    SendData(GetPort(server), "1234");

    char buffer[8] = {};
    int result = pal::recv_from(server, buffer, sizeof(buffer), nullptr);

    EXPECT_EQ(4, result);
    EXPECT_STREQ("1234", buffer);
}

TEST_F(PalPosixSocketTests, RecvFromShouldSetTheSender)
{
    pal::socket_handle server = pal::create_udp_socket();
    pal::bind(server, pal::socket_address::any_ipv4());
    SendData(GetPort(server), "1");

    char buffer[8];
    pal::socket_address address;
    pal::recv_from(server, buffer, sizeof(buffer), &address);

    EXPECT_STREQ("127.0.0.1", address.to_string().c_str());
}

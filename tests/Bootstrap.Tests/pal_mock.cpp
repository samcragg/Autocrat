#include "pal.h"
#include "pal_mock.h"

namespace pal
{
    test_socket_address test_socket_address::any_ipv4()
    {
        return from_string("any_ip4");
    }

    test_socket_address test_socket_address::any_ipv6()
    {
        return from_string("any_ip6");
    }

    test_socket_address test_socket_address::from_string(const std::string& value)
    {
        return test_socket_address(value);
    }

    std::uint16_t test_socket_address::port() const noexcept
    {
        return _port;
    }

    void test_socket_address::port(std::uint16_t value) noexcept
    {
        _port = value;
    }

    std::string test_socket_address::to_string() const
    {
        return _address;
    }

    test_socket_address::test_socket_address(const std::string& address) :
        _address(address)
    {
    }

    bool operator==(const test_socket_address& a, const test_socket_address& b)
    {
        return a.to_string() == b.to_string();
    }

    test_socket_handle::test_socket_handle(const test_socket_address& address)
    {
        _address = std::make_shared<test_socket_address>(address);
    }

    bool operator==(const test_socket_handle& a, const test_socket_handle& b)
    {
        return a.address() == b.address();
    }

    auto test_socket_handle::address() const -> const address_ptr&
    {
        return _address;
    }

    bool test_socket_list::empty() const noexcept
    {
        return _sockets.empty();
    }

    void test_socket_list::erase(const value_type& value)
    {
        for (auto it = _sockets.begin(); it != _sockets.end(); ++it)
        {
            if (it->address() == value.address())
            {
                _sockets.erase(it);
                return;
            }
        }
    }

    void test_socket_list::push_back(value_type&& value)
    {
        _sockets.emplace_back(std::move(value));
    }

    auto test_socket_list::handles() const -> const std::vector<value_type>&
    {
        return _sockets;
    }

    std::size_t test_socket_list::size() const noexcept
    {
        return _sockets.size();
    }

    void bind(const test_socket_handle& socket, const test_socket_address& address)
    {
        active_mock->bind(socket, address);
    }

    test_socket_handle test_create_udp_socket()
    {
        return active_mock->create_udp_socket();
    }

    int recv_from(const test_socket_handle& socket, char* buffer, std::size_t length, test_socket_address* from)
    {
        return active_mock->recv_from(socket, buffer, length, from);
    }
}

pal_socket* active_mock = nullptr;

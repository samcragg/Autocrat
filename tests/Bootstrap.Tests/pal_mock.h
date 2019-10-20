#ifndef PAL_MOCK_H
#define PAL_MOCK_H

#include <cstdint>
#include <functional>
#include <string>
#include <vector>

#define create_udp_socket test_create_udp_socket
#define socket_address test_socket_address
#define socket_handle test_socket_handle
#define socket_list test_socket_list

namespace pal
{
    class test_socket_address
    {
    public:
        test_socket_address() = default;
        static test_socket_address any_ipv4();
        static test_socket_address any_ipv6();
        static test_socket_address from_string(const std::string& value);

        std::uint16_t port() const noexcept;
        void port(std::uint16_t value) noexcept;
        std::string to_string() const;
    private:
        explicit test_socket_address(const std::string& address);

        std::string _address;
        std::uint16_t _port = {};
    };

    bool operator==(const test_socket_address& a, const test_socket_address& b);

    class test_socket_handle
    {
    public:
        using address_ptr = std::shared_ptr<test_socket_address>;

        test_socket_handle() = default;
        explicit test_socket_handle(const test_socket_address& address);

        const address_ptr& address() const;
    private:
        address_ptr _address;
    };

    bool operator==(const test_socket_handle& a, const test_socket_handle& b);

    class test_socket_list
    {
    public:
        using value_type = test_socket_handle;

        [[nodiscard]]
        bool empty() const noexcept;

        void erase(const value_type& value);

        void push_back(value_type&& value);

        const std::vector<value_type>& handles() const;
        std::size_t size() const noexcept;
    private:
        std::vector<value_type> _sockets;
    };


    void bind(const test_socket_handle& socket, const test_socket_address& address);
    test_socket_handle test_create_udp_socket();
    int recv_from(const test_socket_handle& socket, char* buffer, std::size_t length, test_socket_address* from);

}

class pal_socket
{
public:
    virtual ~pal_socket() = default;

    virtual void bind(const pal::socket_handle& socket, const pal::socket_address& address) = 0;
    virtual pal::socket_handle create_udp_socket() = 0;
    virtual void poll(const pal::socket_list& sockets, std::function<void(const pal::socket_handle&, pal::poll_event)> callback) = 0;
    virtual int recv_from(const pal::socket_handle& socket, char* buffer, std::size_t length, pal::socket_address* from) = 0;
};

extern pal_socket* active_socket_mock;

namespace pal
{
    template <typename Fn>
    void poll(const socket_list& sockets, Fn callback)
    {
        active_socket_mock->poll(sockets, callback);
    }
}

#endif

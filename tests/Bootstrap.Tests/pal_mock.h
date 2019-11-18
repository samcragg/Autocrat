#ifndef PAL_MOCK_H
#define PAL_MOCK_H

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#define create_udp_socket test_create_udp_socket
#define socket_address test_socket_address
#define socket_handle test_socket_handle
#define socket_map test_socket_map

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

    template <class T>
    class test_socket_map
    {
    public:
        using key_type = test_socket_handle;
        using mapped_type = T;
        using value_type = std::pair<key_type, mapped_type>;

        using storage_type = std::vector<value_type>;
        using iterator = typename storage_type::iterator;

        iterator begin() noexcept
        {
            return _sockets.begin();
        }

        [[nodiscard]]
        bool empty() const noexcept
        {
            return _sockets.empty();
        }

        iterator end() noexcept
        {
            return _sockets.end();
        }

        void erase(const key_type& key)
        {
            std::ptrdiff_t index = &key - _sockets.data();
            _sockets.at(index);
            _sockets.erase(_sockets.begin() + index);
        }

        void insert(value_type&& value)
        {
            _sockets.push_back(std::move(value));
        }

        std::size_t size() const noexcept
        {
            return _sockets.size();
        }
    private:
        std::vector<value_type> _sockets;
    };


    void bind(const test_socket_handle& socket, const test_socket_address& address);
    test_socket_handle test_create_udp_socket();
    int recv_from(const test_socket_handle& socket, char* buffer, std::size_t length, test_socket_address* from);

}

class pal_service
{
public:
    virtual ~pal_service() = default;

    virtual std::chrono::microseconds get_current_time() = 0;
};

class pal_socket
{
public:
    virtual ~pal_socket() = default;

    virtual void bind(const pal::socket_handle& socket, const pal::socket_address& address) = 0;
    virtual pal::socket_handle create_udp_socket() = 0;
    virtual std::optional<pal::poll_event> get_poll_event(const pal::socket_handle& handle) = 0;
    virtual int recv_from(const pal::socket_handle& socket, char* buffer, std::size_t length, pal::socket_address* from) = 0;
};

pal_service* active_service_mock;
pal_socket* active_socket_mock;

namespace pal
{
    template <typename T, typename Fn>
    void poll(const socket_map<T>& sockets, Fn callback)
    {
        for (auto& pair : const_cast<socket_map<T>&>(sockets))
        {
            auto event = active_socket_mock->get_poll_event(pair.first);
            if (event.has_value())
            {
                callback(pair.first, pair.second, event.value());
            }
        }
    }
}

#endif

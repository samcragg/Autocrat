#ifndef PAL_POSIX_H
#define PAL_POSIX_H

#include <cassert>
#include <cstdint>
#include <functional>
#include <string>
#include <vector>
#include <netinet/in.h>
#include <sys/poll.h>

namespace pal
{
    /**
     * Represents a network address.
     */
    class socket_address
    {
    public:
        socket_address() noexcept;

        /**
         * Represents an IPv4 address where the application does not care which
         * local address gets assigned.
         * @returns A new `socket_address`.
         */
        static socket_address any_ipv4();

        /**
         * Represents an IPv6 address where the application does not care which
         * local address gets assigned.
         * @returns A new `socket_address`.
         */
        static socket_address any_ipv6();

        /**
         * Constructs an address from a native handle.
         * @returns A new `socket_address`.
         */
        static socket_address from_native(const sockaddr* native);

        /**
         * Converts an IPv4 or IPv6 address text to a `socket_address`.
         * @returns The parsed address.
         */
        static socket_address from_string(const std::string& value);

        /**
         * Gets the native handle for this instance.
         * @returns A pointer to the native data.
         */
        const sockaddr* native_handle() const noexcept;

        /**
         * Gets the port number.
         * @returns The port number.
         */
        std::uint16_t port() const noexcept;

        /**
         * Sets the port number.
         * @param value The port number.
         */
        void port(std::uint16_t value) noexcept;

        /**
         * Converts this instance to a string in standard format.
         * @returns The text representation of this instance.
         */
        std::string to_string() const;
    private:
        struct header
        {
            std::uint16_t family;
            std::uint16_t port;
        };

        union
        {
            header _header;
            sockaddr_in _address4;
            sockaddr_in6 _address6;
        };
    };

    /**
     * Represents a native socket handle.
     */
    class socket_handle
    {
    public:
        socket_handle(int type, int protocol);
        ~socket_handle() noexcept;

        socket_handle(const socket_handle&) = delete;
        socket_handle& operator=(const socket_handle&) = delete;

        socket_handle(socket_handle&& other) noexcept;
        socket_handle& operator=(socket_handle&& other) noexcept;

        int handle() const;
    private:
        int _handle;
    };

    /**
     * Represents a group of socket_handles.
     * @tparam T The type to store with the socket handle.
     */
    template <class T>
    class socket_map
    {
    public:
        using key_type = socket_handle;
        using mapped_type = T;
        using value_type = std::pair<key_type, mapped_type>;

        using storage_type = std::vector<value_type>;
        using iterator = typename storage_type::iterator;

        /**
         * Returns an iterator to the first element of the container.
         * @returns An iterator to the first element.
         */
        iterator begin() noexcept
        {
            return _sockets.begin();
        }

        /**
         * Checks if the container has no elements.
         * @returns `true` if the container is empty; otherwise, `false`.
         */
        [[nodiscard]]
        bool empty() const noexcept
        {
            return _sockets.empty();
        }

        /**
         * Returns an iterator to the element following the last element of the
         * container.
         * @returns An iterator to the element following the last element.
         */
        iterator end() noexcept
        {
            return _sockets.end();
        }

        /**
         * Removes the specified element from the container.
         * @param key The key value of the element to remove.
         */
        void erase(const key_type& key)
        {
            std::ptrdiff_t index = &key - _sockets.data();
            assert((index >= 0) && (index < _sockets.size()));
            _sockets.erase(_sockets.begin() + index);
        }

        /**
         * Appends the given element value to the end of the container.
         * @param value The value of the element to append.
         */
        void insert(value_type&& value)
        {
            pollfd& descriptor = _poll_descriptors.emplace_back();
            descriptor.fd = value.first.handle();

            _sockets.push_back(std::move(value));
        }

        template <class U, class Fn>
        friend void poll(const socket_map<U>&, Fn);
    private:
        storage_type _sockets;
        mutable std::vector<pollfd> _poll_descriptors;
    };

    namespace detail
    {
        [[noreturn]]
        void throw_socket_error();

        poll_event translate_event(short revents);
    }

    /**
     * Polls the specified handles to see if any data is waiting to be read.
     * @tparam T  The element type of the `socket_map`.
     * @tparam Fn A callback with the signature of void(const socket_handle&, T&, poll_event)
     * @param sockets  The list of sockets to poll.
     * @param callback The function to invoke when an event has been received.
     */
    template <class T, class Fn>
    void poll(const socket_map<T>& sockets, Fn callback)
    {
        if (sockets.empty())
        {
            return;
        }

        for (pollfd& fd : sockets._poll_descriptors)
        {
            fd.events = POLLIN;
        }

        int result = poll(
            sockets._poll_descriptors.data(),
            static_cast<nfds_t>(sockets._poll_descriptors.size()),
            0);

        if (result == -1)
        {
            detail::throw_socket_error();
        }
        else if (result > 0)
        {
            std::size_t count = sockets._sockets.size();
            for (std::size_t i = 0; i != count; ++i)
            {
                short revents = sockets._poll_descriptors[i].revents;
                auto& pair = sockets._sockets[i];
                if (revents != 0)
                {
                    std::invoke(
                        callback,
                        std::as_const(pair.first),
                        pair.second,
                        detail::translate_event(revents));
                }
            }
        }
    }
}

#endif

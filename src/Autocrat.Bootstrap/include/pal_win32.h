#ifndef PAL_WIN32_H
#define PAL_WIN32_H

#include <cstdint>
#include <string>
#include <vector>

#define VC_EXTRALEAN
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>

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

        SOCKET handle() const;
    private:
        SOCKET _handle;
    };

    /**
     * Represents a group of socket_handles.
     */
    class socket_list
    {
    public:
        using value_type = socket_handle;

        /**
         * Checks if the container has no elements.
         * @returns `true` if the container is empty; otherwise, `false`.
         */
        [[nodiscard]]
        bool empty() const noexcept;

        /**
         * Removes the specified element from the container.
         * @param value The value to remove.
         */
        void erase(const value_type& value);

        /**
         * Appends the given element value to the end of the container.
         * @param value The value of the element to append.
         */
        void push_back(value_type&& value);

        template <typename Fn>
        friend void poll(const socket_list&, Fn);
    private:
        std::vector<socket_handle> _sockets;
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
     * @tparam Fn A callback with the signature of void(const socket_handle&, poll_event)
     * @param sockets  The list of sockets to poll.
     * @param callback The function to invoke when an event has been received.
     */
    template <typename Fn>
    void poll(const socket_list& sockets, Fn callback)
    {
        if (sockets.empty())
        {
            return;
        }

        for (pollfd& fd : sockets._poll_descriptors)
        {
            fd.events = POLLIN;
        }

        int result = WSAPoll(sockets._poll_descriptors.data(), static_cast<ULONG>(sockets._poll_descriptors.size()), 0);
        if (result == SOCKET_ERROR)
        {
            detail::throw_socket_error();
        }
        else if (result > 0)
        {
            std::size_t count = sockets._sockets.size();
            for (std::size_t i = 0; i != count; ++i)
            {
                short revents = sockets._poll_descriptors[i].revents;
                if (revents != 0)
                {
                    std::invoke(callback, sockets._sockets[i], detail::translate_event(revents));
                }
            }
        }
    }
}

#endif

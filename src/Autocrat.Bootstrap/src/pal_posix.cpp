#include <chrono>
#include <cstdlib>
#include <system_error>
#include <spdlog/spdlog.h>

#include <errno.h>
#include <fcntl.h>
#include <pthread.h>
#include <sched.h>
#include <signal.h>
#include <unistd.h>
#include <arpa/inet.h>
#include <linux/futex.h>
#include <sys/syscall.h>

#undef UNIT_TESTS
#include "pal.h"

using namespace std::chrono_literals;

namespace
{
    volatile pal::close_signal_method close_signal_handler;

    void control_c_handler(int)
    {
        auto handler = close_signal_handler;
        if (handler != nullptr)
        {
            handler();
        }
    }

    int futex(std::uint32_t* uaddr, int futex_op, std::uint32_t val, const struct timespec* timeout, int* uaddr2, int val3)
    {
        return syscall(SYS_futex, uaddr, futex_op, val, timeout, uaddr2, val3);
    }
}

namespace pal
{
    namespace detail
    {
        [[noreturn]]
        void throw_socket_error()
        {
            throw std::system_error(errno, std::system_category());
        }

        poll_event translate_event(short revents)
        {
            if ((revents & (POLLIN | POLLPRI | POLLRDBAND | POLLRDNORM)) != 0)
            {
                return poll_event::read;
            }
            else if ((revents & (POLLOUT | POLLWRNORM)) != 0)
            {
                return poll_event::write;
            }
            else if ((revents & POLLHUP) != 0)
            {
                return poll_event::hang_up;
            }
            else
            {
                spdlog::warn("revents: {}", revents);
                return poll_event::error;
            }
        }
    }

    socket_address::socket_address() noexcept
        : _address6({})
    {
    }

    socket_address socket_address::any_ipv4()
    {
        socket_address address;
        address._header.family = AF_INET;
        address._address4.sin_addr.s_addr = INADDR_ANY;
        return address;
    }

    socket_address socket_address::any_ipv6()
    {
        socket_address address;
        address._header.family = AF_INET6;
        address._address6.sin6_addr = in6addr_any;
        return address;
    }

    socket_address socket_address::from_native(const sockaddr* native)
    {
        socket_address address;
        switch (native->sa_family)
        {
        case AF_INET:
            address._address4 = *reinterpret_cast<const sockaddr_in*>(native);
            break;
        case AF_INET6:
            address._address6 = *reinterpret_cast<const sockaddr_in6*>(native);
            break;
        default:
            throw std::invalid_argument("Unknown socket protocol");
        }
        return address;
    }

    socket_address socket_address::from_string(const std::string& value)
    {
        socket_address address;
        address._header.family = AF_INET6;
        int result = inet_pton(AF_INET6, value.c_str(), &address._address6.sin6_addr);
        if (result == 0)
        {
            address._header.family = AF_INET;
            result = inet_pton(AF_INET, value.c_str(), &address._address4.sin_addr);
            if (result == 0)
            {
                throw std::invalid_argument("Invalid IP address");
            }
        }

        // This error checks for the IP4 scenario too
        if (result == -1)
        {
            detail::throw_socket_error();
        }

        return address;
    }

    const sockaddr* socket_address::native_handle() const noexcept
    {
        return reinterpret_cast<const sockaddr*>(&_header);
    }

    std::uint16_t socket_address::port() const noexcept
    {
        return ntohs(_header.port);
    }

    void socket_address::port(std::uint16_t value) noexcept
    {
        _header.port = htons(value);
    }

    std::string socket_address::to_string() const
    {
        // https://docs.microsoft.com/en-gb/windows/win32/api/ws2tcpip/nf-ws2tcpip-inet_ntop
        // For an IPv4 address, this buffer should be large enough to hold at least 16 characters.
        // For an IPv6 address, this buffer should be large enough to hold at least 46 characters.
        char buffer[46];
        const void* addr = (_header.family == AF_INET6) ?
            reinterpret_cast<const void*>(&_address6.sin6_addr) :
            &_address4.sin_addr;

        const char* result = inet_ntop(_header.family, addr, buffer, sizeof(buffer));
        if (result == nullptr)
        {
            detail::throw_socket_error();
        }

        return result;
    }

    socket_handle::socket_handle(int type, int protocol)
    {
        _handle = socket(AF_INET, type, protocol);
        if (_handle == -1)
        {
            detail::throw_socket_error();
        }

        int flags = fcntl(_handle, F_GETFL);
        int result = fcntl(_handle, F_SETFL, flags | O_NONBLOCK);
        if (result == -1)
        {
            detail::throw_socket_error();
        }
    }

    socket_handle::~socket_handle() noexcept
    {
        if (_handle != -1)
        {
            close(_handle);
        }
    }

    socket_handle::socket_handle(socket_handle&& other) noexcept
    {
        _handle = other._handle;
        other._handle = -1;
    }

    socket_handle& socket_handle::operator=(socket_handle&& other) noexcept
    {
        _handle = other._handle;
        other._handle = -1;
        return *this;
    }

    int socket_handle::handle() const
    {
        return _handle;
    }

    void bind(const socket_handle& socket, const socket_address& address)
    {
        int result = ::bind(socket.handle(), address.native_handle(), sizeof(address));
        if (result == -1)
        {
            detail::throw_socket_error();
        }
    }

    socket_handle create_udp_socket()
    {
        return socket_handle(SOCK_DGRAM, IPPROTO_UDP);
    }

    std::size_t get_current_processor()
    {
        return static_cast<std::size_t>(sched_getcpu());
    }

    int recv_from(const socket_handle& socket, char* buffer, std::size_t length, socket_address* from)
    {
        sockaddr_storage address;
        socklen_t address_size = sizeof(address);
        sockaddr* address_ptr = reinterpret_cast<sockaddr*>(&address);
        int result = ::recvfrom(socket.handle(), buffer, length, 0, address_ptr, &address_size);
        if (result == -1)
        {
            // We need to check if the error is that there is no data to read,
            // in which case we just say that we received 0 bytes rather than
            // throwing an exception
            int error = errno;
            if (error == EWOULDBLOCK)
            {
                return 0;
            }
            else
            {
                throw std::system_error(error, std::system_category());
            }
        }

        if (from != nullptr)
        {
            *from = socket_address::from_native(address_ptr);
        }
        return result;
    }

    void set_affinity(std::thread& thread, std::size_t index)
    {
        cpu_set_t cpu = {};
        CPU_SET(index, &cpu);
        int result = pthread_setaffinity_np(thread.native_handle(), sizeof(cpu), &cpu);
        if (result != 0)
        {
            spdlog::error("Unable to set the thread's affinity to {} (code: {})", index, errno);
        }
    }

    void set_close_signal_handler(close_signal_method callback)
    {
        close_signal_handler = callback;
        struct sigaction action = {};
        action.sa_handler = &control_c_handler;
        if (sigaction(SIGINT, &action, nullptr) != 0)
        {
            spdlog::error("Unable to set the console control handler (code: {})", errno);
        }
    }

    void wait_on(std::uint32_t* address)
    {
        std::uint32_t current = *address;
        futex(address, FUTEX_WAIT_PRIVATE, current, nullptr, nullptr, 0);
    }

    void wake_all(std::uint32_t* address)
    {
        __sync_fetch_and_add(address, 1);
        futex(address, FUTEX_WAKE_PRIVATE, std::numeric_limits<int>::max(), nullptr, nullptr, 0);
    }
}

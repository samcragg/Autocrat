#include <chrono>
#include <cstdlib>
#include <spdlog/spdlog.h>
#include <system_error>

#undef UNIT_TESTS
#include "pal.h"

#pragma comment(lib, "Synchronization.lib")
#pragma comment(lib, "Ws2_32.lib")

using namespace std::chrono_literals;

namespace
{

struct win_sock_initializer
{
    win_sock_initializer()
    {
        int result = WSAStartup(MAKEWORD(2, 2), &wsa_data);
        if (result != 0)
        {
            spdlog::error("Unable to initialize WinSocks (Error {})", result);
            std::exit(result);
        }
    }

    ~win_sock_initializer()
    {
        WSACleanup();
    }

    WSADATA wsa_data;
};

volatile pal::close_signal_method close_signal_handler;
win_sock_initializer win_sock;

BOOL WINAPI CtrlHandlerRoutine(DWORD)
{
    auto handler = close_signal_handler;
    if (handler == nullptr)
    {
        return FALSE;
    }

    handler();

    // We're running in a separate thread and as soon as we return then
    // ExitProcess will be called. Block this thread so that the main
    // application can exit properly, which in turn will clean up this
    // thread and end the sleep.
    // Five seconds was chosen to match the timeouts on
    // https://docs.microsoft.com/en-us/windows/console/handlerroutine
    std::this_thread::sleep_for(5s);
    return TRUE;
}

std::int64_t get_clock_frequency()
{
    LARGE_INTEGER frequency = {};
    QueryPerformanceFrequency(&frequency);
    return frequency.QuadPart;
}

}

namespace pal::detail
{

[[noreturn]] void throw_socket_error()
{
    throw std::system_error(WSAGetLastError(), std::system_category());
}

poll_event translate_event(short revents)
{
    if ((revents & (POLLRDBAND | POLLRDNORM)) != 0)
    {
        return poll_event::read;
    }
    else if ((revents & POLLWRNORM) != 0)
    {
        return poll_event::write;
    }
    else if ((revents & POLLHUP) != 0)
    {
        return poll_event::hang_up;
    }
    else
    {
        return poll_event::error;
    }
}

}

namespace pal
{

socket_address::socket_address() noexcept : _address6({})
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
    int result =
        inet_pton(AF_INET6, value.c_str(), &address._address6.sin6_addr);
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
    // For an IPv4 address, this buffer should be large enough to hold at least
    // 16 characters. For an IPv6 address, this buffer should be large enough to
    // hold at least 46 characters.
    char buffer[46];
    const void* addr = (_header.family == AF_INET6)
                           ? reinterpret_cast<const void*>(&_address6.sin6_addr)
                           : &_address4.sin_addr;

    const char* result =
        inet_ntop(_header.family, addr, buffer, sizeof(buffer));
    if (result == nullptr)
    {
        detail::throw_socket_error();
    }

    return result;
}

socket_handle::socket_handle(int type, int protocol)
{
    _handle = socket(AF_INET, type, protocol);
    if (_handle == INVALID_SOCKET)
    {
        detail::throw_socket_error();
    }

    unsigned long enable = 1;
    int result = ioctlsocket(_handle, FIONBIO, &enable);
    if (result == SOCKET_ERROR)
    {
        detail::throw_socket_error();
    }
}

socket_handle::~socket_handle() noexcept
{
    if (_handle != INVALID_SOCKET)
    {
        closesocket(_handle);
    }
}

socket_handle::socket_handle(socket_handle&& other) noexcept
{
    _handle = other._handle;
    other._handle = INVALID_SOCKET;
}

socket_handle& socket_handle::operator=(socket_handle&& other) noexcept
{
    _handle = other._handle;
    other._handle = INVALID_SOCKET;
    return *this;
}

SOCKET socket_handle::handle() const
{
    return _handle;
}

void bind(const socket_handle& socket, const socket_address& address)
{
    int result =
        ::bind(socket.handle(), address.native_handle(), sizeof(address));
    if (result == SOCKET_ERROR)
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
    return static_cast<std::size_t>(GetCurrentProcessorNumber());
}

std::chrono::microseconds get_current_time()
{
    static const std::int64_t ticks_per_microsecond =
        get_clock_frequency() / 1'000'000;

    LARGE_INTEGER time = {};
    QueryPerformanceCounter(&time);
    return std::chrono::microseconds(
        (time.QuadPart + ticks_per_microsecond - 1) / ticks_per_microsecond);
}

int recv_from(
    const socket_handle& socket,
    char* buffer,
    std::size_t length,
    socket_address* from)
{
    sockaddr_storage address;
    int address_size = sizeof(address);
    sockaddr* address_ptr = reinterpret_cast<sockaddr*>(&address);
    int result = ::recvfrom(
        socket.handle(),
        buffer,
        static_cast<int>(length),
        0,
        address_ptr,
        &address_size);
    if (result == SOCKET_ERROR)
    {
        // We need to check if the error is that there is no data to read,
        // in which case we just say that we received 0 bytes rather than
        // throwing an exception
        int error = WSAGetLastError();
        if (error == WSAEWOULDBLOCK)
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
    DWORD_PTR result = SetThreadAffinityMask(
        thread.native_handle(), static_cast<DWORD_PTR>(1) << index);
    if (result == 0)
    {
        spdlog::error(
            "Unable to set the thread's affinity to {} (code: {})",
            index,
            GetLastError());
    }
}

void set_close_signal_handler(close_signal_method callback)
{
    close_signal_handler = callback;
    if (!SetConsoleCtrlHandler(
            &CtrlHandlerRoutine, close_signal_handler != nullptr))
    {
        spdlog::error(
            "Unable to set the console control handler (code: {})",
            GetLastError());
    }
}

void wait_on(std::uint32_t* address)
{
    std::uint32_t current = *address;
    WaitOnAddress(address, &current, sizeof(std::uint32_t), INFINITE);
}

void wake_all(std::uint32_t* address)
{
    InterlockedIncrement(address);
    WakeByAddressAll(address);
}

}

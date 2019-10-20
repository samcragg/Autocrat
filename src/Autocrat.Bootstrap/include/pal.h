#ifndef PAL_H
#define PAL_H

#include <cstddef>
#include <thread>

namespace pal
{
    /**
     * Represents the event received when polling a socket.
     */
    enum class poll_event
    {
        /**
         * Indicates an error has occurred.
         */
        error,

        /**
         * Indicates a stream oriented connection has terminated.
         */
        hang_up,

        /**
         * Indicates data may be read without blocking.
         */
        read,

        /**
         * Indicates data may be written without blocking.
         */
        write,
    };

    class socket_address;
    class socket_handle;
    class socket_list;

    /**
     * Associates a local address with a socket.
     * @param socket  The socket to bind.
     * @param address The local address to bind to.
     */
    void bind(const socket_handle& socket, const socket_address& address);

    /**
     * Creates a UDP socket.
     * @returns A wrapper over a native handle.
     */
    socket_handle create_udp_socket();

    /**
     * Gets the index of the CPU the current thread is running on.
     * @returns The current CPU index.
     */
    std::size_t get_current_processor();

    /**
     * Receives a datagram and stores the source address.
     * @param socket The socket to receive on.
     * @param buffer The buffer for the incoming data.
     * @param length The length, in bytes, of the buffer.
     * @param from   An optional pointer to a `socket_address` that will hold
     *               the source address upon return.
     */
    int recv_from(const socket_handle& socket, char* buffer, std::size_t length, socket_address* from);

    /**
     * Sets the index of the CPU the specified thread should run on.
     * @param thread The thread to set the affinity of.
     * @param index  The index of the CPU to run on.
     */
    void set_affinity(std::thread& thread, std::size_t index);

    /**
     * Blocks the current thread until the specified memory has changed.
     * @param address A pointer to the integer to watch.
     * @remarks This function may spuriously wake (i.e. unblock when the value
     *          has not changed)
     */
    void wait_on(std::uint32_t* address);

    /**
     * Wakes all the threads that are waiting on the specified address to change.
     * @param address A pointer to the address to notify.
     */
    void wake_all(std::uint32_t* address);
}

#if defined(UNIT_TESTS)
#  include <pal_mock.h>
#elif defined(_WIN32)
#  include "pal_win32.h"
#elif defined(__linux__)
#  include "pal_posix.h"
#else
#error Unknown platform
#endif

#endif

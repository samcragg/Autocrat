#ifndef NETWORK_SERVICE_H
#define NETWORK_SERVICE_H

#include <any>
#include <cstdint>
#include <unordered_map>
#include "array_pool.h"
#include "method_handles.h"
#include "pal.h"

namespace autocrat
{
    /**
     * Exposes functionality for dealing with the network.
     */
    class network_service
    {
    public:
        using enqueue_work_function = void(*)(void(*)(std::any&), std::any&&);

        explicit network_service(enqueue_work_function enqueue_work);

        /**
         * Associates the specified port with the handler.
         * @param port     The port number to list for messages on.
         * @param callback The method to invoke with the received data.
         */
        void add_udp_callback(std::uint16_t port, udp_register_method callback);

        /**
         * Checks for network messages and dispatches any that have arrived.
         */
        void check_and_dispatch();
    private:
        void handle_poll(const pal::socket_handle& handle, pal::poll_event event);

        array_pool _array_pool;
        std::unordered_multimap<std::uint16_t, udp_register_method> _callbacks;
        pal::socket_list _sockets;
        enqueue_work_function _enqueue_work;
    };
}

#endif

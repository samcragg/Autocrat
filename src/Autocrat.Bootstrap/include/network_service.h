#ifndef NETWORK_SERVICE_H
#define NETWORK_SERVICE_H

#include <any>
#include <cstdint>
#include <unordered_map>
#include "array_pool.h"
#include "defines.h"
#include "method_handles.h"
#include "pal.h"

namespace autocrat
{
    class thread_pool;

    /**
     * Exposes functionality for dealing with the network.
     */
    class network_service
    {
    public:
        MOCKABLE_CONSTRUCTOR(network_service)

        /**
         * Constructs a new instance of the `network_service` class.
         * @param pool Used to dispatch work to.
         */
        explicit network_service(thread_pool* pool);

        /**
         * Associates the specified port with the handler.
         * @param port     The port number to list for messages on.
         * @param callback The method to invoke with the received data.
         */
        MOCKABLE_METHOD void add_udp_callback(std::uint16_t port, udp_register_method callback);

        /**
         * Checks for network messages and dispatches any that have arrived.
         */
        MOCKABLE_METHOD void check_and_dispatch();
    private:
        void handle_poll(const pal::socket_handle& handle, pal::poll_event event);

        array_pool _array_pool;
        std::unordered_multimap<std::uint16_t, udp_register_method> _callbacks;
        pal::socket_list _sockets;
        thread_pool* _thread_pool;
    };
}

#endif

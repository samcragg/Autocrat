#ifndef NETWORK_SERVICE_H
#define NETWORK_SERVICE_H

#include "array_pool.h"
#include "collections.h"
#include "defines.h"
#include "exports.h"
#include "pal.h"
#include <any>
#include <cstdint>

namespace autocrat
{

class thread_pool;

struct socket_data
{
    small_vector<udp_data_received_method> callbacks;
    std::uint16_t port;
};

/**
 * Exposes functionality for dealing with the network.
 */
class network_service
{
public:
    MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(network_service)

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
    MOCKABLE_METHOD void add_udp_callback(
        std::uint16_t port,
        udp_data_received_method callback);

    /**
     * Checks for network messages and dispatches any that have arrived.
     */
    MOCKABLE_METHOD void check_and_dispatch();

private:
    void handle_poll(
        const pal::socket_handle& handle,
        const socket_data& data,
        pal::poll_event event);

    array_pool _array_pool;
    pal::socket_map<socket_data> _sockets;
    thread_pool* _thread_pool;
};

}

#endif

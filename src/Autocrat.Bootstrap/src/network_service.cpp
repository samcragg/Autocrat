#include "network_service.h"
#include "services.h"
#include "thread_pool.h"
#include <cstdlib>
#include <spdlog/spdlog.h>
#include <tuple>

namespace
{

using callback_data = std::tuple<
    pal::socket_address,
    udp_data_received_method,
    autocrat::managed_byte_array_ptr>;

void invoke_callback(std::any& data)
{
    const auto& [address, method, block] = std::any_cast<callback_data>(data);
    method(address.port(), &block->array);
}

}

namespace autocrat
{

network_service::network_service(thread_pool* pool) : _thread_pool(pool)
{
}

void network_service::add_udp_callback(
    std::uint16_t port,
    udp_data_received_method callback)
{
    for (auto& kvp : _sockets)
    {
        if (kvp.second.port == port)
        {
            kvp.second.callbacks.emplace_back(callback);
            return;
        }
    }

    spdlog::info("Creating socket on port {}", port);
    pal::socket_handle socket = pal::create_udp_socket();
    pal::socket_address address = pal::socket_address::any_ipv4();
    address.port(port);
    pal::bind(socket, address);

    socket_data data = {};
    data.port = port;
    data.callbacks.emplace_back(callback);
    _sockets.insert({std::move(socket), std::move(data)});
}

void network_service::check_and_dispatch()
{
    pal::poll(_sockets, [this](auto&& handle, auto&& data, auto&& event) {
        handle_poll(handle, data, event);
    });
}

void network_service::handle_poll(
    const pal::socket_handle& handle,
    const socket_data& data,
    pal::poll_event event)
{
    if (event != pal::poll_event::read)
    {
        spdlog::error("Error received during socket polling");
        // TODO: What should we do here? Reconnect?
        return;
    }

    managed_byte_array_ptr block = _array_pool.aquire();
    pal::socket_address address;
    int size = pal::recv_from(
        handle,
        reinterpret_cast<char*>(block->array.data()),
        block->array.capacity(),
        &address);
    block->array.resize(size);

    SPDLOG_DEBUG("{} bytes received on port {}", size, data.port);

    for (auto callback : data.callbacks)
    {
        _thread_pool->enqueue(
            &invoke_callback, std::make_tuple(address, callback, block));
    }
}

}

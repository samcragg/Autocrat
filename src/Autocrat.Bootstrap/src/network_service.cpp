#include <cstdlib>
#include <functional>
#include <tuple>
#include <spdlog/spdlog.h>
#include "network_service.h"
#include "services.h"
#include "thread_pool.h"

using namespace std::placeholders;

namespace
{
    using callback_data = std::tuple<pal::socket_address, udp_register_method, autocrat::managed_byte_array_ptr>;

    void invoke_callback(std::any& data)
    {
        const auto& [address, method, array] = std::any_cast<callback_data>(data);
        method(address.port(), array.get());
    }
}

namespace autocrat
{
    network_service::network_service(thread_pool* pool) :
        _thread_pool(pool)
    {
    }

    void network_service::add_udp_callback(std::uint16_t port, udp_register_method callback)
    {
        auto existing = _callbacks.find(port);
        if (existing == _callbacks.end())
        {
            spdlog::info("Creating socket on port {}", port);
            pal::socket_handle socket = pal::create_udp_socket();
            pal::bind(socket, pal::socket_address::any_ipv4());
            _sockets.push_back(std::move(socket));
        }

        _callbacks.emplace_hint(existing, port, callback);
    }

    void network_service::check_and_dispatch()
    {
        pal::poll(_sockets, std::bind(&network_service::handle_poll, this, _1, _2));
    }

    void network_service::handle_poll(const pal::socket_handle& handle, pal::poll_event event)
    {
        if (event != pal::poll_event::read)
        {
            spdlog::error("Error received during socket polling");
            // TODO: What should we do here? Reconnect?
            return;
        }

        managed_byte_array_ptr array = _array_pool.aquire();
        pal::socket_address address;
        int size = pal::recv_from(handle, reinterpret_cast<char*>(array->data()), array->capacity(), &address);
        array->resize(size);

        auto range = _callbacks.equal_range(address.port());
        for (auto it = range.first; it != range.second; ++it)
        {
            _thread_pool->enqueue(&invoke_callback, std::make_tuple(address, it->second, array));
        }
    }
}

#include "method_handles.h"
#include "native_exports.h"
#include "services.h"

extern "C"
{
    void* CDECL load_object(const void* type)
    {
        auto* service = autocrat::global_services.get_service<autocrat::worker_service>();
        return service->get_worker(type);
    }

    void CDECL register_constructor(const void* type, std::int32_t handle)
    {
        auto constructor = std::get<construct_worker>(get_known_method(handle));
        auto* service = autocrat::global_services.get_service<autocrat::worker_service>();
        service->register_type(type, constructor);
    }

    std::int32_t CDECL register_timer(std::int64_t delay_us, std::int64_t interval_us, std::int32_t handle)
    {
        auto callback = std::get<timer_method>(get_known_method(handle));
        auto* service = autocrat::global_services.get_service<autocrat::timer_service>();
        return static_cast<std::int32_t>(service->add_timer_callback(
            std::chrono::microseconds(delay_us),
            std::chrono::microseconds(interval_us),
            callback
        ));
    }

    void CDECL register_udp_data_received(std::int32_t port, std::int32_t handle)
    {
        auto callback = std::get<udp_data_received_method>(get_known_method(handle));
        auto* service = autocrat::global_services.get_service<autocrat::network_service>();
        service->add_udp_callback(
            static_cast<std::uint16_t>(port),
            callback
        );
    }
}

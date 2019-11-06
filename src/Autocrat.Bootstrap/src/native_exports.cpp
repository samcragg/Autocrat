#include "method_handles.h"
#include "native_exports.h"
#include "services.h"

extern "C"
{
    void cdecl register_udp_data_received(std::int32_t port, std::int32_t handle)
    {
        auto callback = std::get<udp_register_method>(get_known_method(handle));
        autocrat::global_services.get_service<autocrat::network_service>()->add_udp_callback(
            static_cast<std::uint16_t>(port),
            callback
        );
    }
}

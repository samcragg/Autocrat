#include <cstdint>
#include <cstdlib>
#include "method_handles.h"

// Autocrat.NativeAdapters.NetworkService::OnDataReceived
extern "C" void __cdecl register_udp_data_received(std::int32_t port, std::int32_t handle)
{
    std::printf("Register called, invoking managed method:\n");
    std::get<udp_register_method>(get_known_method(handle))(port, nullptr);
}

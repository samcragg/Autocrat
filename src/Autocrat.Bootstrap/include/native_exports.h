#ifndef NATIVE_EXPORTS_H
#define NATIVE_EXPORTS_H

#include <cstdint>

extern "C"
{
    // Autocrat.NativeAdapters.NetworkService::OnDataReceived
    extern void __cdecl register_udp_data_received(std::int32_t port, std::int32_t handle);
}

#endif

#ifndef NATIVE_EXPORTS_H
#define NATIVE_EXPORTS_H

#include <cstdint>
#include "defines.h"

extern "C"
{
    // Autocrat.NativeAdapters.NetworkService::OnDataReceived
    extern void CDECL register_udp_data_received(std::int32_t port, std::int32_t handle);
}

#endif

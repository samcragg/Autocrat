#ifndef NATIVE_EXPORTS_H
#define NATIVE_EXPORTS_H

#include <cstdint>
#include "defines.h"

extern "C"
{
    // Autocrat.NativeAdapters.WorkerFactory.GetWorker
    extern void* CDECL load_object(const void* type);

    // Autocrat.NativeAdapters.WorkerFactory.RegisterConstructor
    extern void CDECL register_constructor(const void* type, std::int32_t handle);

    // Autocrat.NativeAdapters.TimerService::OnTimerTick
    extern std::int32_t CDECL register_timer(std::int64_t delay_us, std::int64_t interval_us, std::int32_t handle);

    // Autocrat.NativeAdapters.NetworkService::OnDataReceived
    extern void CDECL register_udp_data_received(std::int32_t port, std::int32_t handle);
}

#endif

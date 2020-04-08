#ifndef NATIVE_EXPORTS_H
#define NATIVE_EXPORTS_H

#include <cstdint>
#include "defines.h"
#include "managed_types.h"

extern "C"
{
    // Autocrat.NativeAdapters.WorkerFactory.LoadObjectGuid
    extern void CDECL load_object_guid(const void* type, managed_guid* id, typed_reference* result);

    // Autocrat.NativeAdapters.WorkerFactory.LoadObjectInt64
    extern void CDECL load_object_int64(const void* type, std::int64_t id, typed_reference* result);

    // Autocrat.NativeAdapters.WorkerFactory.LoadObjectString
    extern void CDECL load_object_string(const void* type, managed_string* id, typed_reference* result);

    // Autocrat.NativeAdapters.WorkerFactory.RegisterConstructor
    extern void CDECL register_constructor(const void* type, std::int32_t handle);

    // Autocrat.NativeAdapters.TimerService::OnTimerTick
    extern std::int32_t CDECL register_timer(std::int64_t delay_us, std::int64_t interval_us, std::int32_t handle);

    // Autocrat.NativeAdapters.NetworkService::OnDataReceived
    extern void CDECL register_udp_data_received(std::int32_t port, std::int32_t handle);

    // Autocrat.NativeAdapters.TaskServiceSynchronizationContext::TaskEnqueue
    extern void CDECL task_enqueue(managed_delegate* callback, void* state);

    // Autocrat.NativeAdapters.TaskServiceSynchronizationContext::TaskStartNew
    extern void CDECL task_start_new(managed_delegate* action);
}

#endif

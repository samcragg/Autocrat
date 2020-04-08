#include <string_view>
#include "method_handles.h"
#include "native_exports.h"
#include "services.h"

namespace
{
    void load_object(const void* type, typed_reference* result, std::string_view id)
    {
        auto* service = autocrat::global_services.get_service<autocrat::worker_service>();
        void* worker = service->get_worker(type, id);

        // result holds a reference to a local managed variable, which is an
        // object reference. So it's a pointer to a pointer, hence the cast
        *static_cast<void**>(result->value) = worker;
    }
}

extern "C"
{
    void CDECL load_object_guid(const void* type, managed_guid* id, typed_reference* result)
    {
        load_object(
            type,
            result,
            std::string_view(reinterpret_cast<char*>(&id->data), sizeof(id->data)));
    }

    void CDECL load_object_int64(const void* type, std::int64_t id, typed_reference* result)
    {
        load_object(
            type,
            result,
            std::string_view(reinterpret_cast<char*>(&id), sizeof(id)));
    }

    void CDECL load_object_string(const void* type, managed_string* id, typed_reference* result)
    {
        load_object(
            type,
            result,
            std::string_view(reinterpret_cast<char*>(id->data), id->length * sizeof(char16_t)));
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

    void CDECL task_enqueue(managed_delegate* callback, void* state)
    {
        autocrat::global_services.get_service<autocrat::task_service>()->enqueue(callback, state);
    }

    void CDECL task_start_new(managed_delegate* action)
    {
        autocrat::global_services.get_service<autocrat::task_service>()->start_new(action);
    }
}

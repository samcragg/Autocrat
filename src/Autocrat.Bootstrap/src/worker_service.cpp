#include <stdexcept>
#include "worker_service.h"

namespace
{
    std::uintptr_t get_type(const void* type)
    {
        // We don't use these bits on x64
        return reinterpret_cast<std::uintptr_t>(type) >> 3;
    }
}
namespace autocrat
{
    worker_service::worker_service(thread_pool*)
    {
    }

    void* worker_service::get_worker(const void* type_ptr)
    {
        std::uintptr_t type = get_type(type_ptr);
        void*& worker = _workers[type];
        if (worker == nullptr)
        {
            worker = make_worker(type);
        }

        return worker;
    }

    void worker_service::register_type(const void* type, construct_worker constructor)
    {
        _constructors.try_emplace(get_type(type), constructor);
    }

    void* worker_service::make_worker(std::uintptr_t type)
    {
        auto constructor = _constructors.find(type);
        if (constructor == _constructors.end())
        {
            throw std::invalid_argument("Type has not been registered");
        }

        return constructor->second();
    }
}

#include <algorithm>
#include <cassert>
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
    thread_local small_vector<worker_service::worker_info*>* worker_service::thread_allocated_workers;

    worker_service::worker_service(thread_pool* pool) :
        _allocated_workers(pool->size())
    {
    }

    void* worker_service::get_worker(const void* type_ptr, std::string_view id)
    {
        std::uintptr_t type = get_type(type_ptr);

        // TODO: C++ 20 would allow the use of string_view here
        auto workers = _workers.equal_range(std::string(id.begin(), id.end()));

        for (auto& it = workers.first; it != workers.second; ++it)
        {
            worker_info& worker = it->second;
            if (worker.type == type)
            {
                return load_worker(worker);
            }
        }

        return make_worker(type, std::string(id.begin(), id.end()));
    }

    void worker_service::on_begin_work(std::size_t thread_id)
    {
        thread_allocated_workers = &_allocated_workers[thread_id];
    }

    void worker_service::on_end_work(std::size_t thread_id)
    {
        assert(thread_allocated_workers == &_allocated_workers[thread_id]);
        UNUSED(thread_id);

        for (worker_info* info : *thread_allocated_workers)
        {
            save_worker(*info);
        }

        thread_allocated_workers->clear();
    }

    void worker_service::register_type(const void* type, construct_worker constructor)
    {
        _constructors.try_emplace(get_type(type), constructor);
    }

    void* worker_service::load_worker(worker_info& info)
    {
        if (info.object == nullptr)
        {
            info.object = info.serializer.restore();
            thread_allocated_workers->emplace_back(&info);
        }

        return info.object;
    }

    void* worker_service::make_worker(type_handle type, std::string id)
    {
        auto constructor = _constructors.find(type);
        if (constructor == _constructors.end())
        {
            throw std::invalid_argument("Type has not been registered");
        }

        auto it = _workers.emplace(std::move(id), worker_info());
        auto& info = it->second;
        info.type = type;
        info.object = constructor->second();

        thread_allocated_workers->emplace_back(&info);
        return info.object;
    }

    void worker_service::save_worker(worker_info& info)
    {
        info.serializer.save(info.object);
        info.object = nullptr;
    }
}

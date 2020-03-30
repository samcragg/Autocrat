#include <algorithm>
#include <mutex>
#include <shared_mutex>
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
    namespace detail
    {
        bool worker_key_equal::operator()(const worker_key& a, const worker_key& b) const
        {
            return (a.type == b.type) && (a.id == b.id);
        }

        std::size_t worker_key_hash::operator()(const worker_key& key) const
        {
            std::hash<std::string> id_hasher;
            return (key.type * 31u) + id_hasher(key.id);
        }
    }

    worker_service::worker_service(thread_pool* pool) :
        base_type(pool)
    {
    }

    void* worker_service::get_worker(const void* type_ptr, std::string_view id)
    {
        std::uintptr_t type = get_type(type_ptr);
        void* worker = nullptr;
        if (find_existing(type, id, worker))
        {
            return worker;
        }
        else
        {
            return make_worker({ type, std::string(id.begin(), id.end()) });
        }
    }

    void worker_service::on_end_work(std::size_t thread_id)
    {
        for (worker_info* info : *thread_storage)
        {
            save_worker(*info);
        }

        thread_storage->clear();
        base_type::on_end_work(thread_id);
    }

    void worker_service::register_type(const void* type, construct_worker constructor)
    {
        _constructors.try_emplace(get_type(type), constructor);
    }

    bool worker_service::find_existing(worker_key::type_handle type, std::string_view id, void*& result) const
    {
        std::shared_lock<decltype(_workers_lock)> lock(_workers_lock);

        // TODO: C++ 20 would allow the use of a different key type here, one using string_view
        auto it = _workers.find({ type, std::string(id.begin(), id.end()) });
        if (it != _workers.end())
        {
            lock.unlock();
            result = load_worker(const_cast<worker_info&>(it->second));
            return true;
        }

        return false;
    }

    void* worker_service::load_worker(worker_info& info) const
    {
        if (!info.lock.try_lock())
        {
            return nullptr;
        }

        if (info.object == nullptr)
        {
            info.object = info.serializer.restore();
            thread_storage->emplace_back(&info);
        }

        return info.object;
    }

    void* worker_service::make_worker(worker_key&& key)
    {
        worker_key::type_handle constructor_type = key.type;

        std::unique_lock<decltype(_workers_lock)> lock(_workers_lock);
        auto [it, inserted] = _workers.try_emplace(std::move(key));
        if (!inserted)
        {
            lock.unlock();
            return load_worker(it->second);
        }
        else
        {
            // We need to release the _worker_lock ASAP, so lock the worker to
            // prevent anyone else playing with it so we can unlock the workers
            // and then we can run the construction without holding any other
            // thread up. Note we can't use the iterator after we've called
            // unlock, as another thread could then insert something that
            // causes a rehash to invalidate it.
            auto& worker = it->second;
            worker.lock.try_lock();
            lock.unlock();

            auto constructor = _constructors.find(constructor_type);
            if (constructor == _constructors.end())
            {
                throw std::invalid_argument("Type has not been registered");
            }

            worker.object = constructor->second();
            thread_storage->emplace_back(&worker);
            return worker.object;
        }
    }

    void worker_service::save_worker(worker_info& info)
    {
        info.serializer.save(info.object);
        info.object = nullptr;
        info.lock.unlock();
    }
}

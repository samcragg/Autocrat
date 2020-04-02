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

    auto worker_service::release_locked() -> worker_collection
    {
        auto locked_workers = thread_storage;
        worker_collection workers(locked_workers->size());

        worker_info** dest = workers.data();
        for (worker_info* worker : *locked_workers)
        {
            save_worker(*worker);
            *dest++ = worker;
        }

        locked_workers->clear();
        return workers;
    }

    auto worker_service::try_lock(const worker_collection& workers) -> std::optional<object_collection>
    {
        // Since the workers locks are recursive, we can go through and try to
        // lock them all. If that works we can then call load_worker, which
        // will lock it again. This means it's locked twice so we need to
        // unlock it once inside this method, which also allows us to handle
        // the case that we only locked some of them and, therefore, need to
        // unlock them again.
        std::size_t locked_count = 0;
        for (; locked_count != workers.size(); ++locked_count)
        {
            if (!workers[locked_count]->lock.try_lock())
            {
                break;
            }
        }

        std::optional<object_collection> result = std::nullopt;
        if (locked_count == workers.size())
        {
            // We locked them all so now we can do the expensive loading
            object_collection objects(workers.size());
            for (std::size_t i = 0; i != workers.size(); ++i)
            {
                objects[i] = load_worker(*workers[i]);
                assert(objects[i] != nullptr);
            }

            result = std::move(objects);
        }

        for (std::size_t i = 0; i != locked_count; ++i)
        {
            workers[i]->lock.unlock();
        }

        return result;
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

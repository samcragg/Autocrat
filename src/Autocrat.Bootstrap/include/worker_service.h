#ifndef WORKER_SERVICE_H
#define WORKER_SERVICE_H

#include <cstdint>
#include <string>
#include <string_view>
#include <unordered_map>
#include "collections.h"
#include "defines.h"
#include "locks.h"
#include "managed_interop.h"
#include "method_handles.h"
#include "thread_pool.h"

namespace autocrat
{
    namespace detail
    {
        struct worker_key
        {
            using type_handle = std::uintptr_t;
            type_handle type;
            std::string id;
        };

        struct worker_key_equal
        {
            bool operator()(const worker_key& a, const worker_key& b) const;
        };

        struct worker_key_hash
        {
            std::size_t operator()(const worker_key& key) const;
        };
    }

    class worker_service;

    /**
     * Contains information about a managed worker.
     */
    class worker_info
    {
    public:
    private:
        friend class worker_service;

        object_serializer serializer;
        void* object;
        exclusive_lock lock;
    };

    /**
     * Exposes functionality for obtaining worker services.
     */
    class worker_service : public thread_specific_storage<small_vector<worker_info*>>
    {
    public:
        using base_type = thread_specific_storage<small_vector<worker_info*>>;
        using worker_collection = dynamic_array<worker_info*>;

        MOCKABLE_CONSTRUCTOR_AND_DESTRUCTOR(worker_service)

        /**
         * Constructs a new instance of the `worker_service` class.
         * @param pool Used to dispatch work to.
         */
        explicit worker_service(thread_pool* pool);

        /**
         * Gets a worker of the specified type.
         * @param type The type of the worker to return.
         * @returns A managed object of the specified type.
         */
        // TODO: C++ 20 span would be better than string_view
        MOCKABLE_METHOD void* get_worker(const void* type, std::string_view id);

        void on_end_work(std::size_t thread_id) override;

        /**
         * Registers the specified constructor.
         * @param type        The type of the worker returned by the constructor.
         * @param constructor The method to construct a new managed instance.
         */
        MOCKABLE_METHOD void register_type(const void* type, construct_worker constructor);
    private:
        using worker_key = detail::worker_key;

        bool find_existing(worker_key::type_handle type, std::string_view id, void*& result) const;
        void* load_worker(worker_info& info) const;
        void* make_worker(worker_key&& key);
        void save_worker(worker_info& info);

        std::unordered_map<worker_key::type_handle, construct_worker> _constructors;
        std::unordered_map<
            worker_key,
            worker_info,
            detail::worker_key_hash,
            detail::worker_key_equal> _workers;

        mutable shared_spin_lock _workers_lock;
    };
}

#endif

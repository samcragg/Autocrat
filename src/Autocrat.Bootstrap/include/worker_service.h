#ifndef WORKER_SERVICE_H
#define WORKER_SERVICE_H

#include <cstdint>
#include <string>
#include <string_view>
#include <unordered_map>
#include "collections.h"
#include "defines.h"
#include "managed_interop.h"
#include "method_handles.h"
#include "thread_pool.h"

namespace autocrat
{
    /**
     * Exposes functionality for obtaining worker services.
     */
    class worker_service : public lifetime_service
    {
    public:
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

        void on_begin_work(std::size_t thread_id) override;
        void on_end_work(std::size_t thread_id) override;

        /**
         * Registers the specified constructor.
         * @param type        The type of the worker returned by the constructor.
         * @param constructor The method to construct a new managed instance.
         */
        MOCKABLE_METHOD void register_type(const void* type, construct_worker constructor);
    private:
        using type_handle = std::uintptr_t;

        struct worker_info
        {
            type_handle type;
            object_serializer serializer;
            void* object;
        };

        void* load_worker(worker_info& info);
        void* make_worker(type_handle type, std::string id);
        void save_worker(worker_info& info);

        static thread_local small_vector<worker_info*>* thread_allocated_workers;
        dynamic_array<small_vector<worker_info*>> _allocated_workers;
        std::unordered_map<type_handle, construct_worker> _constructors;
        std::unordered_multimap<std::string, worker_info> _workers;
    };
}

#endif

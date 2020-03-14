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
    namespace detail
    {
        struct worker_info
        {
            using type_handle = std::uintptr_t;

            type_handle type;
            object_serializer serializer;
            void* object;
        };
    }

    /**
     * Exposes functionality for obtaining worker services.
     */
    class worker_service : public thread_specific_storage<small_vector<detail::worker_info*>>
    {
    public:
        using base_type = thread_specific_storage<small_vector<detail::worker_info*>>;

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
        using worker_info = detail::worker_info;

        void* load_worker(worker_info& info);
        void* make_worker(worker_info::type_handle type, std::string id);
        void save_worker(worker_info& info);

        std::unordered_map<worker_info::type_handle, construct_worker> _constructors;
        std::unordered_multimap<std::string, worker_info> _workers;
    };
}

#endif

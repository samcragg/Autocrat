#ifndef THREAD_POOL_H
#define THREAD_POOL_H

#include <any>
#include <atomic>
#include <thread>
#include <tuple>
#include "collections.h"
#include "defines.h"

namespace autocrat
{
    /**
     * Allows the monitoring of the work request.
     */
    class lifetime_service
    {
    public:
        /**
         * Called when a work item is about to be invoked.
         * @param thread_id The id of the thread that will run the work item.
         */
        virtual void on_begin_work(std::size_t thread_id) = 0;

        /**
         * Called when a work item has completed.
         * @param thread_id The if of the thread that ran the work item.
         */
        virtual void on_end_work(std::size_t thread_id) = 0;
    protected:
        ~lifetime_service() = default;
    };

    /**
     * Allows the queuing up of work to be performed in the future.
     */
    class thread_pool
    {
    public:
        /**
         * Represents the function signature to of methods to invoke on the
         * background threads.
         */
        using callback_function = void(*)(std::any& param);

        MOCKABLE_CONSTRUCTOR(thread_pool)

        /**
         * Constructs a new instance of the `thread_pool` class.
         * @param cpu_id  The index of the first core to bind to.
         * @param threads The number of threads the pool should contain.
         */
        thread_pool(std::size_t cpu_id, std::size_t threads);

        /**
         * Destructs the `thread_pool` instance.
         */
        MOCKABLE_METHOD ~thread_pool() noexcept;

        /**
         * Registers the service to receive notifications.
         * @param service The instance to notify on work item events.
         */
        MOCKABLE_METHOD void add_observer(lifetime_service* service);

        /**
         * Enqueues the specified work to be performed in a background thread.
         * @param function The function to invoke.
         * @param arg      The data to pass to the function.
         */
        MOCKABLE_METHOD void enqueue(callback_function function, std::any&& arg);

        /**
         * Gets the size of the thread pool.
         * @returns The number of worker threads.
         */
        MOCKABLE_METHOD std::size_t size() const noexcept;
    private:
        using work_item = std::tuple<callback_function, std::any>;
        void invoke_work_item(std::size_t index, work_item& item) const;
        void perform_work(std::size_t index);
        void wait_for_work();

        std::atomic_bool _is_running;
        std::atomic_uint32_t _sleeping;
        bounded_queue<work_item, 1024> _work;
        small_vector<lifetime_service*> _observers;
        dynamic_array<std::thread> _threads;
        std::uint32_t _wait_handle;
    };
}

#endif

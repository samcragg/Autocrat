#ifndef THREAD_POOL_H
#define THREAD_POOL_H

#include <any>
#include <atomic>
#include <cassert>
#include <limits>
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
         * Represents the ID of the global thread
         */
        static constexpr std::size_t global_thread_id = std::numeric_limits<std::size_t>::max();

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
         * Represents the function signature of methods to invoke on the
         * background threads.
         */
        using callback_function = void(*)(std::any& param);

        /**
         * Represents the function signature of methods to invoke during
         * background thread initialization.
         */
        using initialize_function = void(*)(std::size_t thread_id);

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

        /**
         * Starts the background threads and, therefore, processing of work.
         * @remarks This method blocks until all the background threads have
         *          completed initialization and, therefore, are ready for
         *          processing work.
         */
        MOCKABLE_METHOD void start(initialize_function initialize);
    private:
        using work_item = std::tuple<callback_function, std::any>;
        void invoke_work_item(std::size_t index, work_item& item) const;
        void perform_work(std::size_t index, initialize_function initialize);
        void wait_for_work();

        std::atomic_bool _is_running;
        std::atomic_uint32_t _initialized;
        std::atomic_uint32_t _sleeping;
        bounded_queue<work_item, 1024> _work;
        small_vector<lifetime_service*> _observers;
        std::size_t _starting_cpu;
        dynamic_array<std::thread> _threads;
        std::uint32_t _wait_handle;
    };

    /**
     * Provides a base class for services requiring storage specific to each
     * thread in the thread pool.
     * @tparam T The type to store per thread.
     */
    template <class T>
    class thread_specific_storage : public lifetime_service
    {
    public:
        void on_begin_work(std::size_t thread_id) override
        {
            // We store the global thread in the first slot (_storage[0]),
            // therefore, add one to the thread_id to skip over it
            assert(thread_storage == nullptr);
            thread_storage = &_storage[thread_id + 1u];
        }

        void on_end_work(std::size_t thread_id) override
        {
            assert(thread_storage == &_storage[thread_id + 1u]);
            UNUSED(thread_id);

            thread_storage = nullptr;
        }
    protected:
        MOCKABLE_CONSTRUCTOR(thread_specific_storage)

        /**
         * Constructs a new instance of the `thread_specific_storage` class.
         * @param pool Used to dispatch work to.
         */
        explicit thread_specific_storage(thread_pool* pool) :
            _storage(pool->size() + 1u) // Allow for the global thread
        {
        }

#ifdef UNIT_TESTS
        virtual ~thread_specific_storage()
        {
            thread_storage = nullptr;
        }
#else
        ~thread_specific_storage() = default;
#endif

        static thread_local inline T* thread_storage;
    private:
        dynamic_array<T> _storage;
    };
}

#endif

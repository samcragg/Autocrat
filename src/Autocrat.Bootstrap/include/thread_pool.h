#ifndef THREAD_POOL_H
#define THREAD_POOL_H

#include <any>
#include <atomic>
#include <memory>
#include <thread>
#include <tuple>
#include "collections.h"

namespace autocrat
{
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

        /**
         * Constructs a new instance of the `thread_pool` class.
         * @param cpu_id  The index of the first core to bind to.
         * @param threads The number of threads the pool should contain.
         */
        thread_pool(std::size_t cpu_id, std::size_t threads);
        ~thread_pool() noexcept;

        /**
         * Enqueues the specified work to be performed in a background thread.
         * @param function The function to invoke.
         * @param arg      The data to pass to the function.
         */
        void enqueue(callback_function function, std::any&& arg);
    private:
        using work_item = std::tuple<callback_function, std::any>;
        void perform_work();
        void wait_for_work();

        std::atomic_bool _is_running;
        std::atomic_uint32_t _sleeping;
        bounded_queue<work_item, 1024> _work;
        std::unique_ptr<std::thread[]> _threads;
        std::size_t _thread_count;
        std::uint32_t _wait_handle;
    };
}

#endif

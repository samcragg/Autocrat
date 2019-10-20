#ifndef SERVICES_H
#define SERVICES_H

#include <memory>
#include <tuple>
#include "defines.h"
#include "network_service.h"
#include "thread_pool.h"

namespace autocrat
{
    /**
     * Contains the services used by the application.
     * @tparam ThreadPool The class used to enqueue background work.
     * @tparam Services   The services classes to maintain.
     * @remarks The `Service` class must have the following:
     * + A constructor accepting a pointer to a ThreadPool
     * + A method called `check_and_dispatch`
     */
    template <class ThreadPool, class... Services>
    class services
    {
    public:
        /**
         * Allows each service to check for work to do and dispatch it to the
         * thread pool.
         */
        void check_and_dispatch()
        {
            std::apply(
                [](auto&... service) { (service->check_and_dispatch(), ...); },
                _services);
        }

        /**
         * Gets the specified service instance.
         * @returns A pointer to the service.
         */
        template <class Service>
        Service* get_service()
        {
            return std::get<std::unique_ptr<Service>>(_services).get();
        }

        /**
         * Initializes the service classes.
         */
        void initialize()
        {
            _services = std::make_tuple(std::make_unique<Services...>(_thread_pool.get()));
        }

        /**
         * Initializes the thread pool.
         * @tparam Args The argument types.
         * @param args The constructor arguments for the thread pool.
         */
        template <class... Args>
        void initialize_thread_pool(Args&&... args)
        {
            _thread_pool = std::make_unique<ThreadPool>(std::forward<Args>(args)...);
        }
    private:
        GIVE_ACCESS_TO_MOCKS
        std::tuple<std::unique_ptr<Services>...> _services;
        std::unique_ptr<ThreadPool> _thread_pool;
    };

    using global_services_type = services<
        thread_pool,
        network_service>;

    extern MOCKABLE_GLOBAL(global_services_type) global_services;
}

#endif

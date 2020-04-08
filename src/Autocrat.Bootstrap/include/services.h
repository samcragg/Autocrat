#ifndef SERVICES_H
#define SERVICES_H

#include <memory>
#include <tuple>
#include <type_traits>
#include "defines.h"
#include "gc_service.h"
#include "network_service.h"
#include "task_service.h"
#include "thread_pool.h"
#include "timer_service.h"
#include "worker_service.h"

namespace autocrat
{
    /**
     * Contains the services used by the application.
     * @tparam ThreadPool The class used to enqueue background work.
     * @tparam Services   The services classes to maintain.
     * @remarks The `Service` class must have the following:
     * + A constructor accepting a pointer to a ThreadPool
     * + (optional) A method called `check_and_dispatch`
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
            invoke_all<has_check_and_dispatch>([](auto& service) { service.check_and_dispatch(); });
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
         * Gets the thread pool.
         */
        ThreadPool& get_thread_pool()
        {
            return *_thread_pool;
        }

        /**
         * Initializes the service classes.
         */
        void initialize()
        {
            _services = std::make_tuple(std::make_unique<Services>(_thread_pool.get())...);
            invoke_all<is_base_of_lifetime_service>([this](auto& service) { _thread_pool->add_observer(&service); });
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

        template <class Service, class Fallback = void>
        struct has_check_and_dispatch : std::false_type
        {
        };

        template <class Service>
        struct has_check_and_dispatch<
            Service,
            typename std::enable_if<std::is_member_function_pointer_v<decltype(&Service::check_and_dispatch)>>::type>
            : std::true_type
        {
        };

        template <class Service>
        struct is_base_of_lifetime_service : std::is_base_of<lifetime_service, Service>
        {
        };

        template <template<class> class Pred, class Action>
        void invoke_all(Action action)
        {
            ((invoke<Pred<Services>>(
                std::get<std::unique_ptr<Services>>(_services),
                action
                )), ...);
        }

        template <class Pred, class Service, class Action>
        void invoke(const std::unique_ptr<Service>& service, Action action)
        {
            if constexpr (Pred::value)
            {
                action(*service);
            }
            else
            {
                ((void)action);
                ((void)service);
            }
        }

        std::tuple<std::unique_ptr<Services>...> _services;
        std::unique_ptr<ThreadPool> _thread_pool;
    };

    using global_services_type = services<
        thread_pool,
        gc_service,
        network_service,
        task_service,
        timer_service,
        worker_service>;

    extern MOCKABLE_GLOBAL(global_services_type) global_services;
}

#endif

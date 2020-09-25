#ifndef MOCK_SERVICES_H
#define MOCK_SERVICES_H

#include "services.h"
#include <cpp_mock.h>

class mock_gc_service : public autocrat::gc_service
{
public:
    MockMethod(void*, allocate, (std::size_t))
    MockMethod(void, begin_work, (std::size_t))
    MockMethod(void, end_work, (std::size_t))

    autocrat::gc_heap reset_heap() override
    {
        return autocrat::gc_heap();
    }

    void set_heap(autocrat::gc_heap&&) override
    {
    }
};

class mock_network_service : public autocrat::network_service
{
public:
    MockMethod(void, add_udp_callback, (std::uint16_t, udp_data_received_method))
    MockMethod(void, check_and_dispatch, ())
};

class mock_task_service : public autocrat::task_service
{
public:
    MockMethod(void, enqueue, (managed_delegate*, void*))
    MockMethod(void, start_new, (managed_delegate*))
};

class mock_thread_pool : public autocrat::thread_pool
{
public:
    MockMethod(void, add_observer, (autocrat::lifetime_service*))
    MockMethod(std::size_t, size, (), const noexcept)
    MockMethod(void, start, (std::size_t, std::size_t, initialize_function))
};

class mock_timer_service : public autocrat::timer_service
{
public:
    MockMethod(std::uint32_t, add_timer_callback, (duration, duration, timer_method))
    MockMethod(void, check_and_dispatch, ())
};

class mock_worker_service : public autocrat::worker_service
{
public:
    MockMethod(void*, get_worker, (const void*, std::string_view))
    MockMethod(void, register_type, (const void*, construct_worker))
    
    std::tuple<object_collection, worker_collection> release_locked() override
    {
        object_collection objects(1u);
        objects[0] = original_worker;
        return std::make_tuple(std::move(objects), worker_collection(1u));
    }

    std::optional<object_collection> try_lock(const worker_collection&) override
    {
        if (is_locked)
        {
            is_locked = false;
            return std::nullopt;
        }
        else
        {
            object_collection objects(1u);
            objects[0] = locked_worker;
            return objects;
        }
    }

    bool is_locked = false;
    void* locked_worker = nullptr;
    void* original_worker = nullptr;
};

class mock_services : public autocrat::global_services_type
{
public:
    MockMethod(void, check_and_dispatch, ())
    MockMethod(void, initialize, ())

    void create_services()
    {
        _thread_pool = std::make_unique<mock_thread_pool>();
        _services = std::make_tuple(
            std::make_unique<mock_gc_service>(),
            std::make_unique<mock_network_service>(),
            std::make_unique<mock_task_service>(),
            std::make_unique<mock_timer_service>(),
            std::make_unique<mock_worker_service>()
        );
    }

    void release_services()
    {
        _services = {};
    }

    mock_gc_service& gc_service()
    {
        return *static_cast<mock_gc_service*>(
            get_service<autocrat::gc_service>());
    }

    mock_network_service& network_service()
    {
        return *static_cast<mock_network_service*>(
            get_service<autocrat::network_service>());
    }

    mock_task_service& task_service()
    {
        return *static_cast<mock_task_service*>(
            get_service<autocrat::task_service>());
    }

    mock_thread_pool& thread_pool()
    {
        return *static_cast<mock_thread_pool*>(_thread_pool.get());
    }

    mock_timer_service& timer_service()
    {
        return *static_cast<mock_timer_service*>(
            get_service<autocrat::timer_service>());
    }

    mock_worker_service& worker_service()
    {
        return *static_cast<mock_worker_service*>(
            get_service<autocrat::worker_service>());
    }
};

extern mock_services mock_global_services;

#endif

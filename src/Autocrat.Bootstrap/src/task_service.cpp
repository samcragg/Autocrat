#include <any>
#include <unordered_map>
#include "gc_service.h"
#include "managed_interop.h"
#include "services.h"
#include "task_service.h"
#include "thread_pool.h"
#include "worker_service.h"

namespace
{
    struct delegate_info
    {
        void* method;
        void* target;
    };

    struct task_context
    {
        using workers_field_map = std::unordered_multimap<std::size_t, void**>;

        autocrat::gc_heap heap;
        autocrat::thread_pool* thread_pool;
        autocrat::worker_service::worker_collection workers;
        workers_field_map worker_fields;
        delegate_info delegate;
        void* state;
    };

    class worker_field_scanner : private autocrat::object_scanner
    {
    public:
        worker_field_scanner(
            task_context::workers_field_map& map,
            const autocrat::worker_service::object_collection& worker_objects) :
            _map(&map),
            _objects_begin(worker_objects.begin()),
            _objects_end(worker_objects.end())
        {
        }

        using autocrat::object_scanner::scan;
    protected:
        void on_field(void** field) override final
        {
            void* object = *field;
            if (object != nullptr)
            {
                auto it = std::find(_objects_begin, _objects_end, object);
                if (it != _objects_end)
                {
                    _map->emplace(std::distance(_objects_begin, it), field);
                }
            }
        }

        void on_object(void*, std::size_t) override final
        {
        }
    private:
        task_context::workers_field_map* _map;
        void* const* _objects_begin;
        void* const* _objects_end;
    };

    delegate_info create_delegate_info(managed_delegate* delegate)
    {
        if (delegate->method_ptr != nullptr)
        {
            return delegate_info{ delegate->method_ptr, nullptr };
        }
        else
        {
            return delegate_info{ delegate->method_ptr_aux, delegate->target };
        }
    }

    void update_workers(
        const task_context& context,
        const autocrat::worker_service::object_collection& objects)
    {
        // Update the workers to their new locations (the list returned by
        // try_lock is in the same order as the workers we passed in)
        std::size_t count = objects.size();
        for (std::size_t i = 0; i != count; ++i)
        {
            void* worker = objects[i];
            auto range = context.worker_fields.equal_range(i);
            for (auto it = range.first; it != range.second; ++it)
            {
                void** field = it->second;
                *field = worker;
            }
        }
    }

    template <class... Args>
    void invoke_delegate(void* method, void* target, Args... args)
    {
        if (target == nullptr)
        {
            (reinterpret_cast<void(*)(Args...)>(method))(args...);
        }
        else
        {
            (reinterpret_cast<void(*)(void*, Args...)>(method))(target, args...);
        }
    }

    void invoke_action(std::any& data)
    {
        auto delegate = std::any_cast<delegate_info>(data);
        invoke_delegate(delegate.method, delegate.target);
    }

    void invoke_send_or_post_callback(std::any& data)
    {
        auto context = std::any_cast<std::shared_ptr<task_context>>(data);
        auto gc = autocrat::global_services.get_service<autocrat::gc_service>();
        auto workers = autocrat::global_services.get_service<autocrat::worker_service>();

        gc->set_heap(std::move(context->heap));
        auto objects = workers->try_lock(context->workers);
        if (!objects)
        {
            // We couldn't lock everything, so queue the work again
            context->heap = gc->reset_heap();
            context->thread_pool->enqueue(invoke_send_or_post_callback, std::move(data));
        }
        else
        {
            update_workers(*context, *objects);
            invoke_delegate(context->delegate.method, context->delegate.target, context->state);
        }
    }
}

namespace autocrat
{
    task_service::task_service(thread_pool* pool) :
        _thread_pool(pool)
    {
    }

    void task_service::enqueue(managed_delegate* callback, void* state)
    {
        auto context = std::make_shared<task_context>();
        context->delegate = create_delegate_info(callback);
        context->state = state;
        context->thread_pool = _thread_pool;

        worker_service::object_collection objects;
        std::tie(objects, context->workers) =
            global_services.get_service<worker_service>()->release_locked();

        // The state will be the only thing the new task will have access to,
        // therefore, it acts as the root object
        worker_field_scanner scanner(context->worker_fields, objects);
        scanner.scan(state);

        context->heap = global_services.get_service<gc_service>()->reset_heap();
        _thread_pool->enqueue(invoke_send_or_post_callback, std::move(context));
    }

    void task_service::start_new(managed_delegate* action)
    {
        // No need to save any context here as it's a new action
        _thread_pool->enqueue(invoke_action, create_delegate_info(action));
    }
}

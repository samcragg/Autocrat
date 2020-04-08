#include "task_service.h"

#include <gtest/gtest.h>
#include <cpp_mock.h>
#include "managed_types.h"
#include "ManagedObjects.h"
#include "mock_services.h"
#include "TestMocks.h"

namespace
{
    std::size_t action_call_count;
    void* action_state;

    class gc_object
    {
    public:
        gc_object()
        {
            _object.m_pEEType = &SingleReferenceInfo.Type;
        }

        SingleReference* managed_object()
        {
            return &_object;
        }
    private:
        char _gc_header[8u] = {};
        SingleReference _object = {};
    };

    struct simple_instance
    {
        std::size_t call_count;
    };


    void instance_action(simple_instance* instance)
    {
        instance->call_count++;
    }

    void save_state(void* state)
    {
        action_state = state;
    }

    void simple_action()
    {
        action_call_count++;
    }
}

class TaskServiceTests : public testing::Test
{
protected:
    TaskServiceTests() :
        _task_service(&_thread_pool)
    {
    }

    FakeThreadPool _thread_pool;
    autocrat::task_service _task_service;
};

TEST_F(TaskServiceTests, EnqueueShouldRetryIfWorkersAreLocked)
{
    mock_global_services.worker_service().is_locked = true;

    managed_delegate delegate = {};
    delegate.method_ptr = reinterpret_cast<void*>(&save_state);

    _task_service.enqueue(&delegate, nullptr);

    EXPECT_EQ(2u, _thread_pool.enqueue_count);
}

TEST_F(TaskServiceTests, EnqueueShouldScanTheStateForWorkers)
{
    gc_object locked_worker;
    gc_object original_worker;
    gc_object state;
    state.managed_object()->Reference = original_worker.managed_object();
    mock_global_services.worker_service().locked_worker = locked_worker.managed_object();
    mock_global_services.worker_service().original_worker = original_worker.managed_object();

    managed_delegate delegate = {};
    delegate.method_ptr = reinterpret_cast<void*>(&save_state);

    action_state = nullptr;
    _task_service.enqueue(&delegate, state.managed_object());

    EXPECT_EQ(state.managed_object(), action_state);
    EXPECT_EQ(locked_worker.managed_object(), state.managed_object()->Reference);
}

TEST_F(TaskServiceTests, StartNewShouldRunInstanceTasksOnTheThreadPool)
{
    simple_instance object = {};
    managed_delegate delegate = {};
    delegate.method_ptr_aux = reinterpret_cast<void*>(&instance_action);
    delegate.target = &object;

    _task_service.start_new(&delegate);

    EXPECT_EQ(1u, object.call_count);
}

TEST_F(TaskServiceTests, StartNewShouldRunStaticTasksOnTheThreadPool)
{
    managed_delegate delegate = {};
    delegate.method_ptr = reinterpret_cast<void*>(&simple_action);

    action_call_count = 0u;
    _task_service.start_new(&delegate);

    EXPECT_EQ(1u, action_call_count);
}

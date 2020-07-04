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
    ManagedObject<SingleReference> locked_worker;
    ManagedObject<SingleReference> original_worker;
    ManagedObject<SingleReference> state;
    state->Reference = original_worker.get();
    mock_global_services.worker_service().locked_worker = locked_worker.get();
    mock_global_services.worker_service().original_worker = original_worker.get();

    managed_delegate delegate = {};
    delegate.method_ptr = reinterpret_cast<void*>(&save_state);

    action_state = nullptr;
    _task_service.enqueue(&delegate, state.get());

    EXPECT_EQ(state.get(), action_state);
    EXPECT_EQ(locked_worker.get(), state->Reference);
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

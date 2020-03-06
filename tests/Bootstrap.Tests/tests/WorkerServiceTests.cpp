#include "worker_service.h"

#include <memory>
#include <vector>
#include <gtest/gtest.h>
#include <cpp_mock.h>
#include "ManagedObjects.h"
#include "mock_services.h"
#include "TestMocks.h"

namespace
{
    std::unique_ptr<std::byte[]> managed_worker;

    void* create_worker()
    {
        return managed_worker.get();
    }
}

class WorkerServiceTests : public testing::Test
{
protected:
    WorkerServiceTests() :
        _service(&_thread_pool),
        _worker_id("id"),
        _worker_type(0)
    {
        _service.on_begin_work(0u);

        When(mock_global_services.gc_service().allocate)
            .Do([](std::size_t size)
                {
                    managed_worker = std::make_unique<std::byte[]>(size);
                    return managed_worker.get();
                });
    }

    ~WorkerServiceTests()
    {
        _service.on_end_work(0u);
        managed_worker.reset();
    }

    FakeThreadPool _thread_pool;
    autocrat::worker_service _service;
    std::string_view _worker_id;
    int _worker_type;
};

TEST_F(WorkerServiceTests, ShouldReturnTheExistingWorker)
{
    managed_worker = std::make_unique<std::byte[]>(sizeof(Object));
    reinterpret_cast<Object*>(managed_worker.get())->m_pEEType = &object_type;
    _service.register_type(&_worker_type, &create_worker);

    void* first = _service.get_worker(&_worker_type, _worker_id);
    void* second = _service.get_worker(&_worker_type, _worker_id);

    EXPECT_EQ(first, second);
}

TEST_F(WorkerServiceTests, ShouldSaveAndRestoreTheWorkerState)
{
    managed_worker = std::make_unique<std::byte[]>(sizeof(BaseClass));
    auto base_class = reinterpret_cast<BaseClass*>(managed_worker.get());
    base_class->m_pEEType = &BaseClassInfo.Type;
    base_class->BaseInteger = 123;

    _service.register_type(&_worker_type, &create_worker);
    void* object = _service.get_worker(&_worker_type, _worker_id);
    EXPECT_EQ(base_class, object);

    // Reset the service to force it to save the worker. We'll also prevent
    // just creating a new worker by resetting the worker we created at the
    // start of the test
    _service.on_end_work(0u);
    managed_worker.reset();
    _service.on_begin_work(0u);

    object = _service.get_worker(&_worker_type, _worker_id);
    base_class = reinterpret_cast<BaseClass*>(object);
    EXPECT_EQ(&BaseClassInfo.Type, base_class->m_pEEType);
    EXPECT_EQ(123, base_class->BaseInteger);
}

TEST_F(WorkerServiceTests, ShouldThrowIfTheConstructorIsNotRegistered)
{
    EXPECT_THROW(_service.get_worker(&_worker_type, _worker_id), std::invalid_argument);
}

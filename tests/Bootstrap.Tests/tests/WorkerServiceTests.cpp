#include "worker_service.h"

#include <algorithm>
#include <atomic>
#include <memory>
#include <mutex>
#include <thread>
#include <vector>
#include <gtest/gtest.h>
#include <cpp_mock.h>
#include "ManagedObjects.h"
#include "mock_services.h"
#include "TestMocks.h"

namespace
{
    std::unique_ptr<ManagedObject<BaseClass>> worker_class;
    std::unique_ptr<ManagedObject<SingleReference>> worker_object;

    void* create_worker_class()
    {
        worker_class = std::make_unique<ManagedObject<BaseClass>>();
        (*worker_class)->BaseInteger = 123;
        return worker_class->get();
    }

    void* create_worker_object()
    {
        worker_object = std::make_unique<ManagedObject<SingleReference>>();
        return worker_object->get();
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
        _service.pool_created(2u);
        _service.begin_work(0u);

        When(mock_global_services.gc_service().allocate)
            .Do([this](std::size_t size)
                {
                    _allocated_bytes = std::make_unique<std::byte[]>(size);
                    return _allocated_bytes.get();
                });
    }

    ~WorkerServiceTests()
    {
        _service.end_work(0u);
        worker_class.reset();
        worker_object.reset();
    }

    void AssertWorkerLocked(const void* type, bool expected)
    {
        void* worker = nullptr;
        std::thread thread([&]()
            {
                _service.begin_work(1u);
                worker = _service.get_worker(type, _worker_id);
                _service.end_work(1u);
            });
        thread.join();

        // If the worker is locked, the other thread wouldn't have been able to
        // get it
        bool is_locked = worker == nullptr;
        EXPECT_EQ(expected, is_locked);
    }

    std::unique_ptr<std::byte[]> _allocated_bytes;
    FakeThreadPool _thread_pool;
    autocrat::worker_service _service;
    std::string_view _worker_id;
    int _worker_type;
};

TEST_F(WorkerServiceTests, GetWorkerShouldReturnTheExistingWorker)
{
    _service.register_type(&_worker_type, &create_worker_object);

    void* first = _service.get_worker(&_worker_type, _worker_id);
    void* second = _service.get_worker(&_worker_type, _worker_id);

    EXPECT_EQ(first, second);
}

TEST_F(WorkerServiceTests, GetWorkerShouldThrowIfTheConstructorIsNotRegistered)
{
    EXPECT_THROW(_service.get_worker(&_worker_type, _worker_id), std::invalid_argument);
}

TEST_F(WorkerServiceTests, ReleaseLockedShouldReturnAllLockedWorkers)
{
    _service.register_type(&worker_class, &create_worker_class);
    _service.register_type(&worker_object, &create_worker_object);

    // Lock the workers (including double locking, which is supported)
    void* class_ptr = _service.get_worker(&worker_class, _worker_id);
    void* object_ptr = _service.get_worker(&worker_object, _worker_id);
    _service.get_worker(&worker_object, _worker_id); // Lock again

    auto [objects, workers] = _service.release_locked();

    EXPECT_EQ(2u, objects.size());
    EXPECT_EQ(2u, workers.size());
    EXPECT_NE(objects.end(), std::find(objects.begin(), objects.end(), class_ptr));
    EXPECT_NE(objects.end(), std::find(objects.begin(), objects.end(), object_ptr));
}

TEST_F(WorkerServiceTests, ShouldSaveAndRestoreTheWorkerState)
{
    _service.register_type(&_worker_type, &create_worker_class);
    void* object = _service.get_worker(&_worker_type, _worker_id);
    EXPECT_EQ(worker_class->get(), object);

    // Reset the service to force it to save the worker
    _service.end_work(0u);
    _service.begin_work(0u);

    object = _service.get_worker(&_worker_type, _worker_id);
    EXPECT_EQ(_allocated_bytes.get(), object);

    auto base_class = reinterpret_cast<BaseClass*>(object);
    EXPECT_EQ(123, base_class->BaseInteger);
}

TEST_F(WorkerServiceTests, TryLockShouldLockAndReturnTheManagedObjects)
{
    _service.register_type(&_worker_type, &create_worker_object);
    _service.get_worker(&_worker_type, _worker_id);
    auto workers = std::get<autocrat::worker_service::worker_collection>(_service.release_locked());

    AssertWorkerLocked(&_worker_type, false);
    auto result = _service.try_lock(workers);

    EXPECT_TRUE(result.has_value());
    EXPECT_EQ(1u, result->size());
    EXPECT_EQ(_allocated_bytes.get(), result->data()[0]);
    AssertWorkerLocked(&_worker_type, true);
}

TEST_F(WorkerServiceTests, TryLockShouldReturnEmptyIfTheObjectIsLocked)
{
    _service.register_type(&_worker_type, &create_worker_object);
    _service.get_worker(&_worker_type, _worker_id);
    auto workers = std::get<autocrat::worker_service::worker_collection>(_service.release_locked());

    // Cause another thread to lock the worker
    std::mutex mutex;
    std::unique_lock<std::mutex> lock(mutex);
    std::atomic_bool worker_locked = false;
    std::thread lock_worker([&]()
        {
            _service.begin_work(1u);
            _service.get_worker(&_worker_type, _worker_id);
            worker_locked = true;

            // Wait for the test to finish so we know to exit
            std::unique_lock<std::mutex> wait_for_end_of_test(mutex);
            _service.end_work(1u);
        });

    while (!worker_locked)
    {
        std::this_thread::yield();
    }

    auto result = _service.try_lock(workers);
    EXPECT_FALSE(result.has_value());

    lock.unlock();
    lock_worker.join();
}

#include "worker_service.h"

#include <gtest/gtest.h>
#include <memory>
#include <vector>

namespace
{
    std::vector<std::unique_ptr<int>> constructed_workers;

    void* create_worker()
    {
        auto& worker = constructed_workers.emplace_back(new int());
        return worker.get();
    }
}

class WorkerServiceTests : public testing::Test
{
protected:
    WorkerServiceTests() :
        _service(nullptr),
        _worker_type(0)
    {
    }

    ~WorkerServiceTests()
    {
        constructed_workers.clear();
    }

    autocrat::worker_service _service;
    int _worker_type;
};

TEST_F(WorkerServiceTests, ShouldReturnAlreadyConstructedServices)
{
    EXPECT_TRUE(constructed_workers.empty());

    _service.register_type(&_worker_type, &create_worker);
    void* first = _service.get_worker(&_worker_type);
    void* second = _service.get_worker(&_worker_type);

    EXPECT_EQ(1u, constructed_workers.size());
    EXPECT_EQ(first, second);
}

TEST_F(WorkerServiceTests, ShouldThrowIfTheConstructorIsNotRegistered)
{
    EXPECT_THROW(_service.get_worker(&_worker_type), std::invalid_argument);
}

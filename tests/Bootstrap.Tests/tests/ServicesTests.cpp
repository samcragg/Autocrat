#include "services.h"

#include <gtest/gtest.h>
#include <cpp_mock.h>

struct MockThreadPool
{
    MockThreadPool(int value) :
        constructor_value(value)
    {
    }

    int constructor_value;
};

struct MockService
{
    MockService(MockThreadPool* pool) :
        thread_pool(pool)
    {
    }

    MockMethod(void, check_and_dispatch, (), )

    MockThreadPool* thread_pool;
};

class ServicesTests : public testing::Test
{
protected:
    autocrat::services<MockThreadPool, MockService> _services;
};

TEST_F(ServicesTests, CheckAndDispatchShouldCallTheServiceMethod)
{
    _services.initialize();

    _services.check_and_dispatch();

    Verify(_services.get_service<MockService>()->check_and_dispatch);
}

TEST_F(ServicesTests, InitializeShouldCreateNewInstances)
{
    _services.initialize_thread_pool(123);
    _services.initialize();

    MockService* instance = _services.get_service<MockService>();

    EXPECT_NE(nullptr, instance);
    EXPECT_EQ(123, instance->thread_pool->constructor_value);
}

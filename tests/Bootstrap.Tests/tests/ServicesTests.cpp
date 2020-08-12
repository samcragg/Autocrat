#include "services.h"

#include <gtest/gtest.h>
#include <cpp_mock.h>

struct MockThreadPool
{
    static std::unique_ptr<MockThreadPool> make_unique()
    {
        return std::make_unique<MockThreadPool>();
    }

    MockMethod(void, add_observer, (autocrat::lifetime_service*), )
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

struct MockLifetimeService : autocrat::lifetime_service
{
    MockLifetimeService(MockThreadPool* pool) :
        thread_pool(pool)
    {
    }

    MockMethod(void, begin_work, (std::size_t))
    MockMethod(void, end_work, (std::size_t))

    MockThreadPool* thread_pool;
};

class ServicesTests : public testing::Test
{
protected:
    autocrat::services<MockThreadPool, MockService, MockLifetimeService> _services;
};

TEST_F(ServicesTests, CheckAndDispatchShouldCallTheServiceMethod)
{
    _services.initialize();

    _services.check_and_dispatch();

    Verify(_services.get_service<MockService>()->check_and_dispatch);
}

TEST_F(ServicesTests, InitializeShouldCreateNewInstances)
{
    _services.initialize();

    MockService* instance = _services.get_service<MockService>();

    EXPECT_NE(nullptr, instance);
    EXPECT_NE(nullptr, instance->thread_pool);
}

TEST_F(ServicesTests, InitializeShouldSubscribeLifetimeServices)
{
    _services.initialize();

    MockLifetimeService* lifetime = _services.get_service<MockLifetimeService>();

    Verify(lifetime->thread_pool->add_observer).With(lifetime);
}

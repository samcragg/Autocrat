#ifndef TEST_MOCKS_H
#define TEST_MOCKS_H

#include "thread_pool.h"

class FakeThreadPool : public autocrat::thread_pool
{
public:
    FakeThreadPool() : autocrat::thread_pool(0u, 0u)
    {
    }

    void enqueue(callback_function callback, std::any&& data) override
    {
        callback(data);
    }

    std::size_t size() const noexcept override
    {
        return 2u;
    }
};

#endif

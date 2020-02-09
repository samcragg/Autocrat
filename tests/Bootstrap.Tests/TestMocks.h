#ifndef TEST_MOCKS_H
#define TEST_MOCKS_H

#include "thread_pool.h"

class MockThreadPool : public autocrat::thread_pool
{
public:
    void enqueue(callback_function callback, std::any&& data) override
    {
        callback(data);
    }

    std::size_t size() const noexcept override
    {
        return 1u;
    }
};

#endif

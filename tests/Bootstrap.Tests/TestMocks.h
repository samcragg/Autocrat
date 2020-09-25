#ifndef TEST_MOCKS_H
#define TEST_MOCKS_H

#include "thread_pool.h"

class FakeThreadPool : public autocrat::thread_pool
{
public:
    void enqueue(callback_function callback, std::any&& data) override
    {
        enqueue_count++;
        callback(data);
    }

    std::size_t size() const noexcept override
    {
        return 2u;
    }

    std::size_t enqueue_count = 0u;
};

#endif

#include "gc_service.h"

#include <algorithm>
#include <unordered_set>
#include <gtest/gtest.h>
#include "MemoryMonitor.h"
#include "TestMocks.h"

namespace
{
    void CheckAllocation(autocrat::gc_service& gc, std::size_t size)
    {
        // Repeat a few times to check when memory is recycled
        for (unsigned i = 0; i != 3; ++i)
        {
            gc.begin_work(0u);
            auto memory = static_cast<std::byte*>(gc.allocate(size));

            EXPECT_TRUE(std::all_of(memory, memory + size, [](auto b) { return b == std::byte{}; }));

            // Dirty the memory to see if it gets returned
            std::fill_n(memory, size, std::byte{ 255u });
            gc.end_work(0u);
        }

    }
}

class GcServiceTests : public testing::Test
{
protected:
    GcServiceTests() :
        _gc(&_threadPool)
    {
        _gc.pool_created(1u);
    }

    static constexpr std::size_t large_allocation = 200'000u;
    static constexpr std::size_t small_allocation = 192u;
    static_assert(small_allocation % 16u == 0, "Must be divisible by 16");

    FakeThreadPool _threadPool;
    autocrat::gc_service _gc;
};

TEST_F(GcServiceTests, AllocateShouldAllocateMultipleSmallBuffers)
{
    constexpr std::size_t allocation_count = 100u;
    _gc.begin_work(0u);

    std::unordered_set<void*> allocations;
    for (std::size_t i = 0; i != allocation_count; ++i)
    {
        allocations.insert(_gc.allocate(20'000));
    }

    EXPECT_EQ(allocation_count, allocations.size());
}

TEST_F(GcServiceTests, AllocateShouldZeroFillLargeBuffers)
{
    CheckAllocation(_gc, large_allocation);
}

TEST_F(GcServiceTests, AllocateShouldZeroFillSmallBuffers)
{
    CheckAllocation(_gc, small_allocation);
}

TEST_F(GcServiceTests, DestructorShouldReleaseAllTheMemory)
{
    {
        // The GC heap is allocated from a global store - preallocate some
        // memory that can be re-used later
        autocrat::gc_service gc(&_threadPool);
        gc.pool_created(1u);
    }

    std::size_t before_bytes = allocated_bytes();
    {
        autocrat::gc_service gc(&_threadPool);
        gc.pool_created(1u);
        gc.begin_work(0);
        EXPECT_NE(nullptr, gc.allocate(small_allocation));
        EXPECT_NE(nullptr, gc.allocate(large_allocation));
    }
    std::size_t after_bytes = allocated_bytes();
    EXPECT_EQ(before_bytes, after_bytes);
}

TEST_F(GcServiceTests, OnEndWorkShouldReleaseAllTheMemory)
{
    std::size_t before_bytes = allocated_bytes();

    _gc.begin_work(0);
    EXPECT_NE(nullptr, _gc.allocate(small_allocation));
    EXPECT_NE(nullptr, _gc.allocate(large_allocation));

    _gc.end_work(0);
    std::size_t after_bytes = allocated_bytes();
    EXPECT_EQ(before_bytes, after_bytes);
}

TEST_F(GcServiceTests, ShouldBeAbleToUseDifferentHeaps)
{
    _gc.begin_work(0);

    auto first = static_cast<std::byte*>(_gc.allocate(small_allocation));
    auto second = static_cast<std::byte*>(_gc.allocate(small_allocation));
    EXPECT_EQ(first + small_allocation, second);

    autocrat::gc_heap heap = _gc.reset_heap();
    auto new_heap = static_cast<std::byte*>(_gc.allocate(small_allocation));
    EXPECT_NE(second + small_allocation, new_heap);

    _gc.set_heap(std::move(heap));
    auto original_heap = static_cast<std::byte*>(_gc.allocate(small_allocation));
    EXPECT_EQ(second + small_allocation, original_heap);
}

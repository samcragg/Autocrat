#include "smart_ptr.h"

#include <algorithm>
#include <gtest/gtest.h>

class simple_object : public autocrat::intrusive_ref_counter<simple_object>
{
public:
    explicit simple_object(int* destructor_called_count) :
        _destructor_called_count(destructor_called_count)
    {
    }

    ~simple_object()
    {
        (*_destructor_called_count)++;
    }
private:
    int* _destructor_called_count;
};

class SmartPtrTests : public testing::Test
{
protected:
    using intrusive_ptr = autocrat::intrusive_ptr<simple_object>;
};

TEST_F(SmartPtrTests, CopyAssignmentShouldBeEqualToTheOriginal)
{
    int destructor_called_count = 0;
    {
        intrusive_ptr original(new simple_object(&destructor_called_count));

        intrusive_ptr copy;
        copy = original;

        EXPECT_EQ(original.get(), copy.get());
    }
    EXPECT_EQ(1, destructor_called_count);
}

TEST_F(SmartPtrTests, CopyConstructorShouldBeEqualToTheOriginal)
{
    int destructor_called_count = 0;
    {
        intrusive_ptr original(new simple_object(&destructor_called_count));

        intrusive_ptr copy(original);

        EXPECT_EQ(original.get(), copy.get());
    }
    EXPECT_EQ(1, destructor_called_count);
}

TEST_F(SmartPtrTests, MoveAssignmentShouldClearTheOriginal)
{
    int destructor_called_count = 0;
    {
        intrusive_ptr original(new simple_object(&destructor_called_count));

        intrusive_ptr moved;
        moved = std::move(original);

        EXPECT_EQ(nullptr, original.get());
        EXPECT_NE(nullptr, moved.get());
    }
    EXPECT_EQ(1, destructor_called_count);
}

TEST_F(SmartPtrTests, MoveConstructorShouldClearTheOriginal)
{
    int destructor_called_count = 0;
    {
        intrusive_ptr original(new simple_object(&destructor_called_count));

        intrusive_ptr moved(std::move(original));

        EXPECT_EQ(nullptr, original.get());
        EXPECT_NE(nullptr, moved.get());
    }
    EXPECT_EQ(1, destructor_called_count);
}

TEST_F(SmartPtrTests, ShouldConvertToBool)
{
    intrusive_ptr empty;
    EXPECT_FALSE(empty);

    int ignored = 0;
    intrusive_ptr non_empty(new simple_object(&ignored));
    EXPECT_TRUE(non_empty);
}

TEST_F(SmartPtrTests, ShouldReleaseTheObjectWhenThereAreNoReferences)
{
    int destructor_called_count = 0;
    {
        simple_object* instance = new simple_object(&destructor_called_count);
        intrusive_ptr outer(instance);
        {
            intrusive_ptr inner(instance);
            EXPECT_EQ(0, destructor_called_count);
        }
        EXPECT_EQ(0, destructor_called_count);
    }
    EXPECT_EQ(1, destructor_called_count);
}

TEST_F(SmartPtrTests, SwapShouldExchangeTheContents)
{
    int destructor_called_count = 0;
    {
        intrusive_ptr original(new simple_object(&destructor_called_count));

        intrusive_ptr swapped;
        swap(original, swapped);

        EXPECT_EQ(nullptr, original.get());
        EXPECT_NE(nullptr, swapped.get());
    }
    EXPECT_EQ(1, destructor_called_count);
}

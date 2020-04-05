#include "managed_interop.h"

#include <unordered_set>
#include <tuple>
#include <vector>
#include <gtest/gtest.h>
#include "ManagedObjects.h"

class FakeScanner : public autocrat::object_scanner
{
public:
    std::unordered_set<void*> fields;
    std::vector<std::tuple<void*, std::size_t>> objects;
protected:
    void on_field(void* field) override
    {
        bool inserted = fields.insert(field).second;
        EXPECT_TRUE(inserted);
    }

    void on_object(void* object, std::size_t size) override
    {
        objects.push_back({ object, size });
    }
};

class ObjectScannerTests : public testing::Test
{
protected:
    FakeScanner _scanner;
};

TEST_F(ObjectScannerTests, ScanShouldHandleCyclicGraphs)
{
    SingleReference object = {};
    object.m_pEEType = &SingleReferenceInfo.Type;
    object.Reference = &object;

    _scanner.scan(&object);
    EXPECT_EQ(1u, _scanner.objects.size());

    // Check we can call it multiple times (i.e. that if it marks something as
    // scanned we can still scan it again)
    _scanner.fields.clear();
    _scanner.scan(&object);
    EXPECT_EQ(2u, _scanner.objects.size());
}

TEST_F(ObjectScannerTests, ScanShouldIncludeTheObjectSize)
{
    DerivedClass object = {};
    object.m_pEEType = &DerivedClassInfo.Type;

    _scanner.scan(&object);

    EXPECT_EQ(1u, _scanner.objects.size());
    EXPECT_EQ(&object, std::get<void*>(_scanner.objects[0]));
    EXPECT_EQ(sizeof(DerivedClass), std::get<std::size_t>(_scanner.objects[0]));
}

TEST_F(ObjectScannerTests, ScanShouldScanAllReferenceFields)
{
    DerivedClass object = {};
    object.m_pEEType = &DerivedClassInfo.Type;

    _scanner.scan(&object);

    EXPECT_EQ(3u, _scanner.fields.size());
    EXPECT_NE(_scanner.fields.end(), _scanner.fields.find(&object.BaseReference));
    EXPECT_NE(_scanner.fields.end(), _scanner.fields.find(&object.FirstReference));
    EXPECT_NE(_scanner.fields.end(), _scanner.fields.find(&object.SecondReference));
}

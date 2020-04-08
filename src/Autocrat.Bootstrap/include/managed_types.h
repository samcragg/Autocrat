#ifndef MANAGED_TYPES_H
#define MANAGED_TYPES_H

#include <cstddef>
#include <cstdint>

struct managed_delegate
{
    void* ee_type;
    void* target;
    void* method_base;
    void* method_ptr;
    void* method_ptr_aux;
};

struct managed_guid
{
    std::byte data[16];
};

struct managed_string
{
    void* ee_type;
    std::uint32_t length;
    char16_t data[1];

};

// Check no padding has been added
static_assert(offsetof(managed_string, length) == 8u);
static_assert(offsetof(managed_string, data) == 12u);


struct typed_reference
{
    void* value;
    void* type;
};

#endif

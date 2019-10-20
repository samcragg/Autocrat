#ifndef MOCK_METHOD_HANDLES_H
#define MOCK_METHOD_HANDLES_H

#include <unordered_map>
#include "method_handles.h"

class method_registration
{
public:
    ~method_registration() noexcept;

    static method_registration register_method(method_types&& method);

    std::int32_t index() const noexcept;
private:
    friend method_types& get_known_method(std::size_t index);

    explicit method_registration(std::int32_t index);

    static std::int32_t counter;
    static std::unordered_map<std::int32_t, method_types> registrations;
    std::int32_t _index;
};

#endif

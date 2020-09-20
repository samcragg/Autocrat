#include "mock_exports.h"

std::int32_t method_registration::counter = 0;
std::unordered_map<std::int32_t, method_types> method_registration::registrations;

method_registration::method_registration(std::int32_t index) :
    _index(index)
{
}

method_registration::~method_registration() noexcept
{
    registrations.erase(_index);
}

method_registration method_registration::register_method(method_types&& method)
{
    std::int32_t index = ++counter;
    registrations.insert({ index, std::move(method) });
    return method_registration(index);
}

std::int32_t method_registration::index() const noexcept
{
    return _index;
}

method_types& get_known_method(std::size_t index)
{
    return method_registration::registrations[static_cast<std::int32_t>(index)];
}

#ifndef METHOD_HANDLES_H
#define METHOD_HANDLES_H

#include <cstddef>
#include <cstdint>
#include <variant>

using udp_register_method = void (*)(std::int32_t, void*);

using method_types = std::variant<
    udp_register_method
>;

extern method_types& get_known_method(std::size_t index);

#endif

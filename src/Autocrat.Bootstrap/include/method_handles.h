#ifndef METHOD_HANDLES_H
#define METHOD_HANDLES_H

// This file is also shared with the generated code file (i.e. gets included
// in the NuGet package)

#include <cstddef>
#include <cstdint>
#include <variant>

using timer_method = void (*)(std::int32_t);
using udp_data_received_method = void (*)(std::int32_t, const void*);

using method_types = std::variant<
    timer_method,
    udp_data_received_method
>;

extern method_types& get_known_method(std::size_t index);

#endif

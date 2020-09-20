#ifndef EXPORTS_H
#define EXPORTS_H

// This file is also shared with the generated code file (i.e. gets included
// in the NuGet package)

#include <cstddef>
#include <cstdint>
#include <variant>

// Returns the managed object
using construct_worker = void* (*)();

// Accepts the timer handle
// Returns a Task
using timer_method = void* (*)(std::int32_t);

// Accepts the port number and byte array
// Returns a Task
using udp_data_received_method = void* (*)(std::int32_t, const void*);

using method_types =
    std::variant<construct_worker, timer_method, udp_data_received_method>;

// Supplied by the library
extern int autocrat_main(int argc, char* argv[]);

// Supplied by the generated code
extern method_types& get_known_method(std::size_t index);

// Supplied by the library
extern void set_description(const char*);

// Supplied by the library
extern void set_version(const char*);

#endif

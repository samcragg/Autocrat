#ifndef MOCK_SERVICES_H
#define MOCK_SERVICES_H

#include "services.h"
#include <cpp_mock.h>

class mock_network_service : public autocrat::network_service
{
public:
    MockMethod(void, add_udp_callback, (std::uint16_t, udp_register_method))
    MockMethod(void, check_and_dispatch, ())
};

class mock_services : public autocrat::global_services_type
{
public:
    void create_services()
    {
        _services = std::make_tuple(
            std::make_unique<mock_network_service>()
        );
    }

    void release_services()
    {
        _services = {};
    }

    mock_network_service& network_service()
    {
        return *dynamic_cast<mock_network_service*>(
            get_service<autocrat::network_service>());
    }
};

extern mock_services mock_global_services;

#endif

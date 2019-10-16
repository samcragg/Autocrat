#include "managed_exports.h"
#include <spdlog/spdlog.h>
#include <spdlog/async.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <cstdio>

namespace
{
    void initialize_logging()
    {
        try
        {
            auto logger = spdlog::create_async_nb<spdlog::sinks::stdout_color_sink_mt>("console");
            spdlog::set_default_logger(logger);
        }
        catch (const spdlog::spdlog_ex& ex)
        {
            std::fprintf(stderr, "Unable to initialize logging: %s\n", ex.what());
            std::exit(1);
        }
    }
}

int autocrat_main()
{
    initialize_logging();

    spdlog::info("Loading configuration");
    managed_exports::OnConfigurationLoaded();

    spdlog::info("Exiting");
    spdlog::shutdown();
    return 0;
}

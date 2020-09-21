#include "application.h"
#include "services.h"
#include <cstdio>
#include <spdlog/async.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/spdlog.h>

namespace autocrat
{

global_services_type global_services;

}

namespace
{

autocrat::application application;

void initialize_logging()
{
    try
    {
        auto logger =
            spdlog::create_async_nb<spdlog::sinks::stdout_color_sink_mt>(
                "console");
        spdlog::set_default_logger(logger);
    }
    catch (const spdlog::spdlog_ex& ex)
    {
        std::fprintf(stderr, "Unable to initialize logging: %s\n", ex.what());
        std::exit(1);
    }
}

void on_close_callback()
{
    application.stop();
}

}

int autocrat_main(int argc, char* argv[])
{
    initialize_logging();
    try
    {
        application.initialize(argc, argv);
        spdlog::info("Initialization complete, program started");

        pal::set_close_signal_handler(&on_close_callback);
        std::printf("Press ctrl-c to exit\n");
        application.run();
    }
    catch (const std::exception& ex)
    {
        spdlog::error("Unexpected exception, exiting. {}", ex.what());
    }

    spdlog::info("Exiting");
    spdlog::shutdown();
    return 0;
}

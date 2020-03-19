#include <atomic>
#include <cstdio>
#include <thread>
#include <spdlog/spdlog.h>
#include <spdlog/async.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include "managed_exports.h"
#include "pal.h"
#include "pause.h"
#include "services.h"

namespace autocrat
{
    global_services_type global_services;
}

namespace
{
    std::atomic_bool program_running;

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

    void on_close_callback()
    {
        program_running = false;
    }

    void run_program_loop()
    {
        while (program_running)
        {
            autocrat::global_services.check_and_dispatch();
            autocrat::pause();
        }
    }

    void setup_services()
    {
        autocrat::global_services.initialize_thread_pool(0, 1);
        autocrat::global_services.initialize();
    }
}

int autocrat_main()
{
    initialize_logging();
    setup_services();

    spdlog::debug("Registering worker type constructors");
    managed_exports::RegisterWorkerTypes();

    spdlog::info("Loading configuration");
    managed_exports::OnConfigurationLoaded();

    program_running = true;
    spdlog::info("Initialization complete, program started");

    pal::set_close_signal_handler(&on_close_callback);
    std::printf("Press ctrl-c to exit\n");
    run_program_loop();

    spdlog::info("Exiting");
    spdlog::shutdown();
    return 0;
}

#include "managed_exports.h"
#include "pal.h"
#include "pause.h"
#include "services.h"
#include "thread_pool.h"
#include <atomic>
#include <cstdio>
#include <spdlog/async.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/spdlog.h>
#include <thread>

namespace autocrat
{

global_services_type global_services;

}

namespace
{

std::atomic_bool program_running;
autocrat::gc_heap global_heap;

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

void initialize_managed_thread(autocrat::gc_service* gc)
{
    gc->set_heap(std::move(global_heap));
    managed_exports::InitializeManagedThread();
    global_heap = gc->reset_heap();
}

void initialize_managed_thread(std::size_t thread_id)
{
    auto* gc = autocrat::global_services.get_service<autocrat::gc_service>();
    gc->begin_work(thread_id);
    initialize_managed_thread(gc);
    gc->end_work(thread_id);
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

void setup_threads()
{
    auto* gc = autocrat::global_services.get_service<autocrat::gc_service>();
    gc->begin_work(autocrat::lifetime_service::global_thread_id);
    global_heap = gc->reset_heap();
    initialize_managed_thread(gc); // Initialize the current thread

    autocrat::global_services.get_thread_pool().start(
        initialize_managed_thread);
}

void shutdown()
{
    auto* gc = autocrat::global_services.get_service<autocrat::gc_service>();
    gc->set_heap(std::move(global_heap));
    gc->end_work(autocrat::lifetime_service::global_thread_id);

    spdlog::shutdown();
}

}

int autocrat_main()
{
    initialize_logging();

    spdlog::debug("Creating native services");
    setup_services();

    spdlog::debug("Setting up native/manage transition for threads");
    setup_threads();

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
    shutdown();
    return 0;
}

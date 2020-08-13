#include "application.h"
#include "managed_exports.h"
#include "pause.h"
#include "services.h"
#include <spdlog/spdlog.h>

namespace autocrat
{

void application::initialize()
{
    spdlog::debug("Creating native services");
    autocrat::global_services.initialize();

    spdlog::debug("Setting up native/manage transition for threads");
    initialize_threads();

    spdlog::debug("Registering worker type constructors");
    managed_exports::RegisterWorkerTypes();

    spdlog::info("Loading configuration");
    // TODO: Load the configuration file
    managed_exports::OnConfigurationLoaded();
}

void application::run()
{
    _running = true;
    do
    {
        global_services.check_and_dispatch();
        pause();
    } while (_running);

    auto* gc = autocrat::global_services.get_service<autocrat::gc_service>();
    gc->set_heap(std::move(_global_heap));
    gc->end_work(autocrat::lifetime_service::global_thread_id);
}

void application::stop()
{
    _running = false;
}

void application::initialize_managed_thread(autocrat::gc_service* gc)
{
    gc->set_heap(std::move(_global_heap));
    managed_exports::InitializeManagedThread();
    _global_heap = gc->reset_heap();
}

void application::initialize_threads()
{
    auto* gc = autocrat::global_services.get_service<autocrat::gc_service>();
    gc->begin_work(autocrat::lifetime_service::global_thread_id);
    _global_heap = gc->reset_heap();
    initialize_managed_thread(gc); // Initialize the current thread

    autocrat::global_services.get_thread_pool().start(
        [this, gc](std::size_t thread_id) {
            gc->begin_work(thread_id);
            initialize_managed_thread(gc);
            gc->end_work(thread_id);
        });
}

}

#ifndef APPLICATION_H
#define APPLICATION_H

#include "gc_service.h"
#include <CLI/App.hpp>
#include <CLI/Config.hpp>
#include <CLI/Formatter.hpp>
#include <atomic>
#include <filesystem>

namespace autocrat
{

/**
 * Performs the main application logic.
 */
class application
{
public:
    /**
     * Sets the applications description in the help text.
     * @param value The value for the description.
     */
    void description(const char* value);

    /**
     * Initializes the services and loads the configuration data.
     * @param argc The number of arguments passed to the program.
     * @param argv The arguments passed to the program.
     */
    void initialize(int argc, const char* const* argv);

    /**
     * Runs the main program loop.
     * @remarks This blocks until a call to `stop` is made.
     */
    void run();

    /**
     * Stops the services.
     */
    void stop();

    /**
     * Sets the applications version information.
     * @param value The value for the version.
     */
    void version(const char* value);

private:
    void initialize_managed_thread(gc_service* gc);
    void initialize_threads();

    CLI::App _app;
    gc_heap _global_heap;
    std::atomic_bool _running;
    int _thread_affinity = -1;
    int _thread_count = -1;
};

/**
 * Gets the path to the configuration file.
 */
std::filesystem::path get_config_file();

}

#endif

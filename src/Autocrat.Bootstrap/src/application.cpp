#include "application.h"
#include "managed_exports.h"
#include "pal.h"
#include "pause.h"
#include "services.h"
#include <cerrno>
#include <cstdlib>
#include <fstream>
#include <memory>
#include <spdlog/spdlog.h>

namespace fs = std::filesystem;

namespace
{

struct byte_array
{
    byte_array() = default;
    byte_array(const byte_array&) = delete;
    byte_array& operator=(const byte_array&) = delete;
    ~byte_array() = delete;

    const void* ee_type;
    std::uint64_t length;
    char data[1];
};

void byte_array_delete(void* array)
{
    delete[] static_cast<char*>(array);
}
using byte_array_ptr =
    std::unique_ptr<byte_array, decltype(&byte_array_delete)>;

byte_array_ptr make_byte_array(std::size_t length)
{
    auto raw = std::make_unique<char[]>(sizeof(byte_array) + length);

    auto array = new (raw.get()) byte_array;
    array->ee_type = managed_exports::GetByteArrayType();
    array->length = length;

    return byte_array_ptr(
        reinterpret_cast<byte_array*>(raw.release()), byte_array_delete);
}

void load_configuration(const fs::path& path)
{
    std::ifstream file(path, std::ios_base::binary | std::ios_base::in);
    if (file.fail())
    {
        spdlog::warn("Unable to open configuration file ({})", errno);
    }
    else
    {
        file.seekg(0, std::ios::end);
        auto length = static_cast<std::size_t>(file.tellg());
        file.seekg(0, std::ios::beg);

        byte_array_ptr array = make_byte_array(length);
        file.read(array->data, length);
        if (!managed_exports::LoadConfiguration(array.get()))
        {
            throw std::runtime_error("Unable to load the configuration");
        }
    }
}

}

namespace autocrat
{

void application::description(const char* value)
{
    _app.description(value);
}

void application::initialize(int argc, const char* const* argv)
{
    spdlog::debug("Parsing command line arguments");
    try
    {
        _app.add_option(
            "affinity",
            _thread_affinity,
            "Specifies the starting CPU affinity for the process");

        _app.add_option(
            "thread_pool",
            _thread_count,
            "Specifies the number of threads to use in the thread pool");

        _app.parse(argc, argv);
    }
    catch (const CLI::Error& error)
    {
        std::exit(_app.exit(error));
    }

    spdlog::debug("Creating native services");
    autocrat::global_services.initialize();

    spdlog::debug("Setting up native/manage transition for threads");
    initialize_threads();

    spdlog::debug("Registering exported managed types");
    managed_exports::RegisterManagedTypes();

    fs::path path = get_config_file();
    spdlog::info("Loading configuration from '{}'", path.string());
    load_configuration(path);
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
    gc->end_work(autocrat::lifetime_service::global_thread_id);
}

void application::stop()
{
    _running = false;
}

void application::version(const char* value)
{
    _app.add_flag_callback(
        "--version",
        [value]() {
            std::cout << value << std::endl;
            throw CLI::Success();
        },
        "Show version information");
}

void application::initialize_managed_thread(autocrat::gc_service* gc)
{
    gc->set_heap(std::move(_global_heap));
    managed_exports::InitializeManagedThread();
    _global_heap = gc->reset_heap();
}

void application::initialize_threads()
{
    if (_thread_count < 0)
    {
        _thread_count = static_cast<int>(std::thread::hardware_concurrency());
    }

    if (_thread_affinity >= 0)
    {
        spdlog::info("Running main thread on CPU {}", _thread_affinity);
        pal::set_affinity(nullptr, _thread_affinity);
        ++_thread_affinity;
    }

    auto* gc = autocrat::global_services.get_service<autocrat::gc_service>();
    autocrat::global_services.get_thread_pool().start(
        _thread_affinity, _thread_count, [this, gc](std::size_t thread_id) {
            gc->begin_work(thread_id);
            initialize_managed_thread(gc);
            gc->end_work(thread_id);
        });

    // Initialize the current thread too. Note we have to do this after we've
    // started the thread pool so that the thread specific storage has been
    // allocated and that all the other threads have finished with the global
    // heap
    gc->begin_work(autocrat::lifetime_service::global_thread_id);
    gc->set_heap(std::move(_global_heap));
    managed_exports::InitializeManagedThread();
}

fs::path get_config_file()
{
    fs::path exe = pal::get_current_executable();
    return exe.replace_filename("config.json");
}

}

#include "application.h"
#include "managed_exports.h"
#include "pal.h"
#include "pause.h"
#include "services.h"
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
    std::ifstream file(
        path.native(), std::ios_base::binary | std::ios_base::in);
    if (!file)
    {
        spdlog::warn("Unable to open configuration file");
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

void application::initialize()
{
    spdlog::debug("Creating native services");
    autocrat::global_services.initialize();

    spdlog::debug("Setting up native/manage transition for threads");
    initialize_threads();

    spdlog::debug("Registering worker type constructors");
    managed_exports::RegisterWorkerTypes();

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

fs::path get_config_file()
{
    fs::path exe = pal::get_current_executable();
    return exe.replace_filename("config.json");
}

}

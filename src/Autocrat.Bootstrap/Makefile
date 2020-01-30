include variables.mk

CXXFLAGS += -I include -isystem ../../libs

SOURCES = \
 src/array_pool.cpp \
 src/gc_service.cpp \
 src/main.cpp \
 src/managed_interop.cpp \
 src/memory_pool.cpp \
 src/native_exports.cpp \
 src/network_service.cpp \
 src/pal_posix.cpp \
 src/thread_pool.cpp \
 src/timer_service.cpp \
 src/worker_service.cpp

OUTPUT_LIB = bin/Autocrat.Bootstrap.a
build: $(OUTPUT_LIB);

$(OBJ_DIR)/%.o: src/%.cpp
	$(call build_object,$<,$@)

OBJECTS = $(patsubst src/%.cpp, $(OBJ_DIR)/%.o, $(SOURCES))
$(OUTPUT_LIB): $(OBJECTS)
	@mkdir -p $(@D)
	@ar rs $(OUTPUT_LIB) $(OBJECTS)

.PHONY: clean
clean:
	@rm -r $(OBJ_DIR)/*.o
	@rm -f $(OUTPUT_LIB)

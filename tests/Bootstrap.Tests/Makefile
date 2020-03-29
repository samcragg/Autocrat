include ../../src/Autocrat.Bootstrap/variables.mk

CXXFLAGS += -DUNIT_TESTS -I . -I ../../src/Autocrat.Bootstrap/include -isystem ../../libs

SRC_DIR = ../../src/Autocrat.Bootstrap/src
SRCS = \
 $(SRC_DIR)/array_pool.cpp \
 $(SRC_DIR)/gc_service.cpp \
 $(SRC_DIR)/locks.cpp \
 $(SRC_DIR)/managed_interop.cpp \
 $(SRC_DIR)/memory_pool.cpp \
 $(SRC_DIR)/native_exports.cpp \
 $(SRC_DIR)/network_service.cpp \
 $(SRC_DIR)/pal_posix.cpp \
 $(SRC_DIR)/thread_pool.cpp \
 $(SRC_DIR)/timer_service.cpp \
 $(SRC_DIR)/worker_service.cpp

TESTS = \
 tests/ArrayPoolTests.cpp \
 tests/BoundedQueueTests.cpp \
 tests/DynamicArrayTests.cpp \
 tests/ExclusiveLockTests.cpp \
 tests/GcServiceTests.cpp \
 tests/FixedHashmapTests.cpp \
 tests/ManagedArrayTests.cpp \
 tests/MemoryPoolTests.cpp \
 tests/NativeExportsTests.cpp \
 tests/NodePoolTests.cpp \
 tests/ObjectSerializerTests.cpp \
 tests/PalServicesTests.cpp \
 tests/PalSocketTests.cpp \
 tests/PalThreadTests.cpp \
 tests/ReferenceScannerTests.cpp \
 tests/ServicesTests.cpp \
 tests/SharedSpinLockTests.cpp \
 tests/SmallVectorTests.cpp \
 tests/SmartPtrTests.cpp \
 tests/ThreadPoolTests.cpp \
 tests/TimerServiceTests.cpp \
 tests/WorkerServiceTests.cpp \
 ManagedObjects.cpp \
 MemoryMonitor.cpp \
 main.cpp \
 mock_method_handles.cpp \
 pal_mock.cpp

TEST_PROGRAM = bin/$(mode)/Bootstrap.Tests
TEST_OUTPUT = bootstrap_results_linux.xml

build: $(TEST_PROGRAM);

test: $(TEST_PROGRAM)
	$(TEST_PROGRAM) --gtest_output="xml:$(TEST_OUTPUT)"

GTEST_OBJ = $(OBJ_DIR)/libs/gtest-all.o
$(GTEST_OBJ): ../../libs/gtest/gtest-all.cc
	$(call build_object,$<,$@)

SRC_OBJS = $(SRCS:$(SRC_DIR)/%.cpp=$(OBJ_DIR)/src/%.o)
$(OBJ_DIR)/src/%.o: $(SRC_DIR)/%.cpp
	$(call build_object,$<,$@)

TESTS_OBJS = $(TESTS:%.cpp=$(OBJ_DIR)/%.o)
$(OBJ_DIR)/%.o: %.cpp
	$(call build_object,$<,$@)

OBJECTS = $(GTEST_OBJ) $(SRC_OBJS) $(TESTS_OBJS)
$(TEST_PROGRAM): $(OBJECTS)
	@mkdir -p $(@D)
	g++ $(OBJECTS) $(CXXFLAGS) -o $(TEST_PROGRAM)

.PHONY: clean
clean:
	@find $(OBJ_DIR) -type f -name '*.o' -delete
	@rm -f $(TEST_PROGRAM)
	@rm -f $(TEST_OUTPUT)

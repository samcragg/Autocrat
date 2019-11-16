include ../../src/Autocrat.Bootstrap/variables.mk

CXXFLAGS += -DUNIT_TESTS -I . -I ../../src/Autocrat.Bootstrap/include -isystem ../../libs

SRC_DIR = ../../src/Autocrat.Bootstrap/src
SRCS = \
 $(SRC_DIR)/array_pool.cpp \
 $(SRC_DIR)/native_exports.cpp \
 $(SRC_DIR)/network_service.cpp \
 $(SRC_DIR)/thread_pool.cpp \
 $(SRC_DIR)/pal_posix.cpp

TESTS = \
 tests/ArrayPoolTests.cpp \
 tests/BoundedQueueTests.cpp \
 tests/ManagedArrayPtrTests.cpp \
 tests/ManagedArrayTests.cpp \
 tests/NativeExportsTests.cpp \
 tests/PalServicesTests.cpp \
 tests/PalSocketTests.cpp \
 tests/PalThreadTests.cpp \
 tests/ServicesTests.cpp \
 tests/SmallVectorTests.cpp \
 tests/ThreadPoolTests.cpp \
 main.cpp \
 mock_method_handles.cpp \
 pal_mock.cpp

TEST_PROGRAM = bin/$(mode)/Bootstrap.Tests
TEST_OUTPUT = test_results.xml
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
	@rm -rf $(OBJ_DIR)
	@rm -f $(TEST_PROGRAM)
	@rm -f $(TEST_OUTPUT)

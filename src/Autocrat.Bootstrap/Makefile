include variables.mk

SOURCES = \
 src/array_pool.cpp

OUTPUT_LIB = bin/$(mode)/Autocrat.Bootstrap.a
build: $(OUTPUT_LIB);

$(OBJ_DIR)/%.o: src/%.cpp
	$(call build_object,$<,$@)

OBJECTS = $(patsubst src/%.cpp, $(OBJ_DIR)/%.o, $(SOURCES))
$(OUTPUT_LIB): $(OBJECTS)
	@mkdir -p $(@D)
	@ar rs $(OUTPUT_LIB) $(OBJECTS)

.PHONY: clean
clean:
	@rm -rf $(OBJ_DIR)
	@rm -f $(OUTPUT_LIB)

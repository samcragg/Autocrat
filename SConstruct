import os

coverage = ARGUMENTS.get("coverage", 0) != 0
mode = ARGUMENTS.get("mode", "Release")

compiler_flags = ["-m64", "-std=c++17", "-isystem", "libs"]
optimise_flags = ["-g"] if (mode == "Debug") else ["-O2", "-DNDEBUG"]
warning_flags = ["-Werror", "-Wall", "-Wextra", "-Wpedantic"]

# Allow the build objects to be separate from the source code without the horror
# of VariantDir
def build_objects(env, output, sources, exclude=[]):
    objects = []
    for source in sources:
        basename = os.path.basename(source.path)
        if (basename not in exclude):
            filename = os.path.splitext(basename)[0]
            target = os.path.join("obj", mode, output, filename)
            objects.append(env.StaticObject(target, source))
    return objects

env = Environment(
    CPPPATH = Dir("src/Autocrat.Bootstrap/include"),
    CXX = os.getenv("CXX", "g++"),
    CXXFLAGS = [warning_flags, optimise_flags, compiler_flags],
    LIBS = ["pthread", "rt"])
env.AddMethod(build_objects, "BuildObjects")

sources = Glob("src/Autocrat.Bootstrap/src/*.cpp", exclude = ["src/Autocrat.Bootstrap/src/*win32.cpp"])

if not coverage:
    SConscript("src/Autocrat.Bootstrap/SConscript", exports = "env sources")

SConscript("tests/Bootstrap.Tests/SConscript", exports = "env sources coverage")

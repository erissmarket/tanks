cmake_minimum_required(VERSION 3.16)
project(TankAntiCheatNative LANGUAGES C CXX)
enable_testing()

add_library(native_rate_limit STATIC rate_limit.c)
target_compile_features(native_rate_limit PUBLIC c_std_11)
target_include_directories(native_rate_limit PUBLIC "${CMAKE_CURRENT_SOURCE_DIR}")

add_executable(native_rate_test rate_limit_test.c)
target_link_libraries(native_rate_test PRIVATE native_rate_limit)
if(UNIX)
    target_link_libraries(native_rate_test PRIVATE m)
endif()

add_executable(native_guard_test smoke_test.cpp)
target_compile_features(native_guard_test PRIVATE cxx_std_17)

foreach(target native_rate_limit native_rate_test native_guard_test)
    if(MSVC)
        target_compile_options(${target} PRIVATE /W4 /WX)
    else()
        target_compile_options(${target} PRIVATE -Wall -Wextra -Werror)
    endif()
endforeach()

add_test(NAME native_rate_limit COMMAND native_rate_test)
add_test(NAME native_guard COMMAND native_guard_test)

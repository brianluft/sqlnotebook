#ifdef __cplusplus
#error This file should be compiled as C.
#endif

// See https://sqlite.org/compile.html

// Recommended Compile-time Options
#define SQLITE_DQS 0
#define SQLITE_THREADSAFE 0
#define SQLITE_DEFAULT_MEMSTATUS 0
// We do not take the recommendation for SQLITE_DEFAULT_WAL_SYNCHRONOUS
#define SQLITE_LIKE_DOESNT_MATCH_BLOBS 1
#define SQLITE_MAX_EXPR_DEPTH 0
#define SQLITE_OMIT_DECLTYPE 1
#define SQLITE_OMIT_DEPRECATED 1
#define SQLITE_OMIT_PROGRESS_CALLBACK 1
#define SQLITE_OMIT_SHARED_CACHE 1
// Alloca is recommended but doesn't seem to be available on ARM64, we disable it.
#undef SQLITE_USE_ALLOCA
#define SQLITE_OMIT_AUTOINIT 1

// Options To Set Default Parameter Values
#define SQLITE_DEFAULT_AUTOMATIC_INDEX 1
#define SQLITE_DEFAULT_AUTOVACUUM 1 // FULL
#define SQLITE_DEFAULT_FOREIGN_KEYS 1
#define SQLITE_DEFAULT_LOCKING_MODE 1 // EXCLUSIVE
#define SQLITE_DEFAULT_SYNCHRONOUS 0 // OFF
#define SQLITE_DEFAULT_WAL_SYNCHRONOUS 0 // OFF
#define SQLITE_DEFAULT_WORKER_THREADS 4
#define SQLITE_MAX_WORKER_THREADS 4
#define SQLITE_WIN32_MALLOC 1

// Options To Enable Features Normally Turned Off
#define SQLITE_ENABLE_API_ARMOR 1
#define SQLITE_ENABLE_BYTECODE_VTAB 1
#define SQLITE_ENABLE_DBPAGE_VTAB 1
#define SQLITE_ENABLE_DBSTAT_VTAB 1
#define SQLITE_ENABLE_EXPLAIN_COMMENTS 1
#define SQLITE_ENABLE_FTS5 1
#define SQLITE_ENABLE_MATH_FUNCTIONS 1
#define SQLITE_ENABLE_STMTVTAB 1
#define SQLITE_ENABLE_STAT4 1
#define SQLITE_SOUNDEX 1
#define SQLITE_USE_SEH 1

// The percentile extension has been part of the amalgamation since SQLite version 3.51.0.
#define SQLITE_ENABLE_PERCENTILE 1

// Options To Omit Features
#define SQLITE_OMIT_DESERIALIZE 1
#define SQLITE_OMIT_GET_TABLE 1
#define SQLITE_OMIT_MEMORYDB 1
#define SQLITE_OMIT_TCL_VARIABLE 1
#define SQLITE_OMIT_TRACE 1

// Compiler Linkage and Calling Convention Control
#define SQLITE_API __declspec(dllexport)
#define SQLITE_CDECL __cdecl
#define SQLITE_APICALL __cdecl

#include "sqlite3.c"

SQLITE_API int SxGetToken(const unsigned char* z, int* tokenType) {
    return sqlite3GetToken(z, tokenType);
}

// Extra modules
#include "../../ext/sqlite/sqlite-src/ext/misc/uuid.c"
#include "../../ext/sqlite/sqlite-src/ext/misc/series.c"

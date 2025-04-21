#include <setjmp.h>
#include "byondapi.h"

// This needs to be in C/C++ because it relies on setjmp/longjmp ;_;
// Technically C++ because byondapi.h doesn't work in pure C.

static jmp_buf callext_crash_jmp;

typedef CByondValue (*ByondApiFunction)(u4c argc, CByondValue argv[]);

extern "C" {

void OpenDream_Internal_ErrorHandlerCrash() {
    longjmp(callext_crash_jmp, 1);
}

CByondValue OpenDream_Internal_ErrorHandlerCallExt(ByondApiFunction func, u4c argc, CByondValue argv[]) {
    if (setjmp(callext_crash_jmp)) {
        return { };
    }

    return func(argc, argv);
}

}

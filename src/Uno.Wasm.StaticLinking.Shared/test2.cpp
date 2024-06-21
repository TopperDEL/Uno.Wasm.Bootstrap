#include <stdlib.h>
#include <string.h>
#include <stdio.h>

#define WASM_EXPORT __attribute__((visibility("default")))

extern "C" {
	WASM_EXPORT int additional_native_add2(int a, int b);
}

WASM_EXPORT int additional_native_add2(int a, int b) {
	printf("additional_native_add2(%d, %d)\r\n", a, b);
	return a + b;
}

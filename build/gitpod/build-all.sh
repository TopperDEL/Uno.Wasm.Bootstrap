#!/bin/bash
cd /workspace/Uno.Wasm.Bootstrap

dotmet build -c Release /p:WasmShellMonoRuntimeExecutionMode=Interpreter src/Uno.Wasm.Bootstrap.sln
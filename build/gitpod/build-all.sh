#!/bin/bash
cd /workspace/Uno.Wasm.Bootstrap

dotnet build -c Release /p:WasmShellMonoRuntimeExecutionMode=Interpreter src/Uno.Wasm.Bootstrap.sln
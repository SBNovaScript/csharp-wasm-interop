# C# <-> WebAssembly Native Interop

Bidirectional interop between C# and WebAssembly with no bridge languages. C# hosts a Wasmtime runtime to load WASM modules written in raw WebAssembly Text Format (WAT). Both sides can call each other's functions through imports/exports and share data through linear memory.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        C# Host                              │
│                                                             │
│  ┌─────────────┐    ┌──────────┐    ┌────────────────────┐  │
│  │  WasmEngine  │───▶│  Linker  │───▶│  WasmModule        │  │
│  │  (Engine +   │    │  (Define │    │  (Instance +       │  │
│  │   Store)     │    │   C# fns │    │   typed exports)   │  │
│  └─────────────┘    │   as     │    └────────┬───────────┘  │
│                     │  imports)│             │              │
│                     └──────────┘    ┌────────▼───────────┐  │
│                                     │  SharedMemory      │  │
│                                     │  (Read/write       │  │
│                                     │   strings, bytes,  │  │
│                                     │   ints via linear   │  │
│                                     │   memory)          │  │
│                                     └────────────────────┘  │
└─────────────────────────────────┬───────────────────────────┘
                                  │
              ┌───────────────────▼───────────────────┐
              │          WASM Modules (.wat)           │
              │                                       │
              │  Exports: functions C# can call       │
              │  Imports: functions provided by C#     │
              │  Memory:  shared linear memory         │
              └───────────────────────────────────────┘
```

**Data flow:**
- **C# -> WASM:** C# calls exported WASM functions via typed delegates (`Func<>` / `Action<>`)
- **WASM -> C#:** WASM calls imported C# functions defined at link time
- **Shared state:** Both sides read/write the same linear memory for complex data (strings, arrays)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- No other tools required — WAT modules are loaded directly at runtime by Wasmtime

## Quick Start

```bash
# Clone and enter the repository
cd csharp-wasm

# Restore packages
cd csharp && dotnet restore

# Run the examples
dotnet run --project examples/Examples

# Run the tests
dotnet test
```

## Project Structure

```
csharp-wasm/
├── README.md
├── wasm/                              # WebAssembly modules (WAT source)
│   ├── arithmetic.wat                 # Pure math operations
│   ├── callbacks.wat                  # WASM calling back into C#
│   ├── string_ops.wat                 # String processing via shared memory
│   ├── fibonacci.wat                  # Fibonacci with progress callbacks
│   └── calculator.wat                 # Bidirectional: WASM + C# math
├── csharp/
│   ├── CSharpWasmInterop.slnx        # Solution file
│   ├── src/WasmInterop/              # Core interop library
│   │   ├── WasmEngine.cs             # Engine, store, linker management
│   │   ├── WasmModule.cs             # Typed access to WASM exports
│   │   ├── SharedMemory.cs           # Linear memory read/write helpers
│   │   └── WasmInteropException.cs   # Custom exception type
│   ├── examples/Examples/             # Runnable example programs
│   │   └── Program.cs                # 6 comprehensive examples
│   └── tests/WasmInterop.Tests/      # xUnit test suite (46 tests)
│       └── UnitTest1.cs
```

## How It Works

### The Only Dependency

This project uses a single NuGet package: [`Wasmtime`](https://www.nuget.org/packages/Wasmtime) — the official .NET embedding of the [Wasmtime](https://wasmtime.dev/) runtime from the Bytecode Alliance. It provides:

- A JIT compiler for WASM (Cranelift)
- The ability to load `.wat` (text) or `.wasm` (binary) modules
- Type-safe function imports and exports
- Shared linear memory access

No JavaScript, no Emscripten, no wasm-bindgen, no bridge languages.

### C# Calling WASM

1. Create a `WasmEngine` (wraps Wasmtime's Engine + Store + Linker)
2. Load a `.wat` file to get a `WasmModule`
3. Get typed function handles and call them

```csharp
using WasmInterop;

using var engine = new WasmEngine();
var module = engine.LoadWatFile("wasm/arithmetic.wat");

var add = module.GetFunction<int, int, int>("add");
Console.WriteLine(add(10, 3)); // 13

var factorial = module.GetFunction<int, long>("factorial");
Console.WriteLine(factorial(10)); // 3628800
```

### WASM Calling C#

1. Define C# functions as imports before loading the module
2. WASM calls them during execution

```csharp
using WasmInterop;

using var engine = new WasmEngine();

// Define C# functions that WASM can call
engine.DefineFunction<int, int>("host", "on_progress",
    (int current, int total) =>
        Console.WriteLine($"Progress: {current}/{total}"));

engine.DefineFunction<int>("host", "on_complete",
    (int result) => Console.WriteLine($"Done: {result}"));

var module = engine.LoadWatFile("wasm/callbacks.wat");
var run = module.GetFunction<int, int>("run_with_callbacks");
run(5); // WASM calls on_progress and on_complete during execution
```

The corresponding WAT declares these as imports:

```wat
(module
  (import "host" "on_progress" (func $on_progress (param i32 i32)))
  (import "host" "on_complete" (func $on_complete (param i32)))

  (func $run_with_callbacks (param $steps i32) (result i32)
    ;; ... computation ...
    local.get $i
    local.get $steps
    call $on_progress    ;; calls C# during execution
    ;; ...
  )
  (export "run_with_callbacks" (func $run_with_callbacks))
)
```

### Shared Memory (Strings & Complex Data)

WASM only supports numeric types (i32, i64, f32, f64). Strings and complex data are passed through shared linear memory:

```csharp
using WasmInterop;

using var engine = new WasmEngine();
var module = engine.LoadWatFile("wasm/string_ops.wat");
var mem = module.GetSharedMemory();

// Write a string into WASM memory
mem.WriteString(0, "Hello, WebAssembly!");

// Call WASM to process it in-place
var toUpper = module.GetAction<int, int>("to_upper");
toUpper(0, 19);

// Read the result back
Console.WriteLine(mem.ReadString(0, 19)); // "HELLO, WEBASSEMBLY!"
```

### Bidirectional (WASM Uses C# Math)

The most powerful pattern: WASM does its own arithmetic but calls C# for operations not available in WASM (sqrt, sin, cos, pow, log):

```csharp
using WasmInterop;

using var engine = new WasmEngine();

// Provide C# math functions as WASM imports
engine.DefineFunction<double, double>("math", "sqrt", Math.Sqrt);
engine.DefineFunction<double, double, double>("math", "pow", Math.Pow);
engine.DefineFunction<double, double>("math", "sin", Math.Sin);
engine.DefineFunction<double, double>("math", "cos", Math.Cos);
engine.DefineFunction<double, double>("math", "log", Math.Log);

var module = engine.LoadWatFile("wasm/calculator.wat");

// WASM internally calls C# sqrt: distance = sqrt(a^2 + b^2)
var distance = module.GetFunction<double, double, double>("distance");
Console.WriteLine(distance(3, 4)); // 5

// WASM internally calls C# pow: compound = principal * pow(1+rate, periods)
var compound = module.GetFunction<double, double, double, double>("compound_interest");
Console.WriteLine(compound(1000, 0.05, 10)); // 1628.89
```

### Inline WAT (No Files)

You can embed WAT directly in C# — useful for small, self-contained modules:

```csharp
using WasmInterop;

using var engine = new WasmEngine();

engine.DefineFunction<int>("env", "print",
    (int value) => Console.WriteLine($"WASM says: {value}"));

var module = engine.LoadWatString("inline", """
    (module
      (import "env" "print" (func $print (param i32)))
      (func $double_and_print (param $x i32)
        local.get $x
        i32.const 2
        i32.mul
        call $print)
      (export "double_and_print" (func $double_and_print)))
    """);

module.GetAction<int>("double_and_print")(21); // prints "WASM says: 42"
```

## Examples

Run all 6 examples:

```bash
cd csharp
dotnet run --project examples/Examples
```

| # | Example | Direction | What It Shows |
|---|---------|-----------|---------------|
| 1 | Arithmetic | C# -> WASM | Pure WASM functions: add, subtract, multiply, divide, factorial, abs, max, min, float ops |
| 2 | Callbacks | WASM -> C# | WASM calls C# for logging, progress reporting, timestamps |
| 3 | String Ops | Bidirectional | C# writes strings to shared memory, WASM processes in-place (uppercase, lowercase, reverse, rot13, count) |
| 4 | Fibonacci | WASM -> C# | WASM computes fibonacci and calls C# with each value; also writes results to shared memory |
| 5 | Calculator | Bidirectional | WASM imports C# Math.Sqrt/Pow/Sin/Cos/Log to compute distance, compound interest, circle area, polar coordinates |
| 6 | Inline WAT | Both | Embedded WAT string — no files needed |

## API Reference

### `WasmEngine`

The entry point. Manages the Wasmtime engine, store, and linker.

```csharp
using var engine = new WasmEngine();

// Load modules (define imports first!)
engine.DefineFunction("module", "name", () => { });
var module = engine.LoadWatFile("path.wat");
var module = engine.LoadWatString("name", watSource);
var module = engine.LoadWasmFile("path.wasm");

// Define host functions (C# -> WASM imports)
engine.DefineFunction("mod", "fn", () => { });                           // void, no params
engine.DefineFunction<int>("mod", "fn", (int x) => { });                 // void, 1 param
engine.DefineFunction<int, int>("mod", "fn", (int a, int b) => { });     // void, 2 params
engine.DefineFunction<int>("mod", "fn", () => 42);                       // returns int
engine.DefineFunction<int, int>("mod", "fn", (int x) => x * 2);         // 1 param, returns int
engine.DefineFunction<int, int, int>("mod", "fn", (int a, int b) => a + b); // 2 params, returns int

// With Caller (for memory access inside callbacks)
engine.DefineFunctionWithCaller<int, int>("mod", "fn",
    (Caller caller, int ptr, int len) => {
        var mem = caller.GetMemory("memory");
        var str = mem.ReadString(ptr, len);
    });
```

### `WasmModule`

Wraps an instantiated WASM module with typed access.

```csharp
// Void functions (Actions)
Action run = module.GetAction("run");
Action<int> process = module.GetAction<int>("process");

// Functions with return values
Func<int> getValue = module.GetFunction<int>("get_value");
Func<int, int, int> add = module.GetFunction<int, int, int>("add");

// Memory access
var mem = module.GetSharedMemory();
var exports = module.ListExports();
```

### `SharedMemory`

Read/write helpers for WASM linear memory.

```csharp
var mem = module.GetSharedMemory();

// Strings
int bytesWritten = mem.WriteString(offset, "hello");
mem.WriteNullTerminatedString(offset, "hello");
string s = mem.ReadString(offset, length);
string s = mem.ReadNullTerminatedString(offset);

// Raw bytes
mem.WriteBytes(offset, byteArray);
byte[] data = mem.ReadBytes(offset, length);

// Integers
mem.WriteInt32(offset, 42);
int value = mem.ReadInt32(offset);

// Size
long bytes = mem.SizeInBytes; // 65536 per page
```

## WAT Module Patterns

### Export-Only (C# calls WASM)

```wat
(module
  (func $add (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add)
  (export "add" (func $add)))
```

### Import + Export (Bidirectional)

```wat
(module
  ;; Import a C# function
  (import "host" "log" (func $log (param i32)))

  ;; Use it in an exported function
  (func $run (param $x i32)
    local.get $x
    call $log)
  (export "run" (func $run)))
```

### With Shared Memory

```wat
(module
  ;; Declare and export memory (1 page = 64KB)
  (memory (export "memory") 1)

  ;; Embed static data
  (data (i32.const 0) "Hello World")

  ;; Process memory contents
  (func $to_upper (param $ptr i32) (param $len i32)
    ;; ... modify bytes in-place ...
  )
  (export "to_upper" (func $to_upper)))
```

## Type Mapping

| WAT Type | C# Type |
|----------|---------|
| `i32` | `int` |
| `i64` | `long` |
| `f32` | `float` |
| `f64` | `double` |
| Linear memory | `SharedMemory` / `Memory` (byte-addressable) |

## Tests

```bash
cd csharp
dotnet test
```

46 tests covering:
- All arithmetic operations with edge cases
- Callback invocation and progress tracking
- String operations (uppercase, lowercase, reverse, rot13, count)
- Fibonacci correctness and callback order
- Calculator composite operations (distance, compound interest, polar coordinates)
- Inline WAT loading, bidirectional round-trips
- Shared memory read/write for strings, bytes, and integers
- Error handling (missing exports, invalid WAT, missing imports)

## Troubleshooting

**"Export 'X' not found or has wrong signature"**
The WASM module doesn't export a function with that name, or the C# type parameters don't match the WAT signature. Check `module.ListExports()` and verify the WAT parameter/result types match the C# generic arguments.

**"Unknown import" or linking error**
All imports must be defined via `DefineFunction` / `DefineFunctionWithCaller` *before* calling `LoadWatFile`. The module/name strings must match exactly.

**"Unable to create a callback with a return type of type 'System.Boolean'"**
Wasmtime only supports WASM numeric types (int, long, float, double). Use `Action` lambdas (`() => { }`) instead of expression lambdas (`() => true`) for void callbacks.

## License

Apache-2.0 — see [LICENSE](LICENSE) and [NOTICE](NOTICE) for details.

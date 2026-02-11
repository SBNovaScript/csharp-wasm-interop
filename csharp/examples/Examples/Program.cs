// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026-present Steven Baumann
// Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
// See LICENSE and NOTICE in the repository root for details.

using Wasmtime;
using WasmInterop;

// ── Resolve the wasm/ directory (two levels up from the examples project) ──
var wasmDir = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "wasm"));

if (!Directory.Exists(wasmDir))
{
    Console.Error.WriteLine($"Cannot find wasm/ directory at {wasmDir}");
    Console.Error.WriteLine("Run from the repository root: dotnet run --project csharp/examples/Examples");
    return 1;
}

Console.WriteLine("==========================================================");
Console.WriteLine("  C# <-> WebAssembly Interop Examples");
Console.WriteLine("==========================================================\n");

RunArithmeticExample(wasmDir);
RunCallbackExample(wasmDir);
RunStringOpsExample(wasmDir);
RunFibonacciExample(wasmDir);
RunCalculatorExample(wasmDir);
RunInlineWatExample();

Console.WriteLine("\n==========================================================");
Console.WriteLine("  All examples completed successfully!");
Console.WriteLine("==========================================================");
return 0;

// ─────────────────────────────────────────────────────────────────────────────
// Example 1: C# calls pure WASM arithmetic functions
// ─────────────────────────────────────────────────────────────────────────────
static void RunArithmeticExample(string wasmDir)
{
    Console.WriteLine("── Example 1: C# Calling WASM Arithmetic ──────────────────\n");

    using var engine = new WasmEngine();
    var module = engine.LoadWatFile(Path.Combine(wasmDir, "arithmetic.wat"));

    // Get typed function handles
    var add       = module.GetFunction<int, int, int>("add");
    var subtract  = module.GetFunction<int, int, int>("subtract");
    var multiply  = module.GetFunction<int, int, int>("multiply");
    var divide    = module.GetFunction<int, int, int>("divide");
    var modulo    = module.GetFunction<int, int, int>("modulo");
    var factorial = module.GetFunction<int, long>("factorial");
    var abs       = module.GetFunction<int, int>("abs");
    var max       = module.GetFunction<int, int, int>("max");
    var min       = module.GetFunction<int, int, int>("min");
    var addF64    = module.GetFunction<double, double, double>("add_f64");
    var mulF64    = module.GetFunction<double, double, double>("multiply_f64");

    Console.WriteLine($"  add(10, 3)        = {add(10, 3)}");
    Console.WriteLine($"  subtract(10, 3)   = {subtract(10, 3)}");
    Console.WriteLine($"  multiply(10, 3)   = {multiply(10, 3)}");
    Console.WriteLine($"  divide(10, 3)     = {divide(10, 3)}");
    Console.WriteLine($"  divide(10, 0)     = {divide(10, 0)}  (guarded)");
    Console.WriteLine($"  modulo(10, 3)     = {modulo(10, 3)}");
    Console.WriteLine($"  factorial(10)     = {factorial(10)}");
    Console.WriteLine($"  abs(-42)          = {abs(-42)}");
    Console.WriteLine($"  max(7, 12)        = {max(7, 12)}");
    Console.WriteLine($"  min(7, 12)        = {min(7, 12)}");
    Console.WriteLine($"  add_f64(1.5, 2.7) = {addF64(1.5, 2.7)}");
    Console.WriteLine($"  mul_f64(3.0, 4.5) = {mulF64(3.0, 4.5)}");
    Console.WriteLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// Example 2: WASM calls back into C# (callbacks, progress, timestamps)
// ─────────────────────────────────────────────────────────────────────────────
static void RunCallbackExample(string wasmDir)
{
    Console.WriteLine("── Example 2: WASM Calling C# via Callbacks ───────────────\n");

    using var engine = new WasmEngine();

    // Define C# functions that WASM will call
    engine.DefineFunctionWithCaller<int, int>("host", "log_message",
        (Caller caller, int ptr, int len) =>
        {
            var memory = caller.GetMemory("memory");
            var message = memory!.ReadString(ptr, len);
            Console.WriteLine($"  [WASM -> C#] Log: {message}");
        });

    engine.DefineFunction<int, int>("host", "on_progress",
        (int current, int total) =>
        {
            var pct = (int)((double)current / total * 100);
            Console.WriteLine($"  [WASM -> C#] Progress: {current}/{total} ({pct}%)");
        });

    engine.DefineFunction<int>("host", "on_complete",
        (int result) => Console.WriteLine($"  [WASM -> C#] Complete! Result = {result}"));

    engine.DefineFunction("host", "get_timestamp",
        () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    var module = engine.LoadWatFile(Path.Combine(wasmDir, "callbacks.wat"));

    // Call the WASM function — it will call back into C# during execution
    var runWithCallbacks = module.GetFunction<int, int>("run_with_callbacks");
    var result = runWithCallbacks(5);
    Console.WriteLine($"  Final result from WASM: {result}");

    // Record a timestamp via WASM -> C# -> WASM round-trip
    var recordTimestamp = module.GetFunction<long>("record_timestamp");
    Console.WriteLine($"  Timestamp from C#: {recordTimestamp()}");

    // WASM writes a dynamic string and asks C# to log it
    var logStep = module.GetAction<int>("log_step_number");
    logStep(7);
    Console.WriteLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// Example 3: String operations through shared linear memory
// ─────────────────────────────────────────────────────────────────────────────
static void RunStringOpsExample(string wasmDir)
{
    Console.WriteLine("── Example 3: String Ops via Shared Memory ────────────────\n");

    using var engine = new WasmEngine();
    var module = engine.LoadWatFile(Path.Combine(wasmDir, "string_ops.wat"));
    var mem = module.GetSharedMemory();

    // Write a string into WASM memory, operate on it, read back
    const string original = "Hello, WebAssembly!";
    var len = mem.WriteNullTerminatedString(0, original);
    Console.WriteLine($"  Original:   \"{original}\"");

    // strlen
    var strlen = module.GetFunction<int, int>("strlen");
    Console.WriteLine($"  strlen:     {strlen(0)}");

    // to_upper (in place)
    var toUpper = module.GetAction<int, int>("to_upper");
    mem.WriteString(0, original);
    toUpper(0, original.Length);
    Console.WriteLine($"  to_upper:   \"{mem.ReadString(0, original.Length)}\"");

    // to_lower (in place)
    var toLower = module.GetAction<int, int>("to_lower");
    mem.WriteString(0, original);
    toLower(0, original.Length);
    Console.WriteLine($"  to_lower:   \"{mem.ReadString(0, original.Length)}\"");

    // reverse (in place)
    var reverse = module.GetAction<int, int>("reverse");
    mem.WriteString(0, original);
    reverse(0, original.Length);
    Console.WriteLine($"  reverse:    \"{mem.ReadString(0, original.Length)}\"");

    // count_char
    var countChar = module.GetFunction<int, int, int, int>("count_char");
    mem.WriteString(0, original);
    var count = countChar(0, original.Length, (int)'l');
    Console.WriteLine($"  count 'l':  {count}");

    // rot13
    var rot13 = module.GetAction<int, int>("rot13");
    mem.WriteString(0, "Hello");
    rot13(0, 5);
    var encoded = mem.ReadString(0, 5);
    Console.Write($"  rot13:      \"Hello\" -> \"{encoded}\"");
    rot13(0, 5);
    Console.WriteLine($" -> \"{mem.ReadString(0, 5)}\"  (round-trip)");

    Console.WriteLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// Example 4: Fibonacci with per-step callbacks into C#
// ─────────────────────────────────────────────────────────────────────────────
static void RunFibonacciExample(string wasmDir)
{
    Console.WriteLine("── Example 4: Fibonacci with Progress Callbacks ───────────\n");

    using var engine = new WasmEngine();

    var values = new List<(int index, long value)>();
    engine.DefineFunction<int, long>("host", "on_value",
        (int index, long value) => values.Add((index, value)));

    var module = engine.LoadWatFile(Path.Combine(wasmDir, "fibonacci.wat"));

    var fibonacci = module.GetFunction<int, long>("fibonacci");
    var result = fibonacci(15);

    Console.Write("  Sequence: ");
    Console.WriteLine(string.Join(", ", values.Select(v => v.value)));
    Console.WriteLine($"  fib(15) = {result}");

    // Also demonstrate the memory-based variant
    var fibMem = module.GetFunction<int, int, int>("fibonacci_to_memory");
    var mem = module.GetSharedMemory();
    fibMem(10, 1024);  // Write fib(0)..fib(10) at offset 1024

    Console.Write("  Memory:   ");
    for (var i = 0; i <= 10; i++)
        Console.Write($"{mem.ReadInt32(1024 + i * 4)}{(i < 10 ? ", " : "")}");
    Console.WriteLine("\n");
}

// ─────────────────────────────────────────────────────────────────────────────
// Example 5: Bidirectional calculator (WASM math + C# advanced math)
// ─────────────────────────────────────────────────────────────────────────────
static void RunCalculatorExample(string wasmDir)
{
    Console.WriteLine("── Example 5: Bidirectional Calculator ─────────────────────\n");

    using var engine = new WasmEngine();

    // Provide C# math functions that WASM will call
    engine.DefineFunction<double, double>("math", "sqrt", Math.Sqrt);
    engine.DefineFunction<double, double, double>("math", "pow", Math.Pow);
    engine.DefineFunction<double, double>("math", "log", Math.Log);
    engine.DefineFunction<double, double>("math", "sin", Math.Sin);
    engine.DefineFunction<double, double>("math", "cos", Math.Cos);

    var module = engine.LoadWatFile(Path.Combine(wasmDir, "calculator.wat"));

    // Basic ops (pure WASM)
    var add = module.GetFunction<double, double, double>("add");
    var div = module.GetFunction<double, double, double>("divide");
    Console.WriteLine($"  add(2.5, 3.7)              = {add(2.5, 3.7)}");
    Console.WriteLine($"  divide(10, 3)              = {div(10, 3):F6}");
    Console.WriteLine($"  divide(1, 0)               = {div(1, 0)}  (NaN guard)");

    // Composite ops (WASM calling C# internally)
    var distance = module.GetFunction<double, double, double>("distance");
    Console.WriteLine($"  distance(3, 4)             = {distance(3, 4)}");

    var compoundInterest = module.GetFunction<double, double, double, double>("compound_interest");
    Console.WriteLine($"  compound(1000, 0.05, 10)   = {compoundInterest(1000, 0.05, 10):F2}");

    var discriminant = module.GetFunction<double, double, double, double>("quadratic_discriminant");
    Console.WriteLine($"  discriminant(1, -5, 6)     = {discriminant(1, -5, 6)}");

    var logBase = module.GetFunction<double, double, double>("log_base");
    Console.WriteLine($"  log_base(100, 10)          = {logBase(100, 10):F6}");

    var circleArea = module.GetFunction<double, double>("circle_area");
    Console.WriteLine($"  circle_area(5)             = {circleArea(5):F6}");

    var polarX = module.GetFunction<double, double, double>("polar_to_x");
    var polarY = module.GetFunction<double, double, double>("polar_to_y");
    var theta = Math.PI / 4; // 45 degrees
    Console.WriteLine($"  polar(r=1, 45deg) -> ({polarX(1, theta):F4}, {polarY(1, theta):F4})");

    Console.WriteLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// Example 6: Inline WAT — no files needed
// ─────────────────────────────────────────────────────────────────────────────
static void RunInlineWatExample()
{
    Console.WriteLine("── Example 6: Inline WAT (No Files) ───────────────────────\n");

    using var engine = new WasmEngine();

    // Define a C# "print" function for WASM to call
    engine.DefineFunction<int>("env", "print",
        (int value) => Console.WriteLine($"  [WASM -> C#] Printed: {value}"));

    const string wat = """
        (module
          (import "env" "print" (func $print (param i32)))
          (func $double_and_print (param $x i32)
            local.get $x
            i32.const 2
            i32.mul
            call $print
          )
          (export "double_and_print" (func $double_and_print))
        )
        """;

    var module = engine.LoadWatString("inline", wat);
    var doubleAndPrint = module.GetAction<int>("double_and_print");

    Console.WriteLine("  Calling WASM double_and_print(21):");
    doubleAndPrint(21);

    Console.WriteLine("\n  Module exports: " + string.Join(", ", module.ListExports()));
    Console.WriteLine();
}

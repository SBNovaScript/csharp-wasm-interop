// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026-present Steven Baumann
// Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
// See LICENSE and NOTICE in the repository root for details.

using Wasmtime;
using WasmInterop;

namespace WasmInterop.Tests;

public class ArithmeticTests
{
    private static string WasmDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "wasm"));

    [Fact]
    public void Add_ReturnsCorrectSum()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var add = module.GetFunction<int, int, int>("add");

        Assert.Equal(13, add(10, 3));
        Assert.Equal(0, add(0, 0));
        Assert.Equal(-1, add(2, -3));
    }

    [Fact]
    public void Subtract_ReturnsCorrectDifference()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var subtract = module.GetFunction<int, int, int>("subtract");

        Assert.Equal(7, subtract(10, 3));
        Assert.Equal(-3, subtract(2, 5));
    }

    [Fact]
    public void Multiply_ReturnsCorrectProduct()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var multiply = module.GetFunction<int, int, int>("multiply");

        Assert.Equal(30, multiply(10, 3));
        Assert.Equal(0, multiply(0, 100));
        Assert.Equal(-6, multiply(-2, 3));
    }

    [Fact]
    public void Divide_HandlesZeroDivisor()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var divide = module.GetFunction<int, int, int>("divide");

        Assert.Equal(3, divide(10, 3));
        Assert.Equal(0, divide(10, 0)); // guarded
    }

    [Fact]
    public void Modulo_ReturnsCorrectRemainder()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var modulo = module.GetFunction<int, int, int>("modulo");

        Assert.Equal(1, modulo(10, 3));
        Assert.Equal(0, modulo(10, 0)); // guarded
    }

    [Fact]
    public void Factorial_ComputesCorrectly()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var factorial = module.GetFunction<int, long>("factorial");

        Assert.Equal(1L, factorial(0));
        Assert.Equal(1L, factorial(1));
        Assert.Equal(120L, factorial(5));
        Assert.Equal(3628800L, factorial(10));
    }

    [Fact]
    public void Abs_ReturnsAbsoluteValue()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var abs = module.GetFunction<int, int>("abs");

        Assert.Equal(42, abs(-42));
        Assert.Equal(42, abs(42));
        Assert.Equal(0, abs(0));
    }

    [Fact]
    public void MaxMin_ReturnCorrectValues()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var max = module.GetFunction<int, int, int>("max");
        var min = module.GetFunction<int, int, int>("min");

        Assert.Equal(12, max(7, 12));
        Assert.Equal(7, min(7, 12));
        Assert.Equal(5, max(5, 5));
        Assert.Equal(5, min(5, 5));
    }

    [Fact]
    public void FloatOps_ReturnCorrectResults()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "arithmetic.wat"));
        var addF64 = module.GetFunction<double, double, double>("add_f64");
        var mulF64 = module.GetFunction<double, double, double>("multiply_f64");

        Assert.Equal(4.2, addF64(1.5, 2.7), precision: 10);
        Assert.Equal(13.5, mulF64(3.0, 4.5), precision: 10);
    }
}

public class CallbackTests
{
    private static string WasmDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "wasm"));

    [Fact]
    public void RunWithCallbacks_ReturnsCorrectResult()
    {
        using var engine = new WasmEngine();
        var progressLog = new List<(int current, int total)>();

        engine.DefineFunctionWithCaller<int, int>("host", "log_message",
            (Caller caller, int ptr, int len) => { /* ignore logs in test */ });
        engine.DefineFunction<int, int>("host", "on_progress",
            (int current, int total) => progressLog.Add((current, total)));
        engine.DefineFunction<int>("host", "on_complete", (int result) => { });
        engine.DefineFunction("host", "get_timestamp", () => 12345L);

        var module = engine.LoadWatFile(Path.Combine(WasmDir, "callbacks.wat"));
        var run = module.GetFunction<int, int>("run_with_callbacks");

        // sum of squares: 0^2 + 1^2 + 2^2 + 3^2 + 4^2 = 0 + 1 + 4 + 9 + 16 = 30
        var result = run(5);
        Assert.Equal(30, result);
        Assert.Equal(5, progressLog.Count);
        Assert.Equal((1, 5), progressLog[0]);
        Assert.Equal((5, 5), progressLog[^1]);
    }

    [Fact]
    public void RecordTimestamp_CallsCSharpFunction()
    {
        using var engine = new WasmEngine();

        engine.DefineFunctionWithCaller<int, int>("host", "log_message",
            (Caller caller, int ptr, int len) => { });
        engine.DefineFunction<int, int>("host", "on_progress", (int c, int t) => { });
        engine.DefineFunction<int>("host", "on_complete", (int r) => { });
        engine.DefineFunction("host", "get_timestamp", () => 99999L);

        var module = engine.LoadWatFile(Path.Combine(WasmDir, "callbacks.wat"));
        var recordTimestamp = module.GetFunction<long>("record_timestamp");

        Assert.Equal(99999L, recordTimestamp());
    }
}

public class StringOpsTests
{
    private static string WasmDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "wasm"));

    [Fact]
    public void Strlen_ReturnsCorrectLength()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "string_ops.wat"));
        var mem = module.GetSharedMemory();
        var strlen = module.GetFunction<int, int>("strlen");

        mem.WriteNullTerminatedString(0, "Hello");
        Assert.Equal(5, strlen(0));

        mem.WriteNullTerminatedString(0, "");
        Assert.Equal(0, strlen(0));
    }

    [Fact]
    public void ToUpper_ConvertsCorrectly()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "string_ops.wat"));
        var mem = module.GetSharedMemory();
        var toUpper = module.GetAction<int, int>("to_upper");

        const string input = "Hello, World!";
        mem.WriteString(0, input);
        toUpper(0, input.Length);

        Assert.Equal("HELLO, WORLD!", mem.ReadString(0, input.Length));
    }

    [Fact]
    public void ToLower_ConvertsCorrectly()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "string_ops.wat"));
        var mem = module.GetSharedMemory();
        var toLower = module.GetAction<int, int>("to_lower");

        const string input = "Hello, World!";
        mem.WriteString(0, input);
        toLower(0, input.Length);

        Assert.Equal("hello, world!", mem.ReadString(0, input.Length));
    }

    [Fact]
    public void Reverse_ReversesString()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "string_ops.wat"));
        var mem = module.GetSharedMemory();
        var reverse = module.GetAction<int, int>("reverse");

        const string input = "abcde";
        mem.WriteString(0, input);
        reverse(0, input.Length);

        Assert.Equal("edcba", mem.ReadString(0, input.Length));
    }

    [Fact]
    public void CountChar_CountsCorrectly()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "string_ops.wat"));
        var mem = module.GetSharedMemory();
        var countChar = module.GetFunction<int, int, int, int>("count_char");

        const string input = "banana";
        mem.WriteString(0, input);

        Assert.Equal(3, countChar(0, input.Length, 'a'));
        Assert.Equal(2, countChar(0, input.Length, 'n'));
        Assert.Equal(0, countChar(0, input.Length, 'z'));
    }

    [Fact]
    public void Rot13_IsSymmetric()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "string_ops.wat"));
        var mem = module.GetSharedMemory();
        var rot13 = module.GetAction<int, int>("rot13");

        const string input = "Hello";
        mem.WriteString(0, input);
        rot13(0, input.Length);
        var encoded = mem.ReadString(0, input.Length);

        Assert.Equal("Uryyb", encoded);

        // Applying rot13 twice should return original
        rot13(0, input.Length);
        Assert.Equal(input, mem.ReadString(0, input.Length));
    }
}

public class FibonacciTests
{
    private static string WasmDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "wasm"));

    [Theory]
    [InlineData(0, 0L)]
    [InlineData(1, 1L)]
    [InlineData(5, 5L)]
    [InlineData(10, 55L)]
    [InlineData(20, 6765L)]
    public void Fibonacci_ReturnsCorrectValues(int n, long expected)
    {
        using var engine = new WasmEngine();
        engine.DefineFunction<int, long>("host", "on_value", (int i, long v) => { });
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "fibonacci.wat"));
        var fibonacci = module.GetFunction<int, long>("fibonacci");

        Assert.Equal(expected, fibonacci(n));
    }

    [Fact]
    public void Fibonacci_CallsOnValueForEachStep()
    {
        using var engine = new WasmEngine();
        var values = new List<long>();
        engine.DefineFunction<int, long>("host", "on_value",
            (int index, long value) => values.Add(value));

        var module = engine.LoadWatFile(Path.Combine(WasmDir, "fibonacci.wat"));
        var fibonacci = module.GetFunction<int, long>("fibonacci");
        fibonacci(7);

        long[] expected = [0, 1, 1, 2, 3, 5, 8, 13];
        Assert.Equal(expected, values);
    }

    [Fact]
    public void FibonacciToMemory_WritesCorrectValues()
    {
        using var engine = new WasmEngine();
        engine.DefineFunction<int, long>("host", "on_value", (int i, long v) => { });
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "fibonacci.wat"));
        var mem = module.GetSharedMemory();
        var fibMem = module.GetFunction<int, int, int>("fibonacci_to_memory");

        fibMem(7, 0);

        int[] expected = [0, 1, 1, 2, 3, 5, 8, 13];
        for (var i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], mem.ReadInt32(i * 4));
    }
}

public class CalculatorTests
{
    private static string WasmDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "wasm"));

    private static WasmEngine CreateCalculatorEngine()
    {
        var engine = new WasmEngine();
        engine.DefineFunction<double, double>("math", "sqrt", Math.Sqrt);
        engine.DefineFunction<double, double, double>("math", "pow", Math.Pow);
        engine.DefineFunction<double, double>("math", "log", Math.Log);
        engine.DefineFunction<double, double>("math", "sin", Math.Sin);
        engine.DefineFunction<double, double>("math", "cos", Math.Cos);
        return engine;
    }

    [Fact]
    public void Distance_ComputesEuclidean()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var distance = module.GetFunction<double, double, double>("distance");

        Assert.Equal(5.0, distance(3.0, 4.0), precision: 10);
        Assert.Equal(0.0, distance(0.0, 0.0), precision: 10);
    }

    [Fact]
    public void CompoundInterest_ComputesCorrectly()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var compound = module.GetFunction<double, double, double, double>("compound_interest");

        Assert.Equal(1628.89, compound(1000, 0.05, 10), precision: 2);
    }

    [Fact]
    public void QuadraticDiscriminant_ComputesCorrectly()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var disc = module.GetFunction<double, double, double, double>("quadratic_discriminant");

        // x^2 - 5x + 6 = 0 => discriminant = 25 - 24 = 1
        Assert.Equal(1.0, disc(1, -5, 6), precision: 10);
        // x^2 + 1 = 0 => discriminant = 0 - 4 = -4 (no real roots)
        Assert.Equal(-4.0, disc(1, 0, 1), precision: 10);
    }

    [Fact]
    public void LogBase_ComputesCorrectly()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var logBase = module.GetFunction<double, double, double>("log_base");

        Assert.Equal(2.0, logBase(100, 10), precision: 10);
        Assert.Equal(3.0, logBase(8, 2), precision: 10);
    }

    [Fact]
    public void CircleArea_ComputesCorrectly()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var circleArea = module.GetFunction<double, double>("circle_area");

        Assert.Equal(Math.PI * 25, circleArea(5), precision: 6);
    }

    [Fact]
    public void PolarToCartesian_ComputesCorrectly()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var polarX = module.GetFunction<double, double, double>("polar_to_x");
        var polarY = module.GetFunction<double, double, double>("polar_to_y");

        var theta = Math.PI / 4;
        Assert.Equal(Math.Sqrt(2) / 2, polarX(1, theta), precision: 10);
        Assert.Equal(Math.Sqrt(2) / 2, polarY(1, theta), precision: 10);
    }

    [Fact]
    public void Divide_ReturnsNaNForZeroDivisor()
    {
        using var engine = CreateCalculatorEngine();
        var module = engine.LoadWatFile(Path.Combine(WasmDir, "calculator.wat"));
        var divide = module.GetFunction<double, double, double>("divide");

        Assert.True(double.IsNaN(divide(1, 0)));
    }
}

public class InlineWatTests
{
    [Fact]
    public void InlineWat_CSharpCallsWasm()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("test", """
            (module
              (func $add (param i32 i32) (result i32)
                local.get 0
                local.get 1
                i32.add)
              (export "add" (func $add)))
            """);

        var add = module.GetFunction<int, int, int>("add");
        Assert.Equal(42, add(40, 2));
    }

    [Fact]
    public void InlineWat_WasmCallsCSharp()
    {
        using var engine = new WasmEngine();
        var called = false;

        engine.DefineFunction("test", "notify", () => { called = true; });

        var module = engine.LoadWatString("test", """
            (module
              (import "test" "notify" (func $notify))
              (func $run (call $notify))
              (export "run" (func $run)))
            """);

        var run = module.GetAction("run");
        run();
        Assert.True(called);
    }

    [Fact]
    public void InlineWat_BidirectionalRoundTrip()
    {
        using var engine = new WasmEngine();

        // C# provides a doubling function
        engine.DefineFunction<int, int>("env", "double_it", (int x) => x * 2);

        // WASM calls C# double_it then adds 1
        var module = engine.LoadWatString("test", """
            (module
              (import "env" "double_it" (func $double_it (param i32) (result i32)))
              (func $double_plus_one (param $x i32) (result i32)
                local.get $x
                call $double_it
                i32.const 1
                i32.add)
              (export "double_plus_one" (func $double_plus_one)))
            """);

        var doublePlusOne = module.GetFunction<int, int>("double_plus_one");
        Assert.Equal(21, doublePlusOne(10)); // 10*2 + 1
        Assert.Equal(1, doublePlusOne(0));   // 0*2 + 1
    }
}

public class SharedMemoryTests
{
    [Fact]
    public void WriteAndReadString_RoundTrips()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("mem", """
            (module (memory (export "memory") 1))
            """);
        var mem = module.GetSharedMemory();

        const string value = "Hello, WASM!";
        var len = mem.WriteString(0, value);
        Assert.Equal(value.Length, len);
        Assert.Equal(value, mem.ReadString(0, len));
    }

    [Fact]
    public void WriteAndReadNullTerminatedString_RoundTrips()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("mem", """
            (module (memory (export "memory") 1))
            """);
        var mem = module.GetSharedMemory();

        const string value = "Test string";
        mem.WriteNullTerminatedString(0, value);
        Assert.Equal(value, mem.ReadNullTerminatedString(0));
    }

    [Fact]
    public void WriteAndReadInt32_RoundTrips()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("mem", """
            (module (memory (export "memory") 1))
            """);
        var mem = module.GetSharedMemory();

        mem.WriteInt32(0, 42);
        Assert.Equal(42, mem.ReadInt32(0));

        mem.WriteInt32(4, -100);
        Assert.Equal(-100, mem.ReadInt32(4));
    }

    [Fact]
    public void WriteAndReadBytes_RoundTrips()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("mem", """
            (module (memory (export "memory") 1))
            """);
        var mem = module.GetSharedMemory();

        byte[] data = [1, 2, 3, 4, 5];
        mem.WriteBytes(0, data);
        Assert.Equal(data, mem.ReadBytes(0, data.Length));
    }

    [Fact]
    public void SizeInBytes_ReportsCorrectSize()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("mem", """
            (module (memory (export "memory") 1))
            """);
        var mem = module.GetSharedMemory();

        // 1 page = 64KB = 65536 bytes
        Assert.Equal(65536, mem.SizeInBytes);
    }
}

public class WasmModuleTests
{
    [Fact]
    public void ListExports_ReturnsAllExports()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("test", """
            (module
              (func $a (result i32) i32.const 1)
              (func $b (result i32) i32.const 2)
              (export "alpha" (func $a))
              (export "beta" (func $b)))
            """);

        var exports = module.ListExports();
        Assert.Contains("alpha", exports);
        Assert.Contains("beta", exports);
        Assert.Equal(2, exports.Count);
    }

    [Fact]
    public void GetAction_ThrowsOnMissingExport()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("test", "(module)");

        Assert.Throws<WasmInteropException>(() => module.GetAction("nonexistent"));
    }

    [Fact]
    public void GetFunction_ThrowsOnMissingExport()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("test", "(module)");

        Assert.Throws<WasmInteropException>(() => module.GetFunction<int>("nonexistent"));
    }

    [Fact]
    public void GetMemory_ThrowsOnMissingMemory()
    {
        using var engine = new WasmEngine();
        var module = engine.LoadWatString("test", "(module)");

        Assert.Throws<WasmInteropException>(() => module.GetMemory());
    }
}

public class ErrorHandlingTests
{
    [Fact]
    public void LoadWatString_ThrowsOnInvalidWat()
    {
        using var engine = new WasmEngine();

        Assert.ThrowsAny<Exception>(() =>
            engine.LoadWatString("bad", "(module (invalid syntax here))"));
    }

    [Fact]
    public void LoadWatFile_ThrowsOnMissingFile()
    {
        using var engine = new WasmEngine();

        Assert.ThrowsAny<Exception>(() =>
            engine.LoadWatFile("/nonexistent/path.wat"));
    }

    [Fact]
    public void MissingImport_ThrowsOnInstantiate()
    {
        using var engine = new WasmEngine();

        // Module requires an import but we don't provide one
        Assert.ThrowsAny<Exception>(() =>
            engine.LoadWatString("test", """
                (module
                  (import "env" "missing" (func))
                  (func $run (call 0))
                  (export "run" (func $run)))
                """));
    }
}

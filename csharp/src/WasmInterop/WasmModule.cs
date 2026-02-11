// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026-present Steven Baumann
// Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
// See LICENSE and NOTICE in the repository root for details.

using Wasmtime;

namespace WasmInterop;

/// <summary>
/// Wraps an instantiated WASM module and provides typed access
/// to its exported functions, memory, and globals.
/// </summary>
public sealed class WasmModule : IDisposable
{
    private readonly Instance _instance;
    private readonly Store _store;
    private readonly Module _module;

    internal WasmModule(Instance instance, Store store, Module module)
    {
        _instance = instance;
        _store = store;
        _module = module;
    }

    public Instance Instance => _instance;

    // ── Void functions (Actions) ────────────────────────────────────

    /// <summary>
    /// Gets an exported void function with no parameters.
    /// </summary>
    public Action GetAction(string name) =>
        _instance.GetAction(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    /// <summary>
    /// Gets an exported void function with one parameter.
    /// </summary>
    public Action<T> GetAction<T>(string name) =>
        _instance.GetAction<T>(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    /// <summary>
    /// Gets an exported void function with two parameters.
    /// </summary>
    public Action<T1, T2> GetAction<T1, T2>(string name) =>
        _instance.GetAction<T1, T2>(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    // ── Functions with return values ────────────────────────────────
    // Struct constraints match WASM's value-type-only return types (i32/i64/f32/f64)
    // and eliminate nullability annotation mismatches with Wasmtime's API.

    /// <summary>
    /// Gets an exported function with no parameters that returns TResult.
    /// </summary>
    public Func<TResult> GetFunction<TResult>(string name)
        where TResult : struct =>
        _instance.GetFunction<TResult>(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    /// <summary>
    /// Gets an exported function with one parameter that returns TResult.
    /// </summary>
    public Func<T, TResult> GetFunction<T, TResult>(string name)
        where TResult : struct =>
        _instance.GetFunction<T, TResult>(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    /// <summary>
    /// Gets an exported function with two parameters that returns TResult.
    /// </summary>
    public Func<T1, T2, TResult> GetFunction<T1, T2, TResult>(string name)
        where TResult : struct =>
        _instance.GetFunction<T1, T2, TResult>(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    /// <summary>
    /// Gets an exported function with three parameters that returns TResult.
    /// </summary>
    public Func<T1, T2, T3, TResult> GetFunction<T1, T2, T3, TResult>(string name)
        where TResult : struct =>
        _instance.GetFunction<T1, T2, T3, TResult>(name)
        ?? throw new WasmInteropException($"Export '{name}' not found or has wrong signature.");

    // ── Memory access ───────────────────────────────────────────────

    /// <summary>
    /// Gets the module's exported linear memory by name (default: "memory").
    /// </summary>
    public Memory GetMemory(string name = "memory") =>
        _instance.GetMemory(name)
        ?? throw new WasmInteropException($"Memory export '{name}' not found.");

    /// <summary>
    /// Returns a <see cref="SharedMemory"/> helper for convenient read/write
    /// of strings and byte arrays through the module's linear memory.
    /// </summary>
    public SharedMemory GetSharedMemory(string name = "memory") =>
        new(GetMemory(name));

    // ── Convenience: try-get patterns ───────────────────────────────

    /// <summary>
    /// Attempts to get an exported function. Returns null on failure.
    /// </summary>
    public Func<T1, T2, TResult>? TryGetFunction<T1, T2, TResult>(string name)
        where TResult : struct =>
        _instance.GetFunction<T1, T2, TResult>(name);

    /// <summary>
    /// Lists the names of all exports on this module.
    /// </summary>
    public IReadOnlyList<string> ListExports()
    {
        var names = new List<string>();
        foreach (var export in _module.Exports)
        {
            names.Add(export.Name);
        }
        return names;
    }

    public void Dispose()
    {
        _module.Dispose();
    }
}

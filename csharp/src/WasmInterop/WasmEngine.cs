// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026-present Steven Baumann
// Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
// See LICENSE and NOTICE in the repository root for details.

using Wasmtime;

namespace WasmInterop;

/// <summary>
/// Manages the Wasmtime engine, store, and linker lifetime.
/// Acts as the entry point for loading and running WASM modules.
/// </summary>
public sealed class WasmEngine : IDisposable
{
    private readonly Engine _engine;
    private readonly Store _store;
    private readonly Linker _linker;
    private bool _disposed;

    public WasmEngine()
    {
        _engine = new Engine();
        _store = new Store(_engine);
        _linker = new Linker(_engine);
    }

    public Engine Engine => _engine;
    public Store Store => _store;
    public Linker Linker => _linker;

    /// <summary>
    /// Loads a WASM module from WebAssembly Text (WAT) source.
    /// All imports must be defined before calling this method.
    /// </summary>
    public WasmModule LoadWatString(string name, string watSource)
    {
        var module = Module.FromText(_engine, name, watSource);
        var instance = _linker.Instantiate(_store, module);
        return new WasmModule(instance, _store, module);
    }

    /// <summary>
    /// Loads a WASM module from a .wat file on disk.
    /// All imports must be defined before calling this method.
    /// </summary>
    public WasmModule LoadWatFile(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var watSource = File.ReadAllText(filePath);
        return LoadWatString(name, watSource);
    }

    /// <summary>
    /// Loads a WASM module from a compiled .wasm binary file.
    /// All imports must be defined before calling this method.
    /// </summary>
    public WasmModule LoadWasmFile(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var bytes = File.ReadAllBytes(filePath);
        var module = Module.FromBytes(_engine, name, bytes);
        var instance = _linker.Instantiate(_store, module);
        return new WasmModule(instance, _store, module);
    }

    // ── Define host functions (no Caller) ───────────────────────────
    // Struct constraints match WASM's value-type-only parameters (i32/i64/f32/f64)
    // and eliminate nullability annotation mismatches with Wasmtime's API.

    public void DefineFunction(string module, string name, Action callback)
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunction<T>(string module, string name, Action<T> callback)
        where T : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunction<T1, T2>(string module, string name, Action<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunction<T1, T2, T3>(string module, string name, Action<T1, T2, T3> callback)
        where T1 : struct where T2 : struct where T3 : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunction<TResult>(string module, string name, Func<TResult> callback)
        where TResult : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunction<T, TResult>(string module, string name, Func<T, TResult> callback)
        where T : struct where TResult : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunction<T1, T2, TResult>(string module, string name, Func<T1, T2, TResult> callback)
        where T1 : struct where T2 : struct where TResult : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    // ── Define host functions (with Caller for memory access) ───────
    // Caller is a ref struct, so we use the dedicated CallerAction/CallerFunc delegates.

    public void DefineFunctionWithCaller(string module, string name, CallerAction callback)
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunctionWithCaller<T1>(string module, string name, CallerAction<T1> callback)
        where T1 : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunctionWithCaller<T1, T2>(string module, string name, CallerAction<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunctionWithCaller<TResult>(string module, string name, CallerFunc<TResult> callback)
        where TResult : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void DefineFunctionWithCaller<T1, TResult>(string module, string name, CallerFunc<T1, TResult> callback)
        where T1 : struct where TResult : struct
    {
        _linker.Define(module, name,
            Function.FromCallback(_store, callback));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _store.Dispose();
        _linker.Dispose();
        _engine.Dispose();
    }
}

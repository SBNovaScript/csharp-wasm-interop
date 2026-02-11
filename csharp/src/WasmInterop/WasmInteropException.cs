// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026-present Steven Baumann
// Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
// See LICENSE and NOTICE in the repository root for details.

namespace WasmInterop;

/// <summary>
/// Thrown when a WASM interop operation fails
/// (missing export, type mismatch, etc.).
/// </summary>
public sealed class WasmInteropException : Exception
{
    public WasmInteropException(string message) : base(message) { }
    public WasmInteropException(string message, Exception inner) : base(message, inner) { }
}

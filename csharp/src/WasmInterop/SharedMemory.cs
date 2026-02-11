// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026-present Steven Baumann
// Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
// See LICENSE and NOTICE in the repository root for details.

using System.Text;
using Wasmtime;

namespace WasmInterop;

/// <summary>
/// Provides convenient read/write helpers over WASM linear memory
/// for passing strings and byte arrays between C# and WASM.
/// </summary>
public sealed class SharedMemory
{
    private readonly Memory _memory;

    internal SharedMemory(Memory memory)
    {
        _memory = memory;
    }

    public Memory Raw => _memory;

    /// <summary>
    /// Writes a UTF-8 string into linear memory at the given offset.
    /// Returns the number of bytes written.
    /// </summary>
    public int WriteString(long offset, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteBytes(offset, bytes);
        return bytes.Length;
    }

    /// <summary>
    /// Writes a null-terminated UTF-8 string into linear memory.
    /// Returns the total number of bytes written (including the null terminator).
    /// </summary>
    public int WriteNullTerminatedString(long offset, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteBytes(offset, bytes);
        _memory.WriteByte(offset + bytes.Length, 0);
        return bytes.Length + 1;
    }

    /// <summary>
    /// Reads a UTF-8 string from linear memory given offset and length.
    /// </summary>
    public string ReadString(long offset, int length) =>
        _memory.ReadString(offset, length);

    /// <summary>
    /// Reads a null-terminated UTF-8 string from linear memory.
    /// </summary>
    public string ReadNullTerminatedString(long offset)
    {
        // Scan for null terminator byte by byte
        var len = 0;
        while (_memory.ReadByte(offset + len) != 0)
            len++;
        return len == 0 ? string.Empty : _memory.ReadString(offset, len);
    }

    /// <summary>
    /// Writes a byte array into linear memory.
    /// </summary>
    public void WriteBytes(long offset, ReadOnlySpan<byte> data)
    {
        var span = _memory.GetSpan(offset, data.Length);
        data.CopyTo(span);
    }

    /// <summary>
    /// Reads bytes from linear memory into a new array.
    /// </summary>
    public byte[] ReadBytes(long offset, int length)
    {
        var span = _memory.GetSpan(offset, length);
        return span.ToArray();
    }

    /// <summary>
    /// Writes a 32-bit integer at the given byte offset.
    /// </summary>
    public void WriteInt32(long offset, int value) =>
        _memory.WriteInt32(offset, value);

    /// <summary>
    /// Reads a 32-bit integer from the given byte offset.
    /// </summary>
    public int ReadInt32(long offset) =>
        _memory.ReadInt32(offset);

    /// <summary>
    /// Returns the current size of linear memory in bytes.
    /// </summary>
    public long SizeInBytes => _memory.GetLength();
}

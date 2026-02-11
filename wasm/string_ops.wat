;; SPDX-License-Identifier: Apache-2.0
;; Copyright (c) 2026-present Steven Baumann
;; Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
;; See LICENSE and NOTICE in the repository root for details.

(module
  ;; String operations through shared linear memory.
  ;; C# writes UTF-8 bytes into memory, WASM processes them in place,
  ;; then C# reads the result back out.

  ;; ── Linear memory ─────────────────────────────────────────────────
  (memory (export "memory") 1)

  ;; ── String length ─────────────────────────────────────────────────
  ;; Returns the number of bytes from `ptr` until a zero byte.
  (func $strlen (param $ptr i32) (result i32)
    (local $len i32)
    i32.const 0
    local.set $len
    block $break
      loop $loop
        local.get $ptr
        local.get $len
        i32.add
        i32.load8_u
        i32.eqz
        br_if $break

        local.get $len
        i32.const 1
        i32.add
        local.set $len
        br $loop
      end
    end
    local.get $len
  )

  ;; ── To uppercase (ASCII) ──────────────────────────────────────────
  ;; Converts `len` bytes at `ptr` to uppercase in place.
  (func $to_upper (param $ptr i32) (param $len i32)
    (local $i i32)
    (local $ch i32)
    i32.const 0
    local.set $i
    block $break
      loop $loop
        local.get $i
        local.get $len
        i32.ge_s
        br_if $break

        ;; Load byte
        local.get $ptr
        local.get $i
        i32.add
        i32.load8_u
        local.set $ch

        ;; If 'a' <= ch <= 'z', subtract 32
        local.get $ch
        i32.const 97   ;; 'a'
        i32.ge_u
        local.get $ch
        i32.const 122  ;; 'z'
        i32.le_u
        i32.and
        if
          local.get $ptr
          local.get $i
          i32.add
          local.get $ch
          i32.const 32
          i32.sub
          i32.store8
        end

        local.get $i
        i32.const 1
        i32.add
        local.set $i
        br $loop
      end
    end
  )

  ;; ── To lowercase (ASCII) ──────────────────────────────────────────
  (func $to_lower (param $ptr i32) (param $len i32)
    (local $i i32)
    (local $ch i32)
    i32.const 0
    local.set $i
    block $break
      loop $loop
        local.get $i
        local.get $len
        i32.ge_s
        br_if $break

        local.get $ptr
        local.get $i
        i32.add
        i32.load8_u
        local.set $ch

        ;; If 'A' <= ch <= 'Z', add 32
        local.get $ch
        i32.const 65   ;; 'A'
        i32.ge_u
        local.get $ch
        i32.const 90   ;; 'Z'
        i32.le_u
        i32.and
        if
          local.get $ptr
          local.get $i
          i32.add
          local.get $ch
          i32.const 32
          i32.add
          i32.store8
        end

        local.get $i
        i32.const 1
        i32.add
        local.set $i
        br $loop
      end
    end
  )

  ;; ── Reverse bytes in place ────────────────────────────────────────
  (func $reverse (param $ptr i32) (param $len i32)
    (local $left i32)
    (local $right i32)
    (local $tmp i32)
    local.get $ptr
    local.set $left
    local.get $ptr
    local.get $len
    i32.add
    i32.const 1
    i32.sub
    local.set $right

    block $break
      loop $loop
        local.get $left
        local.get $right
        i32.ge_u
        br_if $break

        ;; Swap bytes at left and right
        local.get $left
        i32.load8_u
        local.set $tmp

        local.get $left
        local.get $right
        i32.load8_u
        i32.store8

        local.get $right
        local.get $tmp
        i32.store8

        local.get $left
        i32.const 1
        i32.add
        local.set $left
        local.get $right
        i32.const 1
        i32.sub
        local.set $right

        br $loop
      end
    end
  )

  ;; ── Count occurrences of a byte ───────────────────────────────────
  (func $count_char (param $ptr i32) (param $len i32) (param $target i32) (result i32)
    (local $i i32)
    (local $count i32)
    i32.const 0
    local.set $i
    i32.const 0
    local.set $count
    block $break
      loop $loop
        local.get $i
        local.get $len
        i32.ge_s
        br_if $break

        local.get $ptr
        local.get $i
        i32.add
        i32.load8_u
        local.get $target
        i32.eq
        if
          local.get $count
          i32.const 1
          i32.add
          local.set $count
        end

        local.get $i
        i32.const 1
        i32.add
        local.set $i
        br $loop
      end
    end
    local.get $count
  )

  ;; ── ROT13 cipher ──────────────────────────────────────────────────
  (func $rot13 (param $ptr i32) (param $len i32)
    (local $i i32)
    (local $ch i32)
    i32.const 0
    local.set $i
    block $break
      loop $loop
        local.get $i
        local.get $len
        i32.ge_s
        br_if $break

        local.get $ptr
        local.get $i
        i32.add
        i32.load8_u
        local.set $ch

        ;; Handle uppercase A-Z
        local.get $ch
        i32.const 65
        i32.ge_u
        local.get $ch
        i32.const 90
        i32.le_u
        i32.and
        if
          local.get $ptr
          local.get $i
          i32.add
          local.get $ch
          i32.const 65
          i32.sub
          i32.const 13
          i32.add
          i32.const 26
          i32.rem_u
          i32.const 65
          i32.add
          i32.store8
        end

        ;; Handle lowercase a-z
        local.get $ch
        i32.const 97
        i32.ge_u
        local.get $ch
        i32.const 122
        i32.le_u
        i32.and
        if
          local.get $ptr
          local.get $i
          i32.add
          local.get $ch
          i32.const 97
          i32.sub
          i32.const 13
          i32.add
          i32.const 26
          i32.rem_u
          i32.const 97
          i32.add
          i32.store8
        end

        local.get $i
        i32.const 1
        i32.add
        local.set $i
        br $loop
      end
    end
  )

  (export "strlen" (func $strlen))
  (export "to_upper" (func $to_upper))
  (export "to_lower" (func $to_lower))
  (export "reverse" (func $reverse))
  (export "count_char" (func $count_char))
  (export "rot13" (func $rot13))
)

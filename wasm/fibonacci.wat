;; SPDX-License-Identifier: Apache-2.0
;; Copyright (c) 2026-present Steven Baumann
;; Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
;; See LICENSE and NOTICE in the repository root for details.

(module
  ;; Fibonacci computation with progress callbacks into C#.
  ;; Demonstrates iterative algorithm + host notification at every step.

  ;; ── Imports ───────────────────────────────────────────────────────
  (import "host" "on_value" (func $on_value (param i32 i64)))  ;; (index, fib_value)

  ;; ── Memory (exported so C# can inspect it) ────────────────────────
  (memory (export "memory") 1)

  ;; ── Iterative Fibonacci with callback ─────────────────────────────
  ;; Computes fib(n) iteratively. Calls on_value(i, fib(i)) for each i.
  (func $fibonacci (param $n i32) (result i64)
    (local $i i32)
    (local $prev i64)
    (local $curr i64)
    (local $tmp i64)

    ;; fib(0) = 0
    local.get $n
    i32.const 0
    i32.le_s
    if (result i64)
      i32.const 0
      i64.const 0
      call $on_value
      i64.const 0
    else
      ;; fib(1) = 1
      local.get $n
      i32.const 1
      i32.eq
      if (result i64)
        i32.const 0
        i64.const 0
        call $on_value
        i32.const 1
        i64.const 1
        call $on_value
        i64.const 1
      else
        ;; General case
        i64.const 0
        local.set $prev
        i64.const 1
        local.set $curr

        ;; Report fib(0) and fib(1)
        i32.const 0
        i64.const 0
        call $on_value
        i32.const 1
        i64.const 1
        call $on_value

        i32.const 2
        local.set $i

        block $break
          loop $loop
            local.get $i
            local.get $n
            i32.gt_s
            br_if $break

            ;; tmp = prev + curr
            local.get $prev
            local.get $curr
            i64.add
            local.set $tmp

            ;; Shift: prev = curr, curr = tmp
            local.get $curr
            local.set $prev
            local.get $tmp
            local.set $curr

            ;; Callback with (i, curr)
            local.get $i
            local.get $curr
            call $on_value

            local.get $i
            i32.const 1
            i32.add
            local.set $i
            br $loop
          end
        end
        local.get $curr
      end
    end
  )

  ;; ── Store Fibonacci sequence into memory ──────────────────────────
  ;; Writes fib(0)..fib(n) as i32 values at consecutive 4-byte offsets
  ;; starting at `ptr`. Returns fib(n).
  (func $fibonacci_to_memory (param $n i32) (param $ptr i32) (result i32)
    (local $i i32)
    (local $prev i32)
    (local $curr i32)
    (local $tmp i32)

    ;; fib(0)
    local.get $ptr
    i32.const 0
    i32.store

    local.get $n
    i32.const 0
    i32.le_s
    if (result i32)
      i32.const 0
    else
      ;; fib(1)
      local.get $ptr
      i32.const 4
      i32.add
      i32.const 1
      i32.store

      i32.const 0
      local.set $prev
      i32.const 1
      local.set $curr
      i32.const 2
      local.set $i

      block $break
        loop $loop
          local.get $i
          local.get $n
          i32.gt_s
          br_if $break

          local.get $prev
          local.get $curr
          i32.add
          local.set $tmp

          local.get $curr
          local.set $prev
          local.get $tmp
          local.set $curr

          ;; Store at ptr + i*4
          local.get $ptr
          local.get $i
          i32.const 4
          i32.mul
          i32.add
          local.get $curr
          i32.store

          local.get $i
          i32.const 1
          i32.add
          local.set $i
          br $loop
        end
      end
      local.get $curr
    end
  )

  (export "fibonacci" (func $fibonacci))
  (export "fibonacci_to_memory" (func $fibonacci_to_memory))
)

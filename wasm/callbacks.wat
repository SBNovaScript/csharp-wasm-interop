;; SPDX-License-Identifier: Apache-2.0
;; Copyright (c) 2026-present Steven Baumann
;; Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
;; See LICENSE and NOTICE in the repository root for details.

(module
  ;; Demonstrates WASM calling back INTO C#.
  ;; Every imported function is provided by the C# host at link time.

  ;; ── Imports from C# host ──────────────────────────────────────────
  (import "host" "log_message"    (func $log_message (param i32 i32)))  ;; (ptr, len)
  (import "host" "on_progress"    (func $on_progress (param i32 i32)))  ;; (current, total)
  (import "host" "on_complete"    (func $on_complete (param i32)))       ;; (result)
  (import "host" "get_timestamp"  (func $get_timestamp (result i64)))    ;; () -> i64

  ;; ── Linear memory (shared with C#) ────────────────────────────────
  (memory (export "memory") 1)

  ;; Static strings baked into the data segment
  (data (i32.const 0)   "Starting computation")     ;; 20 bytes @ 0
  (data (i32.const 32)  "Processing step")           ;; 15 bytes @ 32
  (data (i32.const 64)  "Computation complete")      ;; 20 bytes @ 64
  (data (i32.const 96)  "Timestamp recorded")        ;; 18 bytes @ 96

  ;; ── Exported functions ────────────────────────────────────────────

  ;; Run a simulated workload that calls back into C# at each step.
  (func $run_with_callbacks (param $steps i32) (result i32)
    (local $i i32)
    (local $sum i32)

    ;; Log "Starting computation"
    i32.const 0
    i32.const 20
    call $log_message

    i32.const 0
    local.set $i
    i32.const 0
    local.set $sum

    block $break
      loop $loop
        local.get $i
        local.get $steps
        i32.ge_s
        br_if $break

        ;; Accumulate: sum += (i * i)
        local.get $sum
        local.get $i
        local.get $i
        i32.mul
        i32.add
        local.set $sum

        ;; Report progress: on_progress(i + 1, steps)
        local.get $i
        i32.const 1
        i32.add
        local.get $steps
        call $on_progress

        local.get $i
        i32.const 1
        i32.add
        local.set $i
        br $loop
      end
    end

    ;; Log "Computation complete"
    i32.const 64
    i32.const 20
    call $log_message

    ;; Notify host with the result
    local.get $sum
    call $on_complete

    local.get $sum
  )

  ;; Record a timestamp from the host and return it.
  (func $record_timestamp (result i64)
    call $get_timestamp
  )

  ;; Write a dynamic message into memory and ask the host to log it.
  ;; Encodes "Step N" at offset 128.
  (func $log_step_number (param $n i32)
    ;; Write "Step " (5 bytes) at offset 128
    (i32.store8 (i32.const 128) (i32.const 83))   ;; 'S'
    (i32.store8 (i32.const 129) (i32.const 116))  ;; 't'
    (i32.store8 (i32.const 130) (i32.const 101))  ;; 'e'
    (i32.store8 (i32.const 131) (i32.const 112))  ;; 'p'
    (i32.store8 (i32.const 132) (i32.const 32))   ;; ' '

    ;; Write the digit (works for 0-9)
    (i32.store8 (i32.const 133) (i32.add (i32.const 48) (local.get $n)))

    ;; Call host to log: ptr=128, len=6
    i32.const 128
    i32.const 6
    call $log_message
  )

  (export "run_with_callbacks" (func $run_with_callbacks))
  (export "record_timestamp" (func $record_timestamp))
  (export "log_step_number" (func $log_step_number))
)

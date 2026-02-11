;; SPDX-License-Identifier: Apache-2.0
;; Copyright (c) 2026-present Steven Baumann
;; Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
;; See LICENSE and NOTICE in the repository root for details.

(module
  ;; Pure WASM arithmetic operations — called FROM C#.
  ;; No imports; every function is self-contained and exported.

  (func $add (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add
  )

  (func $subtract (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.sub
  )

  (func $multiply (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.mul
  )

  (func $divide (param $a i32) (param $b i32) (result i32)
    ;; Guard against division by zero: return 0
    local.get $b
    i32.eqz
    if (result i32)
      i32.const 0
    else
      local.get $a
      local.get $b
      i32.div_s
    end
  )

  (func $modulo (param $a i32) (param $b i32) (result i32)
    local.get $b
    i32.eqz
    if (result i32)
      i32.const 0
    else
      local.get $a
      local.get $b
      i32.rem_s
    end
  )

  (func $factorial (param $n i32) (result i64)
    (local $result i64)
    (local $i i32)
    i64.const 1
    local.set $result
    i32.const 1
    local.set $i
    block $break
      loop $loop
        local.get $i
        local.get $n
        i32.gt_s
        br_if $break

        local.get $result
        local.get $i
        i64.extend_i32_s
        i64.mul
        local.set $result

        local.get $i
        i32.const 1
        i32.add
        local.set $i
        br $loop
      end
    end
    local.get $result
  )

  (func $abs (param $a i32) (result i32)
    local.get $a
    i32.const 0
    i32.lt_s
    if (result i32)
      i32.const 0
      local.get $a
      i32.sub
    else
      local.get $a
    end
  )

  (func $max (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.gt_s
    if (result i32)
      local.get $a
    else
      local.get $b
    end
  )

  (func $min (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.lt_s
    if (result i32)
      local.get $a
    else
      local.get $b
    end
  )

  ;; Float operations
  (func $add_f64 (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $b
    f64.add
  )

  (func $multiply_f64 (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $b
    f64.mul
  )

  (export "add" (func $add))
  (export "subtract" (func $subtract))
  (export "multiply" (func $multiply))
  (export "divide" (func $divide))
  (export "modulo" (func $modulo))
  (export "factorial" (func $factorial))
  (export "abs" (func $abs))
  (export "max" (func $max))
  (export "min" (func $min))
  (export "add_f64" (func $add_f64))
  (export "multiply_f64" (func $multiply_f64))
)

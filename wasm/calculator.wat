;; SPDX-License-Identifier: Apache-2.0
;; Copyright (c) 2026-present Steven Baumann
;; Original repository: https://github.com/SBNovaScript/csharp-wasm-interop
;; See LICENSE and NOTICE in the repository root for details.

(module
  ;; Full bidirectional calculator.
  ;; WASM handles basic arithmetic; it imports advanced math from C#
  ;; (sqrt, pow, log) and uses them to build composite operations.

  ;; ── Imports: advanced math provided by C# ─────────────────────────
  (import "math" "sqrt" (func $sqrt (param f64) (result f64)))
  (import "math" "pow"  (func $pow  (param f64 f64) (result f64)))
  (import "math" "log"  (func $log  (param f64) (result f64)))
  (import "math" "sin"  (func $sin  (param f64) (result f64)))
  (import "math" "cos"  (func $cos  (param f64) (result f64)))

  ;; ── Basic operations (pure WASM) ──────────────────────────────────
  (func $add (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $b
    f64.add
  )

  (func $subtract (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $b
    f64.sub
  )

  (func $multiply (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $b
    f64.mul
  )

  (func $divide (param $a f64) (param $b f64) (result f64)
    local.get $b
    f64.const 0
    f64.eq
    if (result f64)
      f64.const nan
    else
      local.get $a
      local.get $b
      f64.div
    end
  )

  ;; ── Composite operations (WASM arithmetic + C# math) ──────────────

  ;; Euclidean distance: sqrt(a^2 + b^2)
  (func $distance (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $a
    f64.mul
    local.get $b
    local.get $b
    f64.mul
    f64.add
    call $sqrt
  )

  ;; Hypotenuse (alias for distance)
  (func $hypot (param $a f64) (param $b f64) (result f64)
    local.get $a
    local.get $b
    call $distance
  )

  ;; Compound interest: principal * pow(1 + rate, periods)
  (func $compound_interest (param $principal f64) (param $rate f64) (param $periods f64) (result f64)
    local.get $principal
    f64.const 1.0
    local.get $rate
    f64.add
    local.get $periods
    call $pow
    f64.mul
  )

  ;; Quadratic formula — returns the discriminant.
  ;; Full roots must be computed on the C# side using this value.
  ;; discriminant = b^2 - 4ac
  (func $quadratic_discriminant (param $a f64) (param $b f64) (param $c f64) (result f64)
    local.get $b
    local.get $b
    f64.mul
    f64.const 4.0
    local.get $a
    f64.mul
    local.get $c
    f64.mul
    f64.sub
  )

  ;; Natural-log-based change-of-base: log_base(x) = ln(x) / ln(base)
  (func $log_base (param $x f64) (param $base f64) (result f64)
    local.get $x
    call $log
    local.get $base
    call $log
    f64.div
  )

  ;; Circle area: pi * r^2   (uses C# pow)
  (func $circle_area (param $radius f64) (result f64)
    f64.const 3.141592653589793
    local.get $radius
    f64.const 2.0
    call $pow
    f64.mul
  )

  ;; Polar to cartesian X: r * cos(theta)
  (func $polar_to_x (param $r f64) (param $theta f64) (result f64)
    local.get $r
    local.get $theta
    call $cos
    f64.mul
  )

  ;; Polar to cartesian Y: r * sin(theta)
  (func $polar_to_y (param $r f64) (param $theta f64) (result f64)
    local.get $r
    local.get $theta
    call $sin
    f64.mul
  )

  (export "add" (func $add))
  (export "subtract" (func $subtract))
  (export "multiply" (func $multiply))
  (export "divide" (func $divide))
  (export "distance" (func $distance))
  (export "hypot" (func $hypot))
  (export "compound_interest" (func $compound_interest))
  (export "quadratic_discriminant" (func $quadratic_discriminant))
  (export "log_base" (func $log_base))
  (export "circle_area" (func $circle_area))
  (export "polar_to_x" (func $polar_to_x))
  (export "polar_to_y" (func $polar_to_y))
)

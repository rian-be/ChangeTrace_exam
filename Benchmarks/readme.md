# ChangeTrace Benchmarks

ChangeTrace Benchmarks contains repeatable BenchmarkDotNet scenarios for performance-sensitive CPU paths.

The benchmark project exists to measure the CPU side render pipeline before OpenGL/GPU execution. It focuses on timeline event translation, layout computation, render state assembly, scene snapshot generation, visibility planning, frame preparation, and GPU buffer data preparation.

> [!IMPORTANT]
> These benchmarks are for local performance investigation and regression checks. They are not part of the normal application runtime.

> [!WARNING]
> Benchmark results depend on hardware, runtime version, operating system scheduling, power mode, and background processes. Compare results from the same machine and environment when possible.

Use it when you want a repeatable way to check whether render pipeline changes affect CPU time, managed allocations, or scaling with larger timelines.

The usual flow is simple:

- run a focused benchmark group
- compare execution time and allocations
- inspect generated BenchmarkDotNet reports
- use the results before starting deeper optimization work

## What it measures

The benchmark suite currently covers:

- semantic render event translation from timeline events
- hive layout computation and animated convergence
- render state assembly from timeline events
- scene frame update and snapshot preparation
- isolated scene snapshot assembly
- edge visibility planning through scene snapshot assembly
- CPU-side GPU buffer contract preparation
- render frame submission preparation before OpenGL upload

Additional player benchmarks are available for playback cursor, seek, stepper, and player factory costs. They are kept in the same benchmark project, but the primary suite tracks issue #17 and the CPU side render pipeline.

The benchmarks intentionally avoid opening OpenTK windows or requiring GPU access.

## Commands

Run all benchmarks:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*Benchmarks*"
```

Run the full CPU-side render pipeline suite:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*Rendering*"
```

Run render event translation benchmarks:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*RenderEventTranslationBenchmarks*"
```

Run scene snapshot assembly benchmarks:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*SceneSnapshotAssemblyBenchmarks*"
```

Run layout benchmarks:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*HiveLayoutBenchmarks*"
```

Run player benchmarks:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*Player*"
```

Run one benchmark group with a shorter sanity check:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*RenderStateAssemblyBenchmarks*" --warmupCount 1 --iterationCount 3
```

Run one player benchmark group with a shorter sanity check:

```bash
dotnet run -c Release --project Benchmarks/ChangeTrace.Benchmarks.csproj -- --filter "*EventCursorBenchmarks*" --warmupCount 1 --iterationCount 3
```

Or through Task:

```bash
task benchmark
```

## Project Structure

The benchmark project is split by measured subsystem:

- `Rendering/Benchmarks` contains render pipeline benchmark entry points
- `Rendering/Fixtures` contains shared render benchmark data setup
- `Rendering/Gpu` contains CPU-side GPU data preparation benchmarks
- `Rendering/Layout` contains layout engine benchmarks
- `Player/Benchmarks` contains player and playback benchmark entry points
- `Player/Fixtures` contains shared player timeline setup

The render benchmark suite maps to issue #17:

- dedicated `Benchmarks/ChangeTrace.Benchmarks.csproj` project
- BenchmarkDotNet dependency and memory diagnostics
- `1k`, `10k`, and `100k` synthetic event sizes
- timeline-to-render-command translation benchmark
- hive layout benchmark
- render state and scene snapshot assembly benchmarks
- CPU-side GPU buffer data preparation benchmark
- local run commands outside the normal application build flow

## Reports

BenchmarkDotNet writes reports under:

```text
BenchmarkDotNet.Artifacts/results/
```

The most useful files are usually:

- `*-report-github.md` for pull request comments or issue updates
- `*-report.html` for local inspection
- `*-report.csv` for comparisons and spreadsheets

## Notes

Avoid `--job Dry` for performance readings. It forces very short runs and can produce misleading `MinIterationTime` warnings even when benchmarks are configured with a higher minimum iteration time.

The `Failed to set up priority High` message can appear on Linux when the current user cannot raise process priority. It does not mean the benchmark failed.

Real GPU/OpenTK frame benchmarking should be handled separately because results depend heavily on hardware, drivers, VSync, resolution, and windowing environment.

---

ChangeTrace Benchmarks are built for repeatable local performance checks before optimization work.

[Back to top](#changetrace-benchmarks)

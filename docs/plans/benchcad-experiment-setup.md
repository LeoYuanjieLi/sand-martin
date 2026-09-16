# BenchCAD Experiment Plan

## Objective

Build a separate, reproducible benchmark repository that evaluates one BenchCAD
Vision2Code row at a time:

```text
BenchCAD view -> fresh Codex CLI run -> Sand Martin MCP -> Grasshopper solid
              -> STEP export -> BenchCAD voxel IoU -> JSONL result
```

The first milestone is a four-row smoke run with the strongest available model.
Once this baseline is stable, run the same experiment with cheaper models while
changing only the model configuration.

## Repository boundary

### `sand-martin`

Keep only reusable Rhino/Grasshopper capabilities here:

- Sand Martin MCP server and tools.
- A private `reset_canvas` host operation.
- A private `export_output` host operation.
- Host-level tests for reset, recompute, output validation, and STEP export.

The private operations are controlled by the benchmark runner, not exposed to
the model as MCP tools.

### `sand-martin-benchmarks`

Create a separate repository for all experimental concerns:

```text
sand-martin-benchmarks/
├── experiments/benchcad/
│   ├── configs/smoke.yaml
│   ├── cli.py
│   ├── dataset.py
│   ├── codex_runner.py
│   ├── controller.py
│   ├── scoring.py
│   └── results.py
├── tests/
├── third_party/BenchCAD/     # submodule pinned to a commit
├── cache/                    # gitignored prepared inputs and ground truth
├── runs/                     # gitignored traces, geometry, and results
├── pyproject.toml
└── README.md
```

This repository owns dataset access, Codex orchestration, scoring, experiment
configuration, artifacts, and result reporting. It records the expected Sand
Martin commit but does not copy Sand Martin implementation code.

## Baseline contract

- Task: BenchCAD Vision2Code.
- Model input: one `composite_png` plus a fixed minimal instruction.
- Model tools: Sand Martin MCP only.
- Hidden from the model: ground-truth code and STEP, metadata, previous rows,
  both repositories, and other local files.
- Isolation: a new empty working directory, fresh Codex process, and reset
  Grasshopper canvas for every row.
- Output: exactly one component nicknamed `OUTPUT`; output index `0`
  must contain exactly one closed solid.
- Failure rule: invalid output, timeout, model error, or any non-Sand-Martin tool
  call receives IoU `0`.
- Score: BenchCAD 64^3 voxel IoU after its standard geometry normalization.

## Pinned run configuration

Each run starts from a committed YAML configuration:

```yaml
run_id: baseline-smoke-v1

dataset:
  repo: BenchCAD/BenchCAD
  revision: <hugging-face-revision>
  benchcad_commit: <git-commit>
  records:
    - washer_000001_s20260505
    - hex_nut_000006_s20260505
    - spacer_ring_000007_s20260505
    - bolt_000008_s20260505

sand_martin:
  commit: <git-commit>
  host_url: <private-host-url>
  mcp_command: <absolute-python>
  mcp_args: [<absolute-server.py>]

codex:
  executable: /usr/local/bin/codex
  version: 0.153.4
  model: <strongest-available-model>
  reasoning_effort: <level>
  timeout_seconds: 600

prompt_version: baseline-v1
```

At run start, resolve the configuration and save an immutable copy in the run
directory. Refuse to resume the run if any pinned value changes.

## Implementation steps

### 1. Add Sand Martin host support

Implement and test two private operations:

1. `reset_canvas`
   - Remove nodes created by the previous row.
   - Preserve the running Sand Martin server.
   - Recompute and confirm the canvas is ready.
2. `export_output`
   - Force and await a Grasshopper solution.
   - Find the unique `OUTPUT` component.
   - Read output index `0`.
   - Validate exactly one closed solid.
   - Export it to STEP through a temporary Rhino document.

Solution waiting belongs inside export; no separate `wait_for_solution` API is
needed for the initial experiment.

### 2. Scaffold the benchmark repository

- Initialize `sand-martin-benchmarks` with Python 3.11 and a locked dependency
  set.
- Add BenchCAD as a submodule pinned to a reviewed commit.
- Add `cache/` and `runs/` to `.gitignore`.
- Add the smoke configuration and a CLI with `prepare`, `run`, and `regrade`.

### 3. Implement `prepare`

For each configured record:

1. Load the row from the pinned Hugging Face dataset revision.
2. Save `composite_png` as the model input image.
3. Execute the trusted ground-truth CadQuery code once to create a STEP file.
4. Write a local manifest containing only record IDs and artifact paths.

Ground-truth code and STEP files remain private preparation artifacts and must
never enter prompts or Codex traces.

### 4. Implement the Codex runner

For each row, launch a new non-interactive process equivalent to:

```bash
codex exec \
  --ephemeral \
  --ignore-user-config \
  --ignore-rules \
  --json \
  --sandbox read-only \
  --cd <empty-row-workdir> \
  --image <input.png> \
  --model <model-id> \
  -c 'model_reasoning_effort="<level>"' \
  -c 'mcp_servers.sand-martin.command="<absolute-python>"' \
  -c 'mcp_servers.sand-martin.args=["<absolute-server.py>"]' \
  -c 'mcp_servers.sand-martin.required=true' \
  "<baseline-prompt>"
```

Before starting, verify the Codex version and hash its executable. Capture the
complete JSONL event stream, exit code, token usage, and elapsed time. Validate
the trace and mark any non-Sand-Martin tool call as `protocol_violation`.

Authentication comes from the operator's existing Codex login and is never
copied into configurations or artifacts.

### 5. Implement the per-row lifecycle

For every record without a completed result:

1. Reset the experiment canvas.
2. Run Codex with the row image and fixed prompt.
3. Validate the Codex trace.
4. Export `OUTPUT` to STEP.
5. Calculate IoU against the prepared ground-truth STEP.
6. Save all artifacts.
7. Append one result object immediately.

Every attempt produces a result row, including failures, so interrupted runs
can resume without silently dropping or repeating records.

### 6. Persist artifacts and results

```text
runs/<run_id>/
├── resolved-config.yaml
├── results.jsonl
└── records/<record_id>/
    ├── input.png
    ├── trace.jsonl
    ├── output.gh
    ├── output.step
    └── output.png
```

Each `results.jsonl` object records at least:

```json
{
  "run_id": "baseline-smoke-v1",
  "record_id": "washer_000001_s20260505",
  "status": "ok",
  "iou": 0.72,
  "codex_cli_version": "0.153.4",
  "codex_binary_sha256": "<sha256>",
  "model": "<model-id>",
  "reasoning_effort": "<level>",
  "tool_calls": 4,
  "prompt_tokens": 1000,
  "completion_tokens": 800,
  "reasoning_tokens": 2000,
  "total_latency_s": 34.8,
  "codex_exit_code": 0,
  "error": null
}
```

Statuses are `ok`, `codex_error`, `timeout`, `protocol_violation`,
`missing_output`, `invalid_geometry`, `export_error`, or `score_error`.

### 7. Test and run the smoke baseline

Test dataset preparation, configuration locking, resume behavior, trace-policy
validation, canvas isolation, STEP export, and deterministic regrading. Then run:

```bash
python -m experiments.benchcad.cli prepare \
  --config experiments/benchcad/configs/smoke.yaml

python -m experiments.benchcad.cli run \
  --config experiments/benchcad/configs/smoke.yaml

python -m experiments.benchcad.cli regrade \
  --run-id baseline-smoke-v1
```

## Acceptance criteria

- All four smoke records run without manual per-row intervention.
- Every attempt produces exactly one result object.
- Ground truth never appears in model-visible context or traces.
- Canvas, filesystem, and conversation state do not leak between rows.
- Only Sand Martin MCP calls appear in successful traces.
- Interrupted runs resume without repeating completed rows.
- Regrading saved STEP files reproduces stored IoU values.
- A second model can be evaluated by changing only `run_id`, `model`, and any
  explicitly declared reasoning setting.

## Follow-up experiment

After the strongest-model baseline passes the acceptance criteria, select one
or more cheaper models available through the same Codex CLI. Keep the dataset,
prompt, Sand Martin version, tool policy, timeout, and scorer fixed. Compare
mean IoU, successful-execution rate, tokens, tool calls, and latency; treat cost
as unavailable for subscription-authenticated runs unless separately estimated
and clearly labeled.

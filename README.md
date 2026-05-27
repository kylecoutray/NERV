# NERV

[![Website](https://img.shields.io/badge/Website-nervframework.com-blue)](https://nervframework.com)
[![Paper](https://img.shields.io/badge/Paper-Journal%20of%20Neuroscience%20Methods-green)](https://www.sciencedirect.com/science/article/pii/S0165027025002912)
[![DOI](https://img.shields.io/badge/DOI-10.1016%2Fj.jneumeth.2025.110647-blue)](https://doi.org/10.1016/j.jneumeth.2025.110647)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Built%20with-Unity-black)](https://unity.com/)

# Neuroscience Experimental Runtime by Vanderbilt

**NERV** is a Unity-based C# framework for rapid, reproducible, and hardware-synchronized neuroscience experiment design and execution.

NERV combines no-code experiment generation, transparent C# task control, millisecond TTL synchronization, multi-display experimenter interfaces, modular hardware extensions, and automatic archival of code, configuration files, logs, screenshots, and session metadata.

Website: [NERVFRAMEWORK.COM](https://nervframework.com)  
Paper: [Journal of Neuroscience Methods](https://www.sciencedirect.com/science/article/pii/S0165027025002912)  
DOI: [10.1016/j.jneumeth.2025.110647](https://doi.org/10.1016/j.jneumeth.2025.110647)

---

## Why NERV?

Behavioral neuroscience experiments often require precise stimulus timing, hardware triggering, neural data alignment, experimenter monitoring, and reproducible logging. These pieces are usually split across separate tools, scripts, devices, and manual workflows.

NERV centralizes this workflow inside Unity.

It is designed around a **low floor, high ceiling** philosophy:

- **Low floor:** Generate runnable neuroscience tasks through Unity Editor tools without writing code.
- **High ceiling:** Customize the generated C# `TrialManager` script, add modular components, and integrate custom hardware.

NERV supports 2D behavioral tasks, 3D and gamified experiments, human studies, non-human primate experiments, eye tracking, TTL synchronization, and neural recording workflows.

---


## Example Tasks

NERV includes example experiments that demonstrate working memory, sequence manipulation, rule-based behavior, and 3D navigation workflows.

| Acronym | Task | Description |
|---|---|---|
| DMS | Distractor Match-to-Sample | Delayed matching with optional distractors |
| MNM | Match/Non-Match | Two-stimulus comparison task |
| RAM3D | Rule-Adaptive Match 3D | 3D navigation with context-dependent match or non-match rules |
| RSM | Rule-Based Sequence Manipulation | Rule-based transformation of maintained stimulus sequences |
| SDMS | Sequence Distractor Match-to-Sample | Sequence matching with distractors during the delay period |

A live web demonstration is available at [nervframework.com](https://nervframework.com).

---

## Demo Videos

### Astronaut Scholarship Foundation Technical Conference Presentation

[![Watch here](https://img.youtube.com/vi/UzLb2_vhc9c/maxresdefault.jpg)](https://youtu.be/UzLb2_vhc9c?si=NlPGW5geVD_Sqgtc)

<table>
  <tr>
    <td align="center" width="50%">
      <h3>Making a Game, DMS Example</h3>
      <a href="https://youtu.be/QPY7fMwiKoE">
        <img src="https://img.youtube.com/vi/QPY7fMwiKoE/0.jpg" alt="Making a Game, DMS Example" width="100%">
      </a>
    </td>
    <td align="center" width="50%">
      <h3>Experimenter Screen Demo</h3>
      <a href="https://youtu.be/1jZmXWTGsBs">
        <img src="https://img.youtube.com/vi/1jZmXWTGsBs/0.jpg" alt="Experimenter Screen Demo" width="100%">
      </a>
    </td>
  </tr>
</table>

<p align="center">
  <em>Click the thumbnails above to watch the demo videos.</em>
</p>

---


## Architecture

NERV has two main layers:

1. **Editor Tools**
   - Define experiments.
   - Generate stimulus mappings.
   - Generate trial definitions.
   - Generate task scenes and `TrialManager` scripts.

2. **Runtime Framework**
   - Executes trial state machines.
   - Presents stimuli.
   - Handles input and feedback.
   - Sends TTL events.
   - Logs behavioral and hardware events.
   - Archives complete session data.

```text
ExperimentDefinition
        |
        v
Stim Index Mapping
        |
        v
Trial Definition Generator
        |
        v
Task Generator
        |
        v
Generated Scene + TrialManager{Acronym}.cs
        |
        v
Runtime Execution + Logging + TTL + Archival
```

Core runtime components include:

- `TrialManager{Acronym}.cs`
- `GenericConfigManager.cs`
- `SessionLogManager.cs`
- `DisplayManager.cs`
- `StimulusSpawner`
- `BlockPauseController.cs`
- `CoinController.cs`
- `DependenciesManager.cs`

Optional ExtraFunctions include:

- `FixationDotSpawner.cs`
- `PhotodiodeMarker.cs`
- `DwellClick.cs`
- `GazeCursorController.cs`
- `RewardPump.cs`

---

## Quick Start

### Clone the repository

```bash
git clone https://github.com/kylecoutray/NERV.git
cd NERV
```

### Open in Unity

1. Open Unity Hub.
2. Select **Add project from disk**.
3. Choose the Unity project folder.
4. Open the `TaskSelector` scene or one of the example task scenes.
5. Press Play to test inside the Unity Editor.

### Run an existing task

1. Enter a session name.
2. Select a task.
3. Configure display mode, COM port, and test mode if needed.
4. Press Play or build the project.
5. Run the task.
6. Inspect output files in `MASTER_LOGS`.

### Create a new task

1. Create an `ExperimentDefinition`.
2. Define the task states and state types.
3. Generate the stimulus index CSV.
4. Generate the trial definition CSV.
5. Use the Task Generator to create the scene and TrialManager.
6. Run the task.
7. Optionally customize the generated C# script.

---

## Data Output

NERV automatically archives each session in `MASTER_LOGS`.

```text
MASTER_LOGS/
  _MASTER_SUMMARY.csv
  Session_YYYYMMDD_HHMMSS/
    MANIFEST.json
    {ACRONYM}_YYYYMMDD_HHMMSS/
      ALL_LOGS.csv
      TTL_LOGS.csv
      LOGS_HEADER.txt
      SUMMARY.csv
      TrialManager{Acronym}_CODE.txt
      Experimenter_Comments.txt
      {Acronym}_Trial_Def.csv
      {Acronym}_Stim_Index.csv
      StatesCaptured/
```

Important outputs:

| File | Purpose |
|---|---|
| `MANIFEST.json` | Session metadata, system fingerprint, Git commit, and file checksums |
| `ALL_LOGS.csv` | Chronological behavioral event log |
| `TTL_LOGS.csv` | Hardware pulse event log |
| `SUMMARY.csv` | Task-level and trial-level summary |
| `TrialManager{Acronym}_CODE.txt` | Exact archived C# code used for the run |
| `StatesCaptured/` | Screenshots for visual verification |

---

## Hardware Support

NERV supports:

- Arduino-based USB-to-TTL output
- Byte-coded TTL event channels
- Photodiode timing validation
- NI-DAQ based eye-tracking input
- Gaze cursor visualization
- Dwell-click selection
- Reward pump control
- Integration with neural acquisition workflows such as Open Ephys, Neuropixels, and SEEG-compatible pipelines

For hardware-free development, Test Mode disables serial communication while still logging intended TTL events.

---

## Dataset and Supplemental Resources

The article lists the project dataset repository here:

[NERV_PD_TEST](https://github.com/kylecoutray/NERV_PD_TEST)

Additional supplemental resources are available here:

[Google Drive Supplemental Materials](https://drive.google.com/drive/folders/1bfnYk8ob1KO_ltoVwyi9-E69jBNYdmes?usp=sharing)

Supplemental materials include:

- NHP and human implementation test cases
- Executable builds
- Recorded data
- Full logs
- Arduino TTL firmware
- Photodiode timing test data
- Timing analysis scripts

---

## Citation

If you use NERV in your research, please cite:

```bibtex
@article{coutray2026nerv,
  title = {NERV: A comprehensive framework for rapid, reproducible, and hardware-synchronized neuroscience experiment design and execution},
  author = {Coutray, Kyle and Constantinidis, Christos},
  journal = {Journal of Neuroscience Methods},
  volume = {427},
  pages = {110647},
  year = {2026},
  issn = {0165-0270},
  doi = {10.1016/j.jneumeth.2025.110647},
  url = {https://www.sciencedirect.com/science/article/pii/S0165027025002912}
}
```

---

## License

NERV is released under the MIT License. See `LICENSE` for details.

---

## Acknowledgements

NERV was developed in the Department of Biomedical Engineering at Vanderbilt University.

This work was supported by the VUSE Summer Research Program and NIH grant R01 EY017077.

---

## Contact

For questions, issues, or contributions, please use the GitHub Issues tab or contact the repository maintainer.

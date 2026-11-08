# Tank Game — Engineering Guidelines

## Overview

This document defines the development conventions and architectural constraints for the Unity tank game project. The goal is to maintain a clean, modular, and understandable codebase without introducing unnecessary complexity.

## Technology and Architecture

- **Dependency injection:** Use **Zenject** to configure and manage dependencies.
- **Event communication:** Use **Zenject SignalBus** for event-driven communication between modules where appropriate.
- **Asynchronous operations:** Use **UniTask** for all asynchronous workflows. Do not introduce alternative asynchronous patterns without prior approval.

## Design Principles

Follow these principles consistently:

- **SOLID:** Keep responsibilities focused, depend on abstractions where beneficial, and design components for extension without unnecessary modification.
- **DRY (Don't Repeat Yourself):** Consolidate genuinely shared logic and avoid duplicated behavior.
- **KISS (Keep It Simple, Stupid):** Prefer straightforward, readable solutions over unnecessary abstraction or overengineering.

Apply these principles pragmatically: simplicity and maintainability take precedence over rigid adherence to patterns.

## Modularity and Coupling

- Design modules with **high cohesion and low coupling**.
- Minimize dependencies and direct interactions between modules.
- Keep public APIs and integration points small and explicit.
- Prefer **SignalBus** for cross-module notifications and events when it reduces coupling.
- Use direct calls through well-defined interfaces when an explicit request/response interaction is clearer. Do not use signals merely to avoid every direct method call.
- Keep implementation details internal to each module whenever possible.

### Assembly Definitions

- Each module in `Assets/Scripts` compiles into its own assembly (`Tanks.<Module>.asmdef`). `TankCombat` is part of `Tanks.TankSpawning` through `Tanks.TankCombat.asmref`.
- A script's folder determines its assembly. Place new scripts in the folder of the module that owns them, and keep each module's namespace inside its own folder.
- Declare dependencies between modules only through `references` in `.asmdef` files. **Do not add or remove a module reference, create, merge or delete `.asmdef`/`.asmref` files, or move scripts between module folders without explicit approval.**
- Module dependencies must remain acyclic. If a change appears to need a reference that would create a cycle, stop and propose alternatives, such as moving a contract to a lower-level module, inverting the dependency through an interface, or using a signal.
- Do not use `InternalsVisibleTo` or make types `public` merely to cross a module boundary. Propose a narrow public contract instead.
- Each module registers its internal services through a static `*Bindings` class inside that module (for example, `TankSpawningBindings.Install`, `TankInputBindings.InstallKeyboardMouseInput`). `GameInstaller` only composes modules and must not bind another module's internal types.

## Scope Control and Decision-Making

### Implement Only What Was Requested

- **Treat the user’s request as the complete scope of work, not an invitation to design additional features.** Implement only the requested behavior and the minimum supporting code required for it to function.
- **Do not add unrelated or speculative functionality**, even when it seems useful, conventional, or likely to be needed later.
- Do not independently introduce additional gameplay mechanics, camera systems, aiming points, HUD elements, visual effects, helper systems, or third-party dependencies unless they are explicitly requested or approved.
- Do not expand a module’s responsibilities to cover adjacent systems. For example, a request for **tank movement controls** does **not** authorize implementing a camera, an aim point, turret aiming, or weapon controls.
- Avoid opportunistic refactoring or changes outside the requested module unless the user approves them.
- **A suggestion is not an authorization.** Keep optional enhancements as proposals; do not implement them preemptively.

### Clarify Before Making Scope or Architecture Decisions

- If the implementation depends on **missing requirements, ambiguous behavior, integration choices, or changes outside the agreed scope**, **ask the user before proceeding with the affected work**.
- Present the decision concisely, preferably with **two or three concrete options**, their relevant trade-offs, and **one recommended option with a short rationale**.
- Clearly identify any proposed additions to the original scope, including new systems, dependencies, or scene/prefab modifications.
- **Wait for explicit confirmation** before implementing an unrequested feature or choosing an unresolved, consequential design direction. Do not interpret silence as approval.
- For minor, reversible implementation details that do not affect scope, architecture, or externally visible behavior, choose the simplest reasonable approach without unnecessary questions.
- When a blocked decision concerns only part of a task, proceed only with clearly independent, authorized work; do not invent a substitute requirement.

### Pre-Implementation Check

Before writing or modifying code, verify:

1. Is every planned behavior explicitly requested or strictly necessary for the requested behavior?
2. Does the solution introduce any additional system, responsibility, or dependency that needs approval?
3. Is any required decision unspecified or ambiguous enough to affect the result?
4. Will any scene or prefab need modification, or will any method exceed four parameters?
5. Does the change need a new assembly reference, a new or changed `.asmdef`/`.asmref`, or moving scripts between module folders?

If a decision requires approval, **ask first**. When delivering a change, briefly state what was implemented and separately list any **not implemented** suggestions, if relevant.

## Encapsulation and Module Boundaries

### Ownership and Public APIs

- Each module **owns its internal state and implementation details**. Other modules must not read or mutate that state through back doors.
- Expose the **smallest explicit API** that supports the module's responsibilities. Prefer focused interfaces when a dependency is necessary; do not expose implementation classes or collections merely for convenience.
- Do not expose mutable fields, mutable internal collections, or writable properties across module boundaries. Return read-only views or immutable snapshots only when needed.
- In Unity components, use private serialized fields for Inspector configuration rather than public fields, unless a public API is intentionally required.
- Avoid reaching into another module with `GetComponent`, hierarchy traversal, or global lookups to access its internal implementation. Resolve authorized dependencies through Zenject and well-defined contracts.
- Do not introduce abstraction layers, registries, or services solely to satisfy these rules. If extra infrastructure becomes necessary, propose it and request approval first.

### SignalBus Contracts

- Cross-module signals must contain **only the information required by their consumers**. Prefer immutable value data and small, purpose-specific payloads.
- **Do not pass `GameObject`, `Transform`, `MonoBehaviour`, `Component`, or other Unity object references through cross-module signals by default.** Such references leak implementation details and create hidden coupling.
- Signals must not expose mutable module state or grant recipients the ability to modify another module's internals.
- Signals announce events; they are not a substitute for every command, query, or method call. For an intentional request/response interaction, prefer a narrow interface.
- If a Unity object reference is genuinely required in a cross-module contract, **explain why and request explicit approval** rather than silently making an exception.
- **Do not introduce an entity ID system at this stage.** Strongly typed or globally unique entity identifiers are postponed until explicitly requested. Choose the simplest existing contract that respects module boundaries, and ask if that is insufficient.

### Unity References Within a Module

- Unity object references are allowed **inside the module that owns or operates on those objects**.
- Local dictionaries and caches may use Unity objects as keys when their use is private to the owning module and their lifetime is handled correctly.
- Do not expose such caches or their Unity object references as the integration API of another module.

## Evidence-Based Bug Fixing

### Diagnose Before Editing

- **Do not implement a fix based solely on an assumption about the root cause.** Separate observed facts from hypotheses explicitly.
- First establish the actual symptoms, expected behavior, affected path, and—where feasible—reproduction steps. Inspect relevant code, logs, stack traces, execution order, event subscriptions, object lifetimes, and existing tests as appropriate.
- Form a small set of plausible hypotheses and check them against available evidence. Use targeted logging, assertions, breakpoints, or narrowly scoped tests when necessary. Do not add permanent diagnostic infrastructure without approval.
- If evidence is insufficient to distinguish causes, **ask the user for the missing information or approval for a specific diagnostic step**. Offer two or three concrete investigation options when there is a meaningful choice, and recommend one. Do not guess and ship a speculative rewrite.

### Fix the Confirmed Cause, Not the Symptoms

- Once the cause is supported by evidence, apply the **smallest localized change** that addresses it.
- Do not add large state machines, fallback systems, synchronization layers, retries, extra services, or generalized error-handling frameworks to fix a local bug unless their necessity has been demonstrated and approved.
- Do not mask failures with silent catches, arbitrary delays, duplicate handlers, or defensive branches that conceal the underlying defect.
- Avoid unrelated cleanup or refactoring during bug fixes. If a broader change is genuinely necessary, describe the evidence, proposed scope, and alternatives, then request explicit permission **before** implementing it.

### Verify and Report

- Recheck the original reproduction scenario and relevant edge cases after the change. Run focused tests or available validation steps where possible.
- State **what was observed, what root cause was established, what changed, and how the result was verified**.
- If the cause remains unconfirmed, say so explicitly. Do not present a hypothesis as a diagnosis or an untested patch as a verified fix.
- If the first fix does not resolve the issue, revisit the diagnosis instead of layering additional speculative logic onto the failed patch.

## Configuration and Balancing

### Configuration Ownership and Storage

- Store gameplay-tunable parameters and balancing coefficients in appropriate `ScriptableObject` configuration assets.
- Avoid hardcoded gameplay values and magic numbers.
- Always inspect and reuse suitable existing configurations first.
- Group related parameters by responsibility.
- Keep runtime state separate from configuration data.
- Treat configuration assets as read-only during gameplay unless explicitly instructed otherwise.
- Inject configuration dependencies through Zenject where appropriate.
- Component references and strictly local settings may remain private `[SerializeField]` fields.
- Mathematical constants and non-tunable implementation details do not require configuration assets.

### Approval Required for Configuration Changes

- **Never create new `ScriptableObject` configuration classes or assets without explicit user approval.**
- Do not independently extend, restructure, or repurpose existing configuration schemas.
- If an appropriate existing configuration is available, reuse it.
- If no suitable configuration exists, stop and propose 2–3 possible approaches.
- Explain the trade-offs and recommend the most appropriate solution.
- Wait for explicit approval before implementing the selected approach.
- Never bypass these restrictions by hardcoding tunable values or placing unrelated parameters into an unsuitable configuration.

## Implementation Constraints

### Method Parameters

- A method should have **no more than four parameters**.
- If a method genuinely requires **five or more parameters**, request explicit approval **before implementing it**.
- Prefer improving the API design where possible; do not introduce artificial parameter containers solely to bypass this constraint.

### Scene and Prefab Changes

- **Do not directly modify Unity scenes or prefabs without explicit prior approval.**
- If a task requires such changes, describe the intended modifications and request permission before proceeding.
- Approval for one change does not imply blanket approval for unrelated changes.

## Development Expectations

- Favor clear names, focused responsibilities, and code that can be understood without extensive explanation.
- Make the smallest coherent change that satisfies the requirement.
- Avoid unnecessary dependencies, speculative abstractions, and unrelated refactoring.
- For bug fixes, prioritize investigation and verification over code volume; do not use a rewrite as a substitute for diagnosis.
- Preserve established architecture and conventions unless a change has been discussed and approved.
- When a requirement conflicts with these guidelines, raise the conflict before proceeding rather than silently making an exception.

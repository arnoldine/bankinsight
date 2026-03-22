# BankingOS

BankingOS is the new product workspace being carved out from the existing BankInsight repository. This folder is the starting point for the platform identity, metadata contracts, and seed packs needed to evolve BankInsight into a configurable core banking platform.

## Purpose
- establish the BankingOS product identity
- define backend and frontend metadata contracts
- seed prebuilt banking workflows, forms, and themes
- provide a clean handoff point for incremental implementation

## Initial structure
- `docs/`
  - implementation prompt and execution guidance
- `api/Contracts/`
  - backend contract models for forms, themes, queues, and process runtime
- `web/src/platform/`
  - frontend metadata contracts for process/task rendering
- `seed/processes/`
  - seeded workflow and process definitions
- `seed/forms/`
  - seeded low-code form definitions
- `seed/themes/`
  - seeded theme definitions

## Relationship to the current repo
- Existing runtime code remains in `BankInsight.API` and `src`.
- This BankingOS workspace is a landing zone for the new platform abstraction and product identity.
- Implementation should reuse current workflow, approval, audit, lending, account, treasury, branch, and form foundations where sensible.

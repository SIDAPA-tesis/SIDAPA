---
tags: [skill-creator]
max_turns: 60
timeout_seconds: 1800
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash, PowerShell]
---

Quiero pruebas de ReferenciaRepository contra la base de datos de verdad (PostgreSQL), no con mocks: que NextNumberAsync nunca repita números, que ChangeStatusAsync no cambie nada si la referencia ya no está en el estatus de origen y que deje registro en la bitácora cuando sí cambia, y que DeleteAsync borre también la bitácora y los documentos.

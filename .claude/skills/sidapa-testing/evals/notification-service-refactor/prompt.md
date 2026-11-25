---
tags: [skill-creator]
max_turns: 60
timeout_seconds: 1800
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash, PowerShell]
---

NotificationService usa DateTime.UtcNow directo y no tiene ni una prueba. Refactorízalo para que se pueda probar y escríbele pruebas unitarias decentes; me preocupa sobre todo qué pasa si falla el correo y lo de avisar a todos los usuarios de un cliente.

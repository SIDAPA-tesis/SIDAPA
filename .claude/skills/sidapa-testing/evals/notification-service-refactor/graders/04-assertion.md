---
type: llm
focus: trace
weight: 1
---

Hay una prueba donde el correo falla (SendAsync regresa false o lanza) que verifica que el aviso sí se guardó y que NO se marcó como enviado (MarkEmailSentAsync no se llama)

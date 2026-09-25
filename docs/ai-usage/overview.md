# Agentic AI usage

FixFlow uses a .NET orchestration layer in `FixFlow.Application.Agents`.

- `ILlmModelAdapter` returns structured JSON from a model.
- `IToolCaller` executes named tools.
- `IApprovalGate` blocks side effects until a person approves them.
- `POST /api/agents/run` is JWT-protected and returns JSON the clients can render.

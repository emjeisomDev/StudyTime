# StudyTime API

API RESTful para gerenciamento de estudos semanais, construída com .NET 10,
Clean Architecture, DDD, EF Core e PostgreSQL.

## Pré-requisitos

- .NET SDK 10.0.401 ou compatível com global.json.
- Git.
- PostgreSQL (necessário a partir das fases de persistência).

## Estrutura

```text
src/
├── StudyTime.Api
├── StudyTime.Application
├── StudyTime.Domain
└── StudyTime.Infrastructure

tests/
├── Domain.Tests
├── Application.Tests
├── Infrastructure.Tests
└── Api.Tests
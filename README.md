# StudyTime API

API REST para controle de tempo de estudo, desenvolvida com .NET 10 e PostgreSQL 17.

## Stack

- .NET SDK 10.0.401
- ASP.NET Core
- PostgreSQL 17
- Entity Framework Core 10
- Npgsql
- MediatR
- FluentValidation
- AutoMapper
- xUnit
- FluentAssertions
- Testcontainers

## Estrutura

```text
StudyTime/
├── StudyTime.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── docker-compose.yml
├── src/
│   ├── StudyTime.Domain/
│   ├── StudyTime.Application/
│   ├── StudyTime.Infrastructure/
│   └── StudyTime.Api/
└── tests/
    ├── StudyTime.Domain.Tests/
    ├── StudyTime.Application.Tests/
    ├── StudyTime.Infrastructure.Tests/
    └── StudyTime.Api.Tests/
```

## Pré-requisitos

- Windows com CMD
- .NET SDK 10.0.401
- Docker Desktop
- Git

## Validar SDK

```cmd
dotnet --version
```

Resultado esperado:

```text
10.0.401
```

## Restaurar depend�ncias

```cmd
dotnet restore StudyTime.slnx
```

## Compilar a solu��o

```cmd
dotnet build StudyTime.slnx --configuration Release --no-restore
```

## Subir infraestrutura local

```cmd
docker compose up -d
```

Verificar containers:

```cmd
docker compose ps
```

## PostgreSQL

Host: localhost
Porta: 5432
Banco: studytime
Usuário: studytime
Senha: studytime_dev

Connection string sugerida:

```text
Host=localhost;Port=5432;Database=studytime;Username=studytime;Password=studytime_dev
```

## pgAdmin

URL: http://localhost:5050
Email: admin@studytime.local
Senha: admin_dev

## Health check PostgreSQL

```cmd
docker compose exec postgres pg_isready -U studytime -d studytime
```

Resultado esperado:

```text
/var/run/postgresql:5432 - accepting connections
```

## Encerrar infraestrutura

```cmd
docker compose down
```

## Licença

Projeto de estudo e desenvolvimento da API StudyTime.

# Полный список архитектурных паттернов TaskWorkflow

Этот документ содержит структурированный список всех паттернов, реализованных в проекте TaskWorkflow.
Предназначен для последовательного изучения и написания серии эссе по каждому паттерну.

---

## 📋 Сводная таблица паттернов (25 паттернов)

| # | Категория | Паттерн | Файл/Пример | Сложность |
|---|-----------|---------|-------------|-----------|
| 1 | Architecture | Clean Architecture | Solution structure | ⭐⭐⭐ |
| 2 | Architecture | CQRS | Commands/Queries separation | ⭐⭐ |
| 3 | Architecture | Mediator Pattern | MediatR | ⭐⭐ |
| 4 | Architecture | Bounded Context | Identity BC + TaskWorkflow BC | ⭐⭐⭐ |
| 5 | DDD | Aggregate Root | `Project.cs` | ⭐⭐⭐ |
| 6 | DDD | Entity | `TaskItem.cs` | ⭐⭐ |
| 7 | DDD | Value Object | `Email.cs`, `TaskState.cs` | ⭐⭐ |
| 8 | DDD | Domain Events | `TaskCompletedEvent.cs` | ⭐⭐⭐ |
| 9 | DDD | Rich Domain Model | Business logic in entities | ⭐⭐ |
| 10 | DDD | State Machine | TaskItem state transitions | ⭐⭐ |
| 11 | Application | Pipeline Behaviors | `ValidationBehavior.cs` | ⭐⭐ |
| 12 | Application | Result Pattern | `Result<T>.cs` | ⭐⭐ |
| 13 | Application | Unit of Work | `UnitOfWork.cs` | ⭐⭐⭐ |
| 14 | Security | JWT Authentication | `JwtAccessTokenService.cs` | ⭐⭐ |
| 15 | Security | Refresh Token Rotation | `RefreshTokenHandler.cs` | ⭐⭐⭐ |
| 16 | Security | Hash Storage | SHA256 for tokens | ⭐ |
| 17 | Security | Cookie Authentication | Blazor Server auth | ⭐⭐ |
| 18 | API | API Versioning | `/api/v1/...` | ⭐ |
| 19 | API | RFC 7807 ProblemDetails | `TaskWorkflowProblemDetailsFactory.cs` | ⭐⭐ |
| 20 | API | Error Normalization | `ErrorNormalizer.cs` | ⭐⭐ |
| 21 | Resilience | Retry with Exponential Backoff | Polly policies | ⭐⭐ |
| 22 | Resilience | Circuit Breaker | Polly policies | ⭐⭐ |
| 23 | Infrastructure | Repository Pattern | `ProjectRepository.cs` | ⭐⭐ |
| 24 | Testing | Testcontainers | Integration tests | ⭐⭐ |
| 25 | Testing | Database Cleanup (Respawn) | `DatabaseInitializer.cs` | ⭐ |

---

## 🏗️ 1. Architecture Patterns (4 паттерна)

### 1.1 Clean Architecture
**Суть:** Зависимости направлены внутрь. Domain не знает о Infrastructure.

**Где реализовано:**
```
TaskWorkflow.Core         → Domain Layer (нет внешних зависимостей)
TaskWorkflow.Application  → Application Layer (use cases)
TaskWorkflow.Infrastructure → Infrastructure Layer (EF Core, external services)
TaskWorkflow.Api          → Presentation Layer
```

**Ключевые файлы:**
- Solution structure
- `DependencyInjection.cs` в каждом проекте

---

### 1.2 CQRS (Command Query Responsibility Segregation)
**Суть:** Разделение операций чтения и записи.

**Где реализовано:**
- `TaskWorkflow.Application/UseCases/*/Commands/` — команды
- `TaskWorkflow.Application/UseCases/*/Queries/` — запросы

**Ключевые файлы:**
- `CreateProjectCommand.cs`
- `GetAllProjectsQuery.cs`
- `ICommand.cs`, `IQuery.cs`

---

### 1.3 Mediator Pattern
**Суть:** Объекты общаются через посредника, не напрямую.

**Где реализовано:**
- Все handlers вызываются через `IMediator`
- `_mediator.Send(new CreateProjectCommand(...))`

**Ключевые файлы:**
- `DependencyInjection.cs` — регистрация MediatR
- Все `*Handler.cs` файлы

---

### 1.4 Bounded Context
**Суть:** Изолированные контексты с собственными моделями и правилами.

**Где реализовано:**
- `TaskWorkflow.*` — бизнес-логика (проекты, задачи)
- `TaskWorkflow.Identity.*` — аутентификация

**Ключевые файлы:**
- `IdentityDbContext.cs` (identity schema)
- `TaskWorkflowDbContext.cs` (public schema)

---

## 🎯 2. DDD Patterns (6 паттернов)

### 2.1 Aggregate Root
**Суть:** Единая точка входа для модификации группы связанных объектов.

**Где реализовано:**
- `Project` — агрегат для `TaskItem`
- `IdentityUser` — агрегат для `RefreshSession`

**Ключевые файлы:**
- `TaskWorkflow.Core/Entities/Project.cs`
- `TaskWorkflow.Core/Base/AggregateRoot.cs`

---

### 2.2 Entity
**Суть:** Объект с идентичностью (Id).

**Где реализовано:**
- `Project`, `TaskItem`, `User`

**Ключевые файлы:**
- `TaskWorkflow.Core/Base/Entity.cs`
- `TaskWorkflow.Core/Entities/*.cs`

---

### 2.3 Value Object
**Суть:** Объект без идентичности, сравнивается по значению, immutable.

**Где реализовано:**
- `Email` — самовалидирующийся email
- `TaskState` — состояние задачи
- `PasswordHash` — хеш пароля

**Ключевые файлы:**
- `TaskWorkflow.Core/ValueObjects/Email.cs`
- `TaskWorkflow.Core/Base/ValueObject.cs`

---

### 2.4 Domain Events
**Суть:** События, которые происходят в домене и обрабатываются позже.

**Где реализовано:**
- `TaskCreatedEvent`, `TaskCompletedEvent`, `TaskStartedEvent`
- Публикуются после `SaveChangesAsync()`

**Ключевые файлы:**
- `TaskWorkflow.Core/Events/*.cs`
- `TaskWorkflow.Infrastructure/EventHandlers/*.cs`

---

### 2.5 Rich Domain Model
**Суть:** Бизнес-логика находится в сущностях, а не в сервисах.

**Где реализовано:**
- `TaskItem.Start()`, `TaskItem.Complete()`
- `Project.AddTask()`, `Project.RemoveTask()`

**Ключевые файлы:**
- `TaskWorkflow.Core/Entities/TaskItem.cs`
- `TaskWorkflow.Core/Entities/Project.cs`

---

### 2.6 State Machine
**Суть:** Контролируемые переходы между состояниями.

**Где реализовано:**
- `TaskItem`: New → InProgress → Done
- Невалидные переходы выбрасывают исключение

**Ключевые файлы:**
- `TaskWorkflow.Core/Entities/TaskItem.cs`
- `TaskWorkflow.Core/ValueObjects/TaskState.cs`

---

## ⚙️ 3. Application Patterns (3 паттерна)

### 3.1 Pipeline Behaviors
**Суть:** Cross-cutting concerns через цепочку обработчиков.

**Где реализовано:**
```
Request → ValidationBehavior → LoggingBehavior → Handler → UnitOfWorkBehavior → Response
```

**Ключевые файлы:**
- `TaskWorkflow.Application/Behaviors/ValidationBehavior.cs`
- `TaskWorkflow.Application/Behaviors/LoggingBehavior.cs`
- `TaskWorkflow.Application/Behaviors/UnitOfWorkBehavior.cs`

---

### 3.2 Result Pattern
**Суть:** Явная обработка успеха/ошибки без исключений.

**Где реализовано:**
- `Result<T>` — обёртка с `IsSuccess`, `Value`, `Error`
- Все handlers возвращают `Result<T>`

**Ключевые файлы:**
- `TaskWorkflow.Application/Common/Result.cs`
- `TaskWorkflow.Application/Common/Error.cs`

---

### 3.3 Unit of Work
**Суть:** Атомарное сохранение + публикация domain events.

**Где реализовано:**
- `UnitOfWork` собирает events перед save
- После save публикует events через MediatR

**Ключевые файлы:**
- `TaskWorkflow.Infrastructure/UnitOfWork/UnitOfWork.cs`

---

## 🔐 4. Security Patterns (4 паттерна)

### 4.1 JWT Authentication
**Суть:** Stateless аутентификация через подписанные токены.

**Где реализовано:**
- REST API, gRPC — валидация JWT
- Identity BC — выдача JWT

**Ключевые файлы:**
- `TaskWorkflow.Identity.Infrastructure/Auth/JwtAccessTokenService.cs`
- `TaskWorkflow.Identity.Infrastructure/Auth/JwtSettings.cs`

---

### 4.2 Refresh Token Rotation
**Суть:** Каждый refresh создаёт новый refresh token.

**Где реализовано:**
- `RefreshTokenHandler` — старый token revoked, новый создан

**Ключевые файлы:**
- `TaskWorkflow.Identity.Application/Handlers/RefreshTokenHandler.cs`

---

### 4.3 Hash Storage
**Суть:** Токены хранятся как SHA256 hash, не plaintext.

**Где реализовано:**
- `RefreshTokenService.Hash()` — SHA256

**Ключевые файлы:**
- `TaskWorkflow.Identity.Infrastructure/Auth/RefreshTokenService.cs`

---

### 4.4 Cookie Authentication
**Суть:** Server-side сессия для in-process приложений.

**Где реализовано:**
- BlazorServer, Identity.Admin — Cookie auth
- Нет JWT overhead для server-side apps

**Ключевые файлы:**
- `TaskWorkflow.BlazorServer/Program.cs`
- `TaskWorkflow.BlazorServer/Auth/BlazorAuthStateProvider.cs`

---

## 🌐 5. API Patterns (3 паттерна)

### 5.1 API Versioning
**Суть:** Версионирование API для backward compatibility.

**Где реализовано:**
- URL: `/api/v1/projects`
- Header: `X-Api-Version`

**Ключевые файлы:**
- `TaskWorkflow.Api/Program.cs` — AddApiVersioning
- Controllers с `[ApiVersion("1.0")]`

---

### 5.2 RFC 7807 ProblemDetails
**Суть:** Стандартный формат ошибок.

**Где реализовано:**
- Все ошибки возвращают ProblemDetails JSON

**Ключевые файлы:**
- `TaskWorkflow.Api/Errors/TaskWorkflowProblemDetailsFactory.cs`
- `TaskWorkflow.Api/Errors/ProblemDetailsMapper.cs`

---

### 5.3 Error Normalization
**Суть:** REST и gRPC ошибки → единая UI модель.

**Где реализовано:**
- WPF Client нормализует ProblemDetails и RpcException

**Ключевые файлы:**
- `TaskWorkflow.WpfClient/Services/Common/ErrorNormalizer.cs`
- `TaskWorkflow.WpfClient/Services/Common/ApiError.cs`

---

## 💪 6. Resilience Patterns (2 паттерна)

### 6.1 Retry with Exponential Backoff
**Суть:** Повторные попытки с увеличивающейся задержкой.

**Где реализовано:**
- WPF Client HTTP calls
- Polly policies

**Ключевые файлы:**
- `TaskWorkflow.WpfClient/Infrastructure/Resilience/RetryPolicies.cs`

---

### 6.2 Circuit Breaker
**Суть:** Прекращение вызовов к неработающему сервису.

**Где реализовано:**
- WPF Client HTTP/gRPC calls
- 5 failures → 30 sec break

**Ключевые файлы:**
- `TaskWorkflow.WpfClient/Infrastructure/Resilience/CircuitBreakerPolicies.cs`

---

## 🏭 7. Infrastructure Patterns (1 паттерн)

### 7.1 Repository Pattern
**Суть:** Абстракция доступа к данным.

**Где реализовано:**
- `IProjectRepository`, `IUserRepository`
- Реализации в Infrastructure

**Ключевые файлы:**
- `TaskWorkflow.Core/Interfaces/IProjectRepository.cs`
- `TaskWorkflow.Infrastructure/Repositories/ProjectRepository.cs`

---

## 🧪 8. Testing Patterns (2 паттерна)

### 8.1 Testcontainers
**Суть:** Реальная БД в Docker для интеграционных тестов.

**Где реализовано:**
- PostgreSQL container для тестов
- Реальные миграции, не InMemory

**Ключевые файлы:**
- `TaskWorkflow.IntegrationTests/Infrastructure/PostgreSqlContainerFixture.cs`

---

### 8.2 Database Cleanup (Respawn)
**Суть:** Быстрая очистка БД между тестами.

**Где реализовано:**
- Respawn вместо пересоздания БД

**Ключевые файлы:**
- `TaskWorkflow.IntegrationTests/Infrastructure/DatabaseInitializer.cs`

---

## 📝 Рекомендуемый порядок изучения

### Уровень 1: Основы (начните здесь)
1. Entity
2. Value Object
3. Repository Pattern
4. Result Pattern

### Уровень 2: Архитектура
5. Clean Architecture
6. CQRS
7. Mediator Pattern
8. Unit of Work

### Уровень 3: DDD
9. Aggregate Root
10. Domain Events
11. Rich Domain Model
12. State Machine

### Уровень 4: Безопасность
13. JWT Authentication
14. Cookie Authentication
15. Refresh Token Rotation
16. Hash Storage

### Уровень 5: Production-ready
17. Pipeline Behaviors
18. API Versioning
19. RFC 7807 ProblemDetails
20. Error Normalization
21. Bounded Context

### Уровень 6: Resilience & Testing
22. Retry with Exponential Backoff
23. Circuit Breaker
24. Testcontainers
25. Database Cleanup (Respawn)

---

## 📚 Связанные документы

- [PATTERNS.md](PATTERNS.md) — краткий справочник паттернов
- [AUTHENTICATION.md](AUTHENTICATION.md) — архитектура аутентификации
- [ADR-009-SingleAuthSource.md](ADR-009-SingleAuthSource.md) — Bounded Contexts

---

*Создано для систематического изучения архитектурных паттернов*
*Последнее обновление: Декабрь 2024*

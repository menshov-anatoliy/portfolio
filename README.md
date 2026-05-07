<div align="center">

# 🚀 Портфолио кодовой базы

### Витрина инженерных решений промышленной складской платформы

*Подборка ключевых модулей корпоративной системы — от слоя доступа к данным до бюро пропусков и складских терминалов.*

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-9.0-5C2D91?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![NHibernate](https://img.shields.io/badge/NHibernate-ORM-1F425F?style=for-the-badge)](https://nhibernate.info/)
[![Angular](https://img.shields.io/badge/Angular-SPA-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![Docker](https://img.shields.io/badge/Docker-Linux-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)

</div>

---

## 👋 О репозитории

Этот репозиторий — **публичная демо-витрина** избранных фрагментов промышленной мультимандантной складской платформы. Здесь собраны характерные участки кода и архитектурные решения, иллюстрирующие подход к проектированию: **Clean / Hexagonal Architecture**, строгое разделение слоёв, портов и адаптеров, изоляция бизнес-логики от инфраструктуры.

> ⚠️ Код представлен **в иллюстративных целях**: модули вырваны из боевого решения, не собираются как единое приложение и не содержат конфиденциальных данных. Цель — продемонстрировать стиль кода, паттерны и инженерные практики.

---

## 🗂️ Оглавление

| # | Модуль | Краткое описание |
|---|---|---|
| 1 | [🗄️ **DAL**](#1-️-dal--слой-доступа-к-данным) | Слой доступа к данным: NHibernate + SQL Server, Repository/Gateway, кэш-декораторы, мультимандантность |
| 2 | [🚀 **Deploy**](#2--deploy--деплой-бизнес-логики) | REST-сервис управляемого деплоя конфигураций по клиентским окружениям |
| 3 | [🔐 **IdentityServer**](#3--identityserver--аутентификация-и-сессии) | Адаптер `Microsoft.AspNetCore.Identity` под порты ядра: cookie + JWT |
| 4 | [🏢 **PassOffice**](#4--passoffice--бюро-пропусков) | Автоматизация полного жизненного цикла пропуска и контроля доступа на КПП |
| 5 | [📟 **Terminal**](#5--terminal--складские-терминалы) | Универсальный API + Angular-SPA + WinForms-клиент для складских операций |
| 6 | [🧱 **Архитектурные принципы**](#-архитектурные-принципы) | Сквозные подходы: Clean Architecture, DI, тестируемость |
| 7 | [🛠️ **Технологический стек**](#-технологический-стек) | Используемые языки, фреймворки и инструменты |

---

## 📦 Модули

### 1. 🗄️ [DAL — слой доступа к данным](./DAL/README.md)

Промышленный мультимандантный слой персистенции по принципам **гексагональной архитектуры**. Бизнес-логика общается с БД исключительно через абстракции, а конкретные реализации (NHibernate, SQL Server, in-memory кэш) подключаются как взаимозаменяемые адаптеры.

- 🏛️ Чистое разделение `Entities` / `SqlServer` / `Cache`
- 🔁 Паттерны **Repository + Gateway**
- 🏢 Мультимандантность: каждый клиент на собственном шарде SQL Server
- 🔐 `SESSION_CONTEXT` для аудита и Row-Level Security
- ♻️ Прозрачные **кэш-декораторы** с типобезопасными ключами и `Clone()`-изоляцией

📂 [`./DAL`](./DAL) · 📖 [Подробнее](./DAL/README.md)

---

### 2. 🚀 [Deploy — деплой бизнес-логики](./Deploy/README.md)

**Deploy.Api** — REST-сервис для управляемого развёртывания конфигураций (менеджеров) на целевые клиентские окружения, распределённые по разным БД, **из единой точки входа**.

- 📋 Получение списка активных мандантов
- 🧪 Предварительный прогон конфигураций (**Execute**) для предпросмотра
- 🚢 Атомарный **Deploy** результата на целевые БД
- 🛡️ Контекст безопасности и аудит через `IWarehouseSecurityContext`
- 🐳 ASP.NET Core 9, Docker (Linux), Swagger

📂 [`./Deploy`](./Deploy) · 📖 [Подробнее](./Deploy/README.md)

---

### 3. 🔐 [IdentityServer — аутентификация и сессии](./IdentityServer/README.md)

Инфраструктурный модуль (Integration Layer) — **адаптер** над `Microsoft.AspNetCore.Identity`, реализующий внутренние порты ядра (`ISimpleAuthenticationPort`). Полностью изолирует бизнес-логику от деталей механизмов авторизации.

- 🛡️ Динамическое переключение **Cookies ↔ JWT** по типу клиента
- 🔄 Управление жизненным циклом сессий, `RefreshSignIn`, lockout
- 🧩 Бесшовная синхронизация `Principal` ↔ `ISecurityContext`

📂 [`./IdentityServer`](./IdentityServer) · 📖 [Подробнее](./IdentityServer/README.md)

---

### 4. 🏢 [PassOffice — бюро пропусков](./PassOffice/README.md)

Комплексное решение для автоматизации **бюро пропусков** и контроля доступа на территорию складского комплекса. Покрывает полный жизненный цикл пропуска — от подачи заявки до автоматизированного выезда через КПП.

- 🎫 Разовые / временные / постоянные пропуска для водителей, пешеходов, гостей и арендаторов
- 🔗 Удалённое дозаполнение данных по ссылке
- 📷 Интеграция с камерами распознавания номеров и шлагбаумами
- ✅ Многоуровневое согласование заявок
- 🚦 Контроль убытия и реверсивное движение полос

📂 [`./PassOffice`](./PassOffice) · 📖 [Подробнее](./PassOffice/README.md)

---

### 5. 📟 [Terminal — складские терминалы](./Terminal/README.md)

Корпоративная подсистема управления складскими операциями: единый универсальный API + несколько клиентов под разные сценарии работы.

- 🌐 Универсальный REST-эндпоинт (`POST api/terminal/postResponse`)
- 📱 Web-клиент (**Angular SPA**) и Windows-клиент (**WinForms**)
- 🔐 Аутентификация через **IdentityServer** (cookie + bearer)
- 🧵 Цепочка middleware для сессий и Security-контекста
- 🚀 Интеграция с подсистемой деплоя и кэшированием

📂 [`./Terminal`](./Terminal) · 📖 [Подробнее](./Terminal/README.md)

---

## 🧱 Архитектурные принципы

Все модули следуют **общим инженерным принципам**, обеспечивающим долгосрочную поддерживаемость и развитие платформы:

- 🧭 **Clean / Hexagonal Architecture** — `API → Facade → Services → DAL`, бизнес-ядро не знает об инфраструктуре.
- 🔌 **Ports & Adapters** — интеграции (Identity, ORM, Cache) подключаются как заменяемые адаптеры через интерфейсы-порты.
- 💉 **Dependency Injection** — все компоненты регистрируются в контейнере, поощряется `Lazy<T>` для тяжёлых зависимостей.
- 🧪 **Тестируемость** — интерфейсная развязка позволяет покрывать слои модульными тестами без поднятия реальной инфраструктуры.
- 🏢 **Multi-tenant by design** — мультимандантность учитывается на всех уровнях: от шардирования БД до Security-контекста.
- 🛡️ **Security-first** — контекст пользователя прокидывается до уровня СУБД (`SESSION_CONTEXT`) для сквозного аудита.
- 🧬 **POCO без протечек** — доменные сущности не зависят от ORM и могут быть переиспользованы любым адаптером.

---

## 🛠️ Технологический стек

<div align="center">

| Категория | Технологии |
|---|---|
| **Язык / платформа** | C# 12, .NET 9 |
| **Web / API** | ASP.NET Core 9, Swagger / OpenAPI |
| **ORM** | NHibernate (боевой), EF Core (миграции) |
| **СУБД** | Microsoft SQL Server (`SESSION_CONTEXT`, TVP, UDT, хранимые процедуры) |
| **Identity** | `Microsoft.AspNetCore.Identity` (Cookies + JWT Bearer) |
| **Кэш** | `Microsoft.Extensions.Caching.Memory` + типобезопасные ключи |
| **Frontend** | Angular SPA, WinForms |
| **Инфраструктура** | Docker (Linux), CORS-политики |
| **Архитектура** | Clean / Hexagonal, DDD-influenced, Repository + Gateway, Decorator, Factory |

</div>

---

<div align="center">

### 💡 Этот репозиторий — приглашение к диалогу

*Если вас заинтересовал стиль кода или подход к проектированию — буду рад обсудить детали.*

**Автор:** Меньшов Анатолий · 2026

---

### 📬 Контакты для связи

[![Phone](https://img.shields.io/badge/Телефон-+7_(916)_487--23--89-25D366?style=for-the-badge&logo=whatsapp&logoColor=white)](tel:+79164872389)
[![Email](https://img.shields.io/badge/Email-menshov.anatoliy@gmail.com-EA4335?style=for-the-badge&logo=gmail&logoColor=white)](mailto:menshov.anatoliy@gmail.com)
[![Telegram](https://img.shields.io/badge/Telegram-@menshov__anatoliy-26A5E4?style=for-the-badge&logo=telegram&logoColor=white)](https://t.me/menshov_anatoliy)

| | |
|---|---|
| 📞 **Телефон** | [+7 (916) 487-23-89](tel:+79164872389) |
| ✉️ **Email** | [menshov.anatoliy@gmail.com](mailto:menshov.anatoliy@gmail.com) |
| 💬 **Telegram** | [@menshov_anatoliy](https://t.me/menshov_anatoliy) |

</div>


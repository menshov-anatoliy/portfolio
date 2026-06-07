**Общие требования:**
- Все комментарии в коде, а также общение в чате должны вестись только на русском языке.
- Для PowerShell, ты работаешь в операционной системе Windows.
- При генерации описания коммита, добавляй к сообщению номер задачи "refs #issue_number" (номер задачи бери из наименования текущей ветки Git).

**⚠️ ОБЯЗАТЕЛЬНОЕ ИСПОЛЬЗОВАНИЕ MCP-СЕРВЕРОВ:**
Перед написанием ЛЮБОГО кода или работой с UI/дизайном — агент **ОБЯЗАН** использовать MCP-серверы.
Полное описание серверов и правил их использования — в файле `AGENTS.md` (раздел «⚠️ ОБЯЗАТЕЛЬНЫЕ MCP-СЕРВЕРЫ»).
- **Любой код (Angular, .NET и др.)** → сначала `mcp_context7_resolve-library-id` + `mcp_context7_query-docs`.
- **Любой PrimeNG-компонент** → сначала `mcp_primeng_get_component_props` + `mcp_primeng_get_usage_example`.
- **Любое UI-изменение** → после реализации **Chrome DevTools MCP** (`mcp_chrome-devtoo_navigate_page` + `mcp_chrome-devtoo_take_screenshot` + `mcp_chrome-devtoo_list_console_messages`).
- **Любая правка дизайна** → только через `mcp_pencil_*` инструменты.
~~~~
**Скиллы (загружать при соответствующих задачах):**
- При генерации дизайна с использованием pencil-mcp — загрузи скилл `pass-office-frontend` (`.agents/skills/pass-office-frontend/SKILL.md`): стек, паттерны, компоненты, токены, чеклист.

**Требования к оформлению:**
- Соблюдай требования к оформлению кода из #prompt:'prompts/formatting.prompt'
- Соблюдай требования к оформлению юнит-тестов из #prompt:'prompts/unittests.prompt'
- При разработке Angular frontend соблюдай правила из #prompt:'prompts/angular-frontend.prompt'
- При любой работе с дизайном (создание/изменение экранов, компонентов, карточек, таблиц в design.pen) **обязательно** используй правила из #prompt:'prompts/design-pencil.prompt'

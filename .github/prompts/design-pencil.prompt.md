## Дизайн в Pencil — правила и практики

> **⚠️ Рекомендуется при любой работе с дизайном:**  
> Этот промпт применяется **всегда**, когда нужно создать, изменить или воссоздать UI-дизайн
> в файле `design.pen`, разработать новые экраны, компоненты или карточки форм
> для Angular-приложения `Molcom.PassOffice.Api`.

---

### Файл дизайна

Дизайн-файл: `Molcom.PassOffice.Api/ClientApp/design.pen`

Открывать через MCP Pencil:
```
mcp_pencil_open_document("D:\\Git\\molcom\\Molcom.Warehouse.Web\\Molcom.PassOffice.Api\\ClientApp\\design.pen")
```

---

### Рекомендуемый цикл работы с MCP

При задачах дизайна обычно полезно выполнять шаги в следующем порядке:

1. **`mcp_pencil_get_editor_state(include_schema: true)`** — узнать текущее состояние, список компонентов.
2. **`mcp_pencil_get_variables()`** — получить дизайн-токены (цвета, размеры).
3. **`mcp_pencil_batch_get()`** — изучить структуру нужных компонентов (readDepth: 3).
4. **`mcp_pencil_snapshot_layout()`** — проверить существующий layout.
5. **`mcp_pencil_find_empty_space_on_canvas()`** — найти свободное место для нового фрейма.
6. **`mcp_pencil_batch_design()`** — создать/изменить дизайн (≤ 25 операций на вызов).
7. **`mcp_pencil_get_screenshot()`** — визуальная проверка результата.
8. При несоответствии — итеративно корректировать до полного соответствия.

---

### Дизайн-токены (переменные)

Использовать только через `$` (например, `fill: "$molcom-blue"`):

| Переменная | Значение | Назначение |
|---|---|---|
| `$molcom-blue` | `#1B4FBF` | Основной бренд-цвет MOLCOM (кнопки, акценты) |
| `$dark-blue` | `#0A1F4E` | Тёмно-синий (заголовки, легенды fieldset) |
| `$surface-0` | `#FFFFFF` | Белый фон |
| `$surface-50` | `#F8FAFC` | Фон страницы |
| `$surface-100` | `#F1F5F9` | Фон шапок таблиц |
| `$surface-200` | `$border-color` | Граница / разделители |
| `$surface-400` | `#94A3B8` | Placeholder-текст, иконки |
| `$surface-500` | `#64748B` | Вторичный текст |
| `$surface-600` | `#475569` | Текст вторичного уровня |
| `$surface-700` | `#334155` | Метки полей |
| `$surface-900` | `#0F172A` | Основной текст |
| `$text-primary` | `#0F172A` | Основной текст |
| `$text-secondary` | `#475569` | Вторичный текст |
| `$text-muted` | `#64748B` | Приглушённый текст |
| `$border-color` | `#E2E8F0` | Цвет границ |
| `$success` | `#22C55E` | Успех |
| `$danger` | `#EF4444` | Ошибка |
| `$warning` | `#F59E0B` | Предупреждение |
| `$info` | `#3B82F6` | Информация |
| `$sidebar-width` | `220` | Ширина сайдбара |
| `$topbar-height` | `56` | Высота топбара |

---

### Типографика

Шрифты:
- **Inter** — основной интерфейсный шрифт (`fontFamily: "Inter"`)
- **Archivo Black** — заголовки `h1`, легенды `p-fieldset`, заголовки диалогов (`fontFamily: "Archivo Black"`)

Размеры текста (стандартные):
| Назначение | fontSize | fontWeight |
|---|---|---|
| Основной текст | 14 | normal |
| Метка поля | 13 | "500" |
| Мелкий / caption | 12 | normal |
| Заголовок секции | 14 | "600" |
| H1 страницы | 24–28 | "700" |

---

### Дизайн-система: компоненты в design.pen

Переиспользуемые компоненты (15 штук). **Всегда использовать через `ref`**, не создавать заново.

#### Поля ввода

| ID компонента | Имя | Назначение | Ширина по умолчанию |
|---|---|---|---|
| `lkIoV` | StringInput | Текстовое поле | 240 |
| `7IxY0` | DropdownInput | Выпадающий список | 240 |
| `g12M9` | DateInput | Поле даты | 240 |
| `lOrvS` | ClassifierInput | Справочник с поиском | 240 |

Все поля имеют структуру `label + input-wrap`, высота `38px`, скругление `6px`.

Кастомизация (через `descendants`):
```javascript
// ✅ Изменить метку через U() — одиночный путь (прямой потомок экземпляра)
U(field + "/G8DLu", { content: "Новая метка" })

// ✅ Изменить метку и placeholder через descendants при вставке I()
// (path "i0RBR/xM0XR" работает как двухуровневый внутри descendants)
field = I(parent, {
  type: "ref",
  ref: "lkIoV",
  width: "fill_container",
  descendants: {
    "G8DLu": { content: "Фамилия" },
    "i0RBR/xM0XR": { content: "Введите фамилию..." }
  }
})

// ❌ НЕ использовать U() с двухуровневым путём ПОСЛЕ вставки
// U(field + "/i0RBR/xM0XR", { content: "..." })  — не работает через U()
```

> **Правило:** Многоуровневые пути работают только в `descendants` при `I()` / `C()`.
> После вставки через `U()` доступны только пути одного уровня вложенности.

#### Кнопки

| ID | Имя | Вид | Цвет |
|---|---|---|---|
| `XWXQf` | ButtonPrimary | Заливка | `$molcom-blue`, текст белый |
| `g5sAO` | ButtonOutline | Контурная | контур `$molcom-blue` |
| `GOU8W` | ButtonIcon | Иконочная 32×32 | прозрачный |

Кастомизация кнопок:
```javascript
// Изменить иконку и подпись ButtonPrimary
U(btn + "/B1xpv", { iconFontName: "save" })
U(btn + "/PIMr7", { content: "Сохранить" })
```

#### Контейнеры

| ID | Имя | Назначение |
|---|---|---|
| `9MmZI` | Fieldset | Группировка полей формы (legend + slot) |

Fieldset имеет `cornerRadius: 8`, `stroke` по `$border-color`.
Содержимое добавляется в слот `g3slQ` (body > slot):
```javascript
fieldset = I("parentFrame", { type: "ref", ref: "9MmZI", width: "fill_container" })
// Добавить поле в слот fieldset
field = I(fieldset + "/G1f11/g3slQ", { type: "ref", ref: "lkIoV" })
```

#### Таблица

| ID | Имя | Ширина | Высота |
|---|---|---|---|
| `j0tlG` | TableHeaderCell | 160 | 40 |
| `0SQTx` | TableCell | 160 | 40 |

Структура таблицы:
```
table (frame, layout: vertical)
  └── header-row (frame, layout: horizontal)
  │     └── TableHeaderCell × N
  └── data-row (frame, layout: horizontal)
        └── TableCell × N
```

#### Поиск и фильтры

| ID | Имя | Назначение |
|---|---|---|
| `ZkQzy` | SearchInput | Глобальный поиск (240px) |
| `9YP2Q` | FilterInput | Inline-фильтр столбца (140px) |

#### Статусы и навигация

| ID | Имя | Назначение |
|---|---|---|
| `20i59` | StatusBadge | Бейдж статуса (зелёный по умолчанию) |
| `L7EJA` | NavItem | Пункт меню сайдбара |
| `CjdTS` | ValidationError | Сообщение об ошибке валидации |

---

### Структура экранов в design.pen

| ID | Название | Описание |
|---|---|---|
| `Kew2a` | == Компоненты == | Витрина переиспользуемых компонентов |
| `E22aW` | Главный экран — Заявки | Основной экран с таблицей заявок |
| `RMLon` | Диалог редактирования заявки | Модальное окно редактирования |
| `IQOAD` | Список пропусков | Экран списка пропусков |

---

### Паттерн: экран-список (табличная форма)

```
screen (frame, layout: vertical, fill: $surface-50)
  ├── toolbar (frame, layout: horizontal, height: 56, padding: [0,16])
  │     ├── ButtonPrimary (label: "Создать", icon: "plus")
  │     ├── ButtonOutline (label: "Обновить", icon: "refresh-cw")
  │     └── SearchInput (ml-auto)
  └── table-wrap (frame, layout: vertical, flex-1)
        ├── header-row (frame, layout: horizontal, fill: $surface-100)
        │     └── TableHeaderCell × N
        └── data-row × N (frame, layout: horizontal)
              └── TableCell × N
```

---

### Паттерн: карточка-форма (редактирование)

```
dialog (frame, layout: vertical, width: 900–1100)
  ├── header (molcom-grid-bg, Archivo Black, $dark-blue)
  └── content (frame, layout: horizontal, gap: 24)
        ├── left-column (frame, layout: vertical, gap: 16)
        │     ├── Fieldset (legend: "Общая информация")
        │     │     └── slot: DropdownInput × 2
        │     └── Fieldset (legend: "Срок действия")
        │           └── slot: DateInput × 1–2
        └── right-column (frame, layout: vertical)
              └── Fieldset (legend: "Данные посетителя")
                    └── slot: StringInput × 4, DropdownInput × 1
```

---

### Правила работы с design.pen

1. **Placeholder-флаг обязателен** на каждом фрейме во время работы над ним. Снимать только после завершения.
2. **Никогда не хардкодить** числовые значения там, где есть переменная (`$molcom-blue`, `$surface-0` и т.д.).
3. **Использовать существующие компоненты** через `ref`, не создавать дубликаты.
4. **Использовать flexbox-layout** (`layout: "vertical"` / `"horizontal"`), избегать абсолютных координат.
5. **Предпочитать `fill_container` / `fit_content`** вместо хардкода размеров.
6. **Текст всегда требует `fill`** — без него невидим.
7. **Называть ноды по-русски** или по-английски понятными именами (не `frame-1`).
8. **После каждого блока изменений** — вызов `mcp_pencil_get_screenshot()` для верификации.

---

### Стиль MOLCOM

- Фирменный паттерн: белый фон с синей сеткой (molcom-grid-bg) — используется в сайдбаре, топбаре, заголовках диалогов.
- Скругление диалогов: `cornerRadius: 16`.
- Скругление карточек/полей: `cornerRadius: 6–8`.
- Тени: `effect: { type: "shadow", blur: 8, spread: 0, color: "#0000001A", offset: {x: 0, y: 2} }`.
- Иконки: lucide icon font (`iconFontFamily: "lucide"`), размер 14–18px.

---

### Соответствие Angular-компонентов и Pencil-компонентов

| Angular-компонент | Pencil-компонент | ID |
|---|---|---|
| `app-string-input` | StringInput | `lkIoV` |
| `app-dropdown-input` | DropdownInput | `7IxY0` |
| `app-date-input` | DateInput | `g12M9` |
| `app-classifier-input` | ClassifierInput | `lOrvS` |
| `p-button` (primary) | ButtonPrimary | `XWXQf` |
| `p-button [outlined]` | ButtonOutline | `g5sAO` |
| `p-button [icon-only]` | ButtonIcon | `GOU8W` |
| `p-fieldset` | Fieldset | `9MmZI` |
| `p-table th` | TableHeaderCell | `j0tlG` |
| `p-table td` | TableCell | `0SQTx` |
| `p-iconfield + p-inputicon` | SearchInput | `ZkQzy` |
| `p-tag` / статус-бейдж | StatusBadge | `20i59` |
| Навигационный пункт | NavItem | `L7EJA` |
| Ошибка валидации `<small>` | ValidationError | `CjdTS` |




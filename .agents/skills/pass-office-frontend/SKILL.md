---
name: pass-office-frontend
description: Разработка frontend-компонентов для Angular-приложения «Бюро пропусков» (Molcom.PassOffice.Api/ClientApp). Используй этот скилл при создании/изменении Angular-компонентов, экранов, форм, таблиц, карточек, сервисов, фильтров и любых UI-элементов «Бюро пропусков». Покрывает: Angular 21 standalone-компоненты, PrimeNG 21, Tailwind CSS 4 + tailwindcss-primeui, signals, reactive forms, дизайн-токены MOLCOM.
---

# Скилл: Frontend «Бюро пропусков» (Molcom.PassOffice.Api/ClientApp)

> **Когда активировать:** при любой задаче на фронтенд-разработку в приложении «Бюро пропусков» — создание экранов, компонентов, форм, таблиц, карточек, фильтров, сервисов, стилей, маршрутов.

---

## 1. Стек и версии

| Библиотека | Версия | Ключевые особенности |
|---|---|---|
| Angular | ^21.2 | Standalone Components, Signals, Control Flow (`@if`, `@for`) |
| PrimeNG | ^21.1 | Компоненты UI, токены дизайна, theming через CSS-переменные |
| Tailwind CSS | ^4.2 | Утилитарные классы, `@theme`, `@variant`, CSS Layers |
| tailwindcss-primeui | ^0.6 | Семантические классы (`bg-primary`, `text-muted-color`), `p-*:` варианты |
| RxJS | ~7.8 | Observable для HTTP и сложных потоков |
| TypeScript | ~5.9 | Строгая типизация |

---

## 2. Структура проекта

```
Molcom.PassOffice.Api/ClientApp/src/app/
  ├── app.component.ts/.html        # Корневой компонент (router-outlet)
  ├── app.routes.ts                 # Маршруты (lazy loading)
  ├── core/
  │     └── services/               # Синглтон-сервисы (API-клиенты, auth, config)
  ├── features/                     # Feature-модули (каждый — отдельный экран/домен)
  │     ├── requests/               # Заявки на пропуск
  │     │     ├── request-list/     # Экран-список с таблицей
  │     │     ├── request-card/     # Карточка редактирования (диалог)
  │     │     ├── approver-card/    # Карточка согласующего
  │     │     └── route-card/       # Карточка маршрута
  │     ├── passes/                 # Пропуска
  │     ├── employees/              # Сотрудники
  │     ├── checkpoint/             # КПП
  │     ├── dashboard/              # Дашборд
  │     ├── login/                  # Форма входа
  │     └── design-system/          # Витрина дизайн-системы
  └── shared/
        ├── components/             # Переиспользуемые UI-компоненты
        │     ├── inputs/           # string-input, dropdown-input, date-input, classifier-input, boolean-input, textarea-input
        │     ├── buttons/          # clear-button и другие кнопки
        │     ├── column-filters/   # column-filter-input, column-filter-date, column-filter-select, column-filter-boolean, column-filter-number
        │     ├── fieldset-panel/   # Обёртка p-fieldset
        │     ├── form-field/       # Обёртка label+control
        │     ├── global-filter/    # Глобальный поиск
        │     ├── kpi-card/         # KPI-карточка дашборда
        │     ├── pagination/       # Кастомная пагинация
        │     ├── search-input/     # Поиск с иконкой
        │     ├── status-badge/     # Бейдж статуса
        │     ├── table-cell/       # Ячейка таблицы
        │     ├── table-header-cell/# Заголовок столбца
        │     ├── table-header-sortable/ # Сортируемый заголовок
        │     ├── topbar/           # Верхняя панель навигации
        │     ├── validation-error/ # Сообщение об ошибке валидации
        │     └── classifier-picker-dialog/ # Диалог выбора из справочника
        └── models/                 # Интерфейсы, типы, enum-ы
```

---

## 3. Обязательные правила компонентов

### 3.1 Структура компонента

```typescript
import { ChangeDetectionStrategy, Component, computed, input, output, signal, inject } from '@angular/core';

/**
 * Описание компонента на русском языке.
 * Соответствует контролу <ИмяКонтрола> из дизайн-системы design.pen.
 */
@Component({
  selector: 'app-my-component',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-component.component.html',
  imports: [/* только нужные модули */],
})
export class MyComponent {
  // DI через inject(), не через конструктор
  private readonly myService = inject(MyService);

  // Входные данные через input()
  readonly label = input<string>('');
  readonly data = input.required<MyData>();

  // Выходные события через output()
  readonly saved = output<void>();
  readonly selected = output<MyData>();

  // Состояние через signal()
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Производные данные через computed()
  readonly itemCount = computed(() => this.data().items.length);
  readonly isEmpty = computed(() => this.itemCount() === 0);
}
```

### 3.2 Шаблон (HTML)

- Всегда в **отдельном файле** `.html`.
- **Control Flow** — только `@if`, `@for` (с `track`), `@switch`:

```html
@if (isLoading()) {
  <p-progressBar mode="indeterminate" />
} @else {
  @for (item of items(); track item.id) {
    <app-item [data]="item" />
  } @empty {
    <span class="text-text-muted text-sm">Нет данных</span>
  }
}
```

- **ЗАПРЕЩЕНО:** `*ngIf`, `*ngFor`, `*ngSwitch`, `ng-template` (кроме PrimeNG шаблонов).

### 3.3 Формы

- Только реактивные формы (`FormBuilder`, `ReactiveFormsModule`).
- Перед отправкой: `form.markAllAsTouched()`.
- Ошибки показывать при `invalid && touched`:

```html
@if (form.controls.name.invalid && form.controls.name.touched) {
  <app-validation-error message="Обязательное поле" />
}
```

---

## 4. Стилизация: Tailwind + PrimeNG

### 4.1 Общие правила

- Стилизация **ТОЛЬКО** через Tailwind-утилиты и PrimeNG-компоненты.
- **ЗАПРЕЩЕНЫ:** кастомные CSS/SCSS, `styleUrls`, `[style]`-биндинги, `styles: [...]`.
- Исключение: глобальные стили в `src/styles.css` для PrimeNG-токенов и нормализации.

### 4.2 Дизайн-токены MOLCOM

Определены в `src/styles.css` через `@theme`:

| Токен | CSS-переменная | Назначение |
|---|---|---|
| Основной цвет | `--color-primary` (#0060FF) | Кнопки, акценты, ссылки |
| Наведение | `--color-primary-hover` (#2151F2) | Hover-состояние кнопок |
| Тёмно-синий | `--color-dark-blue` (#051533) | Заголовки h1, fieldset legend |
| Фон страницы | `--color-background` (#F8F9FC) | bg страниц |
| Белый | `--color-surface` (#FFFFFF) | Карточки, поля |
| Альт. фон | `--color-surface-alt` (#F0F4FF) | Заголовки таблиц, hover строк |
| Граница | `--color-border` (#DDE3EF) | Рамки полей, таблиц |
| Текст основной | `--color-text-primary` (#051533) | Основной текст |
| Текст вторичный | `--color-text-secondary` (#6B7A99) | Подписи, описания |
| Текст приглушённый | `--color-text-muted` (#9BA8C2) | Placeholder, disabled |
| Успех | `--color-success` (#22C55E) | Статус «Согласована» |
| Опасность | `--color-danger` (#EF4444) | Статус «Отклонена» |
| Предупреждение | `--color-warning` (#F59E0B) | Статус «На согласовании» |
| Скругление SM | `--radius-sm` (6px) | Поля, кнопки |
| Скругление MD | `--radius-md` (8px) | Карточки |
| Скругление LG | `--radius-lg` (12px) | Диалоги |

### 4.3 Tailwind-классы (приоритет семантических)

**Используй кастомные токены из `@theme`:**
```html
<!-- ✅ ПРАВИЛЬНО — семантические классы из @theme -->
<div class="bg-background text-text-primary border border-border rounded-md">
<button class="bg-primary text-text-on-primary hover:bg-primary-hover rounded-sm">
<span class="text-text-secondary text-sm">
<div class="bg-surface-alt">

<!-- ❌ НЕПРАВИЛЬНО — прямые цветовые значения -->
<div class="bg-blue-500 text-gray-700 border-gray-200">
```

### 4.4 tailwindcss-primeui варианты

Используй `p-*:`-варианты для стилизации состояний PrimeNG:

```html
<div class="p-disabled:opacity-50 p-disabled:cursor-not-allowed">
<div class="p-selected:bg-primary-light p-selected:font-semibold">
<div class="p-focus:ring-2 p-focus:ring-primary">
```

### 4.5 Адаптивность

- Mobile-first: `sm:`, `md:`, `lg:` префиксы.
- Динамические классы через `computed()`:

```typescript
readonly cardClass = computed(() =>
  this.selected()
    ? 'border-2 border-primary bg-primary-light'
    : 'border border-border bg-surface'
);
```

---

## 5. Фирменный стиль MOLCOM

### 5.1 Шрифты
- **Inter** — основной интерфейсный шрифт (все тексты).
- **Archivo Black** — заголовки h1, легенды fieldset, заголовки диалогов.

### 5.2 Фирменная сетка
Белый фон + синие линии. CSS-класс: `molcom-grid-bg`.
Применяется в: заголовках диалогов, сайдбаре, форме входа.

### 5.3 Стиль компонентов

| Элемент | Скругление | Высота | Особенности |
|---|---|---|---|
| Поля ввода | 6px | 38px | Граница `--color-border`, фон `--color-surface` |
| Кнопки Primary | 6px | auto | Фон `--color-primary`, текст белый |
| Кнопки Outlined | 6px | auto | Граница `--color-border`, фон `--color-surface` |
| Диалоги | 16px | — | Заголовок с `molcom-grid-bg`, Archivo Black |
| Fieldset | 8px | — | Легенда `--color-dark-blue`, Archivo Black |
| Таблицы | 0 (обёртка `rounded-md`) | — | Заголовки `--color-surface-alt` |

---

## 6. Паттерны экранов

### 6.1 Экран-список (таблица)

Структура:
```
page (flex flex-col h-full bg-background)
  ├── topbar (TopbarComponent)
  ├── toolbar (flex items-center gap-2 px-4 py-2 bg-surface border-b border-border)
  │     ├── p-button (primary) — «Создать»
  │     ├── p-button (outlined, secondary) — «Обновить»
  │     ├── p-button (outlined, secondary) — «Объединить» [disabled если < 2 выбрано]
  │     ├── spacer (flex-1)
  │     └── app-global-filter — поиск
  ├── table-container (flex-1 overflow-auto)
  │     └── p-table (с column-filter-* в заголовках)
  └── app-pagination (border-t border-border)
```

Ключевые приёмы:
- `p-table` с `[value]`, `[globalFilterFields]`, `[selection]`, `[expandedRowKeys]`.
- Фильтры колонок: `app-column-filter-input` (текст), `app-column-filter-date` (дата), `app-column-filter-select` (multiselect).
- Кастомная пагинация через `app-pagination` (signal-based, без PrimeNG Paginator).
- Строки с иерархией: `expandedRowKeys`, `pRowToggler`, дочерние строки через `@for`.

### 6.2 Карточка-форма (диалог)

Структура:
```
p-dialog [modal] [maximizable] [header]="Заголовок"
  ├── content (grid grid-cols-2 gap-6 p-6)
  │     ├── left-column (flex flex-col gap-4)
  │     │     ├── p-fieldset legend="Общая информация"
  │     │     │     ├── app-dropdown-input label="Тип гостя"
  │     │     │     └── app-dropdown-input label="Тип пропуска"
  │     │     └── p-fieldset legend="Срок действия"
  │     │           ├── app-date-input label="Дата начала"
  │     │           └── app-date-input label="Дата окончания"
  │     └── right-column (flex flex-col gap-4)
  │           └── p-fieldset legend="Данные посетителя"
  │                 ├── app-string-input label="Фамилия"
  │                 ├── app-string-input label="Имя"
  │                 └── ...
  └── footer (flex justify-end gap-2 p-4 border-t border-border)
        ├── p-button [outlined] severity="secondary" — «Отмена»
        └── p-button — «Сохранить»
```

### 6.3 Дашборд

- KPI-карточки (`app-kpi-card`) в ряду.
- Графики (если нужны).
- Быстрые действия.

---

## 7. Переиспользуемые компоненты (shared)

### 7.1 Поля ввода (app-*-input)

Все поля имеют единый интерфейс:

| Свойство | Тип | Назначение |
|---|---|---|
| `label` | `input<string>` | Текст метки |
| `required` | `input<boolean>` | Обязательное поле |
| `placeholder` | `input<string>` | Плейсхолдер |
| `value` | `model<T>` | Двусторонняя привязка |
| `showError` | `input<boolean>` | Показывать ошибку |
| `errorMessage` | `input<string>` | Текст ошибки |
| `cleared` | `output<void>` | Событие очистки |

Компоненты:
- `app-string-input` — текстовое поле
- `app-dropdown-input` — выпадающий список (p-select)
- `app-date-input` — дата (p-datepicker)
- `app-classifier-input` — справочник с автоподбором (p-autocomplete)
- `app-boolean-input` — чекбокс (p-checkbox)
- `app-textarea-input` — многострочный текст (p-textarea)

### 7.2 Фильтры колонок (column-filter-*)

Компактные фильтры встроены в заголовки таблицы. Высота 26px, без рамок (transparent bg).

| Компонент | Фильтрация | PrimeNG-компонент |
|---|---|---|
| `app-column-filter-input` | Текст (contains) | Input с debounce |
| `app-column-filter-date` | Дата (equals) | p-datepicker |
| `app-column-filter-select` | Множественный выбор (in) | p-multiselect |
| `app-column-filter-boolean` | Да/Нет | p-checkbox |
| `app-column-filter-number` | Число | p-inputnumber |

### 7.3 Статус-бейдж (app-status-badge)

```html
<app-status-badge [label]="request.status"
                  [severity]="getStatusBadgeSeverity(request.status)" />
```

Severity: `'success' | 'warning' | 'danger' | 'info' | 'secondary'`.
Визуал: цветная точка + текст (без цветного фона).

### 7.4 Пагинация (app-pagination)

Signal-based компонент. Интерфейс:
- `totalRecords: input<number>` — общее количество записей
- `pageSize: input<number>` — размер страницы (по умолчанию 50)
- `currentPage: input<number>` — текущая страница
- `pageChanged: output<number>` — смена страницы
- `pageSizeChanged: output<number>` — смена размера

---

## 8. Рекомендуемый MCP-цикл

При задачах фронтенд-разработки обычно полезно выполнить следующий цикл:

### Шаг 1: Документация (ПЕРЕД написанием кода)

```
// Angular — актуальный API
mcp_context7_resolve-library-id("Angular", "standalone components signals")
mcp_context7_query-docs("/angular/angular", "input output signal computed inject")

// PrimeNG — API нужного компонента
mcp_primeng_get_component_props("Table")
mcp_primeng_get_usage_example("Table")

// Tailwind + PrimeUI — семантические классы
mcp_primeng_get_tailwind_guide()
mcp_context7_resolve-library-id("tailwindcss-primeui", "semantic color classes")
```

### Шаг 2: Реализация

Написать код, следуя правилам этого скилла.

### Шаг 3: Проверка (ПОСЛЕ реализации)

```
// Визуальная проверка через Chrome DevTools MCP
navigate → http://localhost:4200/нужный-маршрут
screenshot → визуальный контроль
get_console_logs(error) → убедиться, что нет ошибок
```

---

## 9. Чеклист перед завершением задачи

- [ ] Компонент `standalone: true` с `OnPush`
- [ ] Шаблон в отдельном `.html` файле
- [ ] DI через `inject()`, входы через `input()`, выходы через `output()`
- [ ] Состояние через `signal()`, производные через `computed()`
- [ ] Control Flow: `@if`, `@for` (с `track`), `@switch`
- [ ] Стили только через Tailwind-утилиты (семантические токены из `@theme`)
- [ ] Нет кастомного CSS/SCSS (`styleUrls`, `styles`, `[style]`)
- [ ] API PrimeNG-компонента проверен через MCP или актуальную документацию перед использованием
- [ ] Формы реактивные (`FormBuilder`, `markAllAsTouched()`)
- [ ] Все комментарии на русском языке
- [ ] Маршрут добавлен в `app.routes.ts` (lazy loading)
- [ ] Результат проверен через Chrome DevTools MCP или Playwright

---

## 10. Типичные ошибки (избегать!)

| ❌ Ошибка | ✅ Правильно |
|---|---|
| `@Input() label: string` | `readonly label = input<string>('')` |
| `constructor(private svc: Service)` | `private readonly svc = inject(Service)` |
| `*ngIf="condition"` | `@if (condition) { ... }` |
| `*ngFor="let item of items"` | `@for (item of items; track item.id) { ... }` |
| `BehaviorSubject` для UI-состояния | `signal()` |
| `bg-blue-500` | `bg-primary` (токен из @theme) |
| `[style.color]="'red'"` | `class="text-danger"` |
| `styleUrls: ['./x.css']` | Tailwind-утилиты в шаблоне |
| `template: '<div>...'` (inline) | `templateUrl: './x.component.html'` |
| Использование PrimeNG без проверки API | `mcp_primeng_get_component_props` / docs → код |

---

## 11. Команды разработки

```powershell
# Запуск dev-сервера
cd Molcom.PassOffice.Api\ClientApp; npm start

# Production-сборка
cd Molcom.PassOffice.Api\ClientApp; npm run build

# Генерация компонента (Angular CLI)
cd Molcom.PassOffice.Api\ClientApp; npx ng generate component features/my-feature/my-component --standalone --change-detection OnPush
```

---

## 12. Дизайн-файл и Pencil MCP

Дизайн-файл: `Molcom.PassOffice.Api/ClientApp/design.pen`

Соответствие Angular ↔ Pencil-компоненты:

| Angular | Pencil (ref ID) |
|---|---|
| `app-string-input` | StringInput (`lkIoV`) |
| `app-dropdown-input` | DropdownInput (`7IxY0`) |
| `app-date-input` | DateInput (`g12M9`) |
| `app-classifier-input` | ClassifierInput (`lOrvS`) |
| `p-button` primary | ButtonPrimary (`XWXQf`) |
| `p-button` outlined | ButtonOutline (`g5sAO`) |
| `p-button` icon-only | ButtonIcon (`GOU8W`) |
| `p-fieldset` | Fieldset (`9MmZI`) |
| `app-status-badge` | StatusBadge (`20i59`) |
| `app-pagination` | (нет отдельного) |
| `app-global-filter` | SearchInput (`ZkQzy`) |
| `app-validation-error` | ValidationError (`CjdTS`) |

При работе с дизайном сначала изучи компоненты через `mcp_pencil_batch_get`, затем применяй изменения через `mcp_pencil_batch_design`.

---

## 13. Скобка-коннектор (иерархические таблицы)

Для визуальной группировки дочерних строк в таблицах используется CSS-паттерн «скобка-коннектор»:

- Полукольцевой маркер (`.group-bracket-half-ring`) у раскрытой родительской строки.
- Вертикальная линия (`.group-bracket-line::before`) через все дочерние строки.
- Горизонтальный отросток (`.group-bracket-line::after`) к кольцевому маркеру (`.group-bracket-ring`).
- Последняя строка — L-форма со скруглением (`.group-bracket-line--last`).
- Все линии: 2px, `var(--color-primary)`, opacity 0.35.

Стили определены глобально в `src/styles.css`.


## Angular Frontend — правила разработки

### Стек (Molcom.PassOffice.Api/ClientApp)

| Инструмент | Версия |
|---|---|
| Angular | ^21.2 |
| PrimeNG | ^21.1 |
| Tailwind CSS | ^4.2 |
| tailwindcss-primeui | ^0.6 |
| RxJS | ~7.8 |
| TypeScript | ~5.9 |

> `Molcom.Terminal.Api/ClientApp` — устаревшее Angular 16 приложение (NgModules + Angular Material). Не применяй к нему эти правила.

---

### Архитектура компонентов

- **Только Standalone Components**: `standalone: true`, никаких `NgModule`.
- **Всегда** указывать `changeDetection: ChangeDetectionStrategy.OnPush`.
- Разделять шаблон и логику: `.html` — отдельный файл, не inline `template`.
- Для DI использовать `inject()`, не инъекцию через конструктор.
- Для входных/выходных данных использовать `input()` / `output()` (не `@Input`/`@Output`).

```typescript
// ✅ Правильно
@Component({
  selector: 'app-example',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './example.component.html',
  imports: [ButtonModule, ...],
})
export class ExampleComponent {
  private readonly service = inject(ExampleService);
  readonly label = input<string>('');
  readonly saved = output<void>();
}

// ❌ Неправильно — конструктор, inline шаблон, @Input
```

---

### Состояние: Signals

- Реактивное состояние — `signal()`, производные — `computed()`.
- Для асинхронных операций — отдельный signal статуса:

```typescript
readonly isLoading = signal(false);
readonly errorMessage = signal<string | null>(null);

async submit(): Promise<void> {
  this.isLoading.set(true);
  try {
    await this.service.save(this.form.value);
  } catch {
    this.errorMessage.set('Ошибка сохранения. Попробуйте позже.');
  } finally {
    this.isLoading.set(false);
  }
}
```

- **Запрещено** использовать `BehaviorSubject` / `Subject` там, где достаточно `signal`.

---

### Шаблоны: Control Flow

Использовать только `@if`, `@for`, `@switch` — никаких `*ngIf`, `*ngFor`, `*ngSwitch`.

```html
@if (isLoading()) {
  <p-progressBar mode="indeterminate" />
} @else {
  @for (item of items(); track item.id) {
    <app-item [data]="item" />
  } @empty {
    <span class="text-surface-400">Нет данных</span>
  }
}
```

---

### Формы

- Реактивные формы (`ReactiveFormsModule`, `FormBuilder`) — не template-driven.
- Все поля проверять через `markAllAsTouched()` перед отправкой.
- Показывать ошибку только после `touched` + `invalid`:

```html
@if (form.controls.name.invalid && form.controls.name.touched) {
  <p-message severity="error" text="Поле обязательно" />
}
```

---

### UI: PrimeNG + Tailwind

- Стилизация **исключительно** через утилитарные классы Tailwind и компоненты PrimeNG.
- **Запрещены** кастомные CSS/SCSS классы, `[style]`-биндинги, `styleUrls`.
- Использовать семантические токены PrimeNG (`bg-surface-0`, `text-primary`, `border-surface-200`).
- Динамические классы — через `computed()`:

```typescript
readonly cardClass = computed(() =>
  this.selected()
    ? 'border-2 border-primary bg-primary-50'
    : 'border border-surface-200 bg-surface-0'
);
```

- Адаптивность — mobile-first, через префиксы Tailwind (`sm:`, `md:`, `lg:`).
- Интерактивные состояния — через Tailwind (`hover:`, `focus:`, `active:`, `disabled:`).

---

### MCP-инструменты в процессе разработки

При генерации кода агенту рекомендуется:

1. **context7** — получать актуальную документацию Angular, PrimeNG, Tailwind перед реализацией.
2. **primeng-mcp-server** — уточнять props, events, шаблоны конкретного PrimeNG-компонента.
3. **chrome-dev-tools-mcp** — после реализации:
   - запустить визуальный анализ страницы,
   - сделать скриншот,
   - проверить DOM-структуру и адаптивность.
4. При расхождении с требованиями — итеративно исправлять и повторять проверку до 100% соответствия.

---

### Прочее

- Все комментарии в коде — **только на русском языке**.
- Приватные поля класса — `readonly` где возможно, с префиксом `private readonly`.
- Публичные данные компонента — `readonly` сигналы или `readonly`-поля.
- Не создавать `app.module.ts` — точка входа через `bootstrapApplication`.


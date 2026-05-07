# Portfolio

В репозитории собран структурированный материал с примерами кода по репозиториям организации `OOO-IT-MOLCOM`.

## Что добавлено

- `tools/collect_org_examples.py` — скрипт для автоматического сбора примеров кода из репозиториев организации.
- `examples/ooo-it-molcom-code-examples.md` — итоговый компонуемый документ с описанием и примерами.

## Как обновлять подборку примеров

1. Создайте персональный GitHub token с доступом к репозиториям организации.
2. Запустите:

```bash
export GITHUB_TOKEN=your_token_here
python tools/collect_org_examples.py \
  --org OOO-IT-MOLCOM \
  --output examples/ooo-it-molcom-code-examples.md
```

Скрипт сам:
- получает список репозиториев организации;
- выбирает показательные code-snippet'ы;
- собирает единый структурированный markdown-файл с ссылками и описанием.

# Примеры кода: организация `OOO-IT-MOLCOM`

Документ собирается автоматически скриптом:
`/home/runner/work/portfolio/portfolio/tools/collect_org_examples.py`.

## Текущий статус

В среде выполнения агента запрос к API GitHub для этой организации заблокирован, поэтому автоматически получить список репозиториев не удалось.

## Как собрать/обновить у себя

```bash
python /home/runner/work/portfolio/portfolio/tools/collect_org_examples.py \
  --org OOO-IT-MOLCOM \
  --output /home/runner/work/portfolio/portfolio/examples/ooo-it-molcom-code-examples.md
```

После запуска с рабочим доступом к GitHub API файл будет заполнен:

- списком репозиториев организации;
- короткими показательными примерами кода;
- ссылками на источник и кратким описанием по каждому репозиторию.

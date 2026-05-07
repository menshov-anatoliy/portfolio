#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import json
import os
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from typing import Iterable


API_BASE = "https://api.github.com"
ALLOWED_EXTENSIONS = {
    ".py",
    ".js",
    ".ts",
    ".tsx",
    ".jsx",
    ".go",
    ".java",
    ".kt",
    ".cs",
    ".rb",
    ".php",
    ".rs",
    ".swift",
    ".scala",
    ".sql",
}
SKIP_PATH_PARTS = {"node_modules", "dist", "build", "vendor", "generated", "__pycache__"}
SNIPPET_START_MARKERS = ("class ", "def ", "func ", "interface ", "type ", "export ", "public ")


@dataclass
class Repository:
    full_name: str
    name: str
    default_branch: str
    html_url: str
    description: str | None


class GitHubClient:
    def __init__(self, token: str) -> None:
        self.token = token

    def get_json(self, url: str) -> object:
        request = urllib.request.Request(
            url,
            headers={
                "Authorization": f"Bearer {self.token}",
                "Accept": "application/vnd.github+json",
                "X-GitHub-Api-Version": "2022-11-28",
                "User-Agent": "portfolio-org-examples-collector",
            },
        )
        try:
            with urllib.request.urlopen(request) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            body = error.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"GitHub API error {error.code} for {url}: {body}") from error

    def list_repositories(self, org: str, max_repos: int) -> list[Repository]:
        url = f"{API_BASE}/orgs/{urllib.parse.quote(org)}/repos?per_page={max_repos}&sort=updated"
        payload = self.get_json(url)
        if not isinstance(payload, list):
            raise RuntimeError(f"Unexpected repositories payload: {payload}")

        repos: list[Repository] = []
        for item in payload:
            if not isinstance(item, dict):
                continue
            repos.append(
                Repository(
                    full_name=str(item.get("full_name", "")),
                    name=str(item.get("name", "")),
                    default_branch=str(item.get("default_branch", "main")),
                    html_url=str(item.get("html_url", "")),
                    description=item.get("description"),
                )
            )
        return repos

    def list_files(self, repo: Repository) -> list[str]:
        tree_url = (
            f"{API_BASE}/repos/{urllib.parse.quote(repo.full_name)}/git/trees/"
            f"{urllib.parse.quote(repo.default_branch)}?recursive=1"
        )
        payload = self.get_json(tree_url)
        if not isinstance(payload, dict):
            return []

        tree = payload.get("tree")
        if not isinstance(tree, list):
            return []

        file_paths: list[str] = []
        for item in tree:
            if not isinstance(item, dict) or item.get("type") != "blob":
                continue
            path = str(item.get("path", ""))
            if not path:
                continue
            if any(part in SKIP_PATH_PARTS for part in path.split("/")):
                continue
            if self._is_code_file(path):
                file_paths.append(path)
        return file_paths

    def get_file_content(self, repo: Repository, path: str) -> str:
        content_url = (
            f"{API_BASE}/repos/{urllib.parse.quote(repo.full_name)}/contents/"
            f"{urllib.parse.quote(path)}?ref={urllib.parse.quote(repo.default_branch)}"
        )
        payload = self.get_json(content_url)
        if not isinstance(payload, dict):
            raise RuntimeError(f"Unexpected file payload for {repo.full_name}:{path}")

        encoded_content = payload.get("content")
        if not isinstance(encoded_content, str):
            raise RuntimeError(f"File content is missing for {repo.full_name}:{path}")

        normalized = encoded_content.replace("\n", "")
        return base64.b64decode(normalized).decode("utf-8", errors="replace")

    @staticmethod
    def _is_code_file(path: str) -> bool:
        lower = path.lower()
        return any(lower.endswith(ext) for ext in ALLOWED_EXTENSIONS)


def select_snippet(content: str, snippet_lines: int) -> str:
    lines = content.splitlines()
    if not lines:
        return ""

    start = 0
    for idx, line in enumerate(lines):
        stripped = line.strip()
        if stripped.startswith(SNIPPET_START_MARKERS):
            start = idx
            break

    end = min(len(lines), start + snippet_lines)
    snippet = lines[start:end]

    while snippet and not snippet[0].strip():
        snippet.pop(0)
    while snippet and not snippet[-1].strip():
        snippet.pop()

    return "\n".join(snippet)


def choose_example_file(paths: Iterable[str]) -> str | None:
    """Pick a representative file, preferring root-level and shorter paths."""
    prioritized = sorted(paths, key=lambda p: (p.count("/"), len(p)))
    return prioritized[0] if prioritized else None


def render_markdown(org: str, repos: list[Repository], snippets: list[str], errors: list[str]) -> str:
    lines: list[str] = [
        f"# Примеры кода: организация `{org}`",
        "",
        "Документ собран автоматически и показывает короткие, читаемые фрагменты из репозиториев организации.",
        "",
    ]

    if not repos:
        lines.extend(
            [
                "## Результат",
                "",
                "Не удалось получить список репозиториев. Проверьте права доступа токена.",
                "",
            ]
        )

    lines.extend(snippets)

    if errors:
        lines.extend(["## Ошибки сбора", ""])
        lines.extend(f"- {error}" for error in errors)
        lines.append("")

    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Collect code examples from all organization repositories.")
    parser.add_argument("--org", default="OOO-IT-MOLCOM", help="GitHub organization name")
    parser.add_argument("--output", required=True, help="Path to resulting markdown file")
    parser.add_argument("--max-repos", type=int, default=100, help="Maximum number of repositories to process")
    parser.add_argument("--snippet-lines", type=int, default=30, help="Maximum snippet length in lines")
    args = parser.parse_args()

    token = os.getenv("GITHUB_TOKEN", "").strip()
    if not token:
        raise SystemExit("GITHUB_TOKEN is required to collect organization repositories.")

    client = GitHubClient(token=token)
    errors: list[str] = []
    snippets: list[str] = []

    try:
        repositories = client.list_repositories(args.org, args.max_repos)
    except RuntimeError as error:
        repositories = []
        errors.append(str(error))

    for repo in repositories:
        try:
            files = client.list_files(repo)
            target_file = choose_example_file(files)
            if not target_file:
                errors.append(f"{repo.full_name}: не найдено подходящих исходных файлов")
                continue

            content = client.get_file_content(repo, target_file)
            snippet = select_snippet(content, args.snippet_lines)
            if not snippet:
                errors.append(f"{repo.full_name}: файл {target_file} не содержит подходящего фрагмента")
                continue

            extension = os.path.splitext(target_file)[1].lstrip(".") or "text"
            file_url = f"{repo.html_url}/blob/{repo.default_branch}/{target_file}"
            repo_description = repo.description or "Описание отсутствует"

            snippets.extend(
                [
                    f"## {repo.full_name}",
                    "",
                    f"- Репозиторий: [{repo.full_name}]({repo.html_url})",
                    f"- Описание: {repo_description}",
                    f"- Файл примера: [{target_file}]({file_url})",
                    "",
                    f"```{extension}",
                    snippet,
                    "```",
                    "",
                ]
            )
        except RuntimeError as error:
            errors.append(f"{repo.full_name}: {error}")

    markdown = render_markdown(args.org, repositories, snippets, errors)
    output_path = os.path.abspath(args.output)
    output_dir = os.path.dirname(output_path)
    if output_dir:
        os.makedirs(output_dir, exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as file:
        file.write(markdown)

    print(f"Готово: {output_path}")


if __name__ == "__main__":
    main()

"""Jornada do explorador de pastas com a API simulada no browser.

Executa pela borda nginx com Angular em container (sem BFF, API ou Keycloak),
interceptando as rotas HTTP com dados sintéticos. Não usa credenciais.
"""

import json
import math
import os
from pathlib import Path
import time
from uuid import uuid4

import pytest
from playwright.sync_api import Page, Route, expect

pytestmark = [pytest.mark.e2e, pytest.mark.mocked]

REPORTS = Path(os.getenv("ALTIVY_E2E_REPORTS", "/app/reports"))


class FakeVault:
    def __init__(self) -> None:
        self.folders: set[str] = {"projetos"}
        self.notes: dict[str, str] = {
            "projetos/plano.md": "# Plano sintético\n",
            "projetos/sem-metadados.md": "# Nota externa\n",
        }
        self.trash: dict[str, dict[str, object]] = {}
        self.requests: list[tuple[str, str, str | None]] = []

    def children(self, parent: str) -> list[dict[str, object]]:
        prefix = f"{parent}/" if parent else ""
        entries: list[dict[str, object]] = []
        for folder in sorted(self.folders):
            if folder.startswith(prefix) and "/" not in folder[len(prefix):]:
                entries.append({"path": folder, "name": folder[len(prefix):], "kind": "folder", "eTag": None})
        for note in sorted(self.notes):
            if note.startswith(prefix) and "/" not in note[len(prefix):]:
                entry: dict[str, object] = {
                    "path": note, "name": note[len(prefix):], "kind": "note", "eTag": "v1"
                }
                if note == "projetos/sem-metadados.md":
                    entry.update({"isUnclassified": True, "hasMinimumMetadata": False})
                entries.append(entry)
        return entries

    def move_folder(self, source: str, target: str) -> None:
        def remap(item: str) -> str:
            return target + item[len(source):] if item == source or item.startswith(f"{source}/") else item
        self.folders = {remap(folder) for folder in self.folders}
        self.notes = {remap(note): content for note, content in self.notes.items()}

    def move_to_trash(self, path: str, kind: str | None = None) -> bool:
        is_folder = path in self.folders if kind is None else kind == "folder"
        if is_folder:
            if path not in self.folders:
                return False
            folders = {folder for folder in self.folders if folder == path or folder.startswith(f"{path}/")}
            notes = {note: content for note, content in self.notes.items() if note.startswith(f"{path}/")}
            self.folders.difference_update(folders)
            for note in notes:
                self.notes.pop(note, None)
        else:
            if path not in self.notes:
                return False
            folders = set()
            notes = {path: self.notes.pop(path)}
        operation_id = str(uuid4())
        parent = path.rsplit("/", 1)[0] if "/" in path else ""
        self.trash[operation_id] = {
            "entry": {
                "id": operation_id,
                "path": path,
                "name": path.rsplit("/", 1)[-1],
                "kind": "folder" if is_folder else "note",
                "deletedAtUtc": "2026-09-28T00:00:00Z",
                "retentionDays": None,
                "canRestoreToOriginal": (not parent or parent in self.folders) and
                    path not in self.folders and path not in self.notes,
            },
            "folders": folders,
            "notes": notes,
        }
        return True

    def trash_listing(self) -> list[dict[str, object]]:
        entries: list[dict[str, object]] = []
        for item in self.trash.values():
            entry = dict(item["entry"])
            path = str(entry["path"])
            parent = path.rsplit("/", 1)[0] if "/" in path else ""
            entry["canRestoreToOriginal"] = (not parent or parent in self.folders) and \
                path not in self.folders and path not in self.notes
            entries.append(entry)
        return sorted(entries, key=lambda item: str(item["deletedAtUtc"]), reverse=True)

    def restore_from_trash(self, operation_id: str, target: str) -> str | None:
        item = self.trash.get(operation_id)
        if item is None:
            return "not-found"
        entry = item["entry"]
        destination = target
        if entry["kind"] == "note" and not destination.lower().endswith(".md"):
            destination = f"{destination}.md"
        parent = destination.rsplit("/", 1)[0] if "/" in destination else ""
        if parent and parent not in self.folders:
            return "trash-destination-folder-missing"
        if destination in self.folders or destination in self.notes:
            return "trash-destination-exists"
        original = str(entry["path"])
        if entry["kind"] == "folder":
            def remap(path: str) -> str:
                return destination + path[len(original):] if path == original or path.startswith(f"{original}/") else path
            self.folders.update(remap(folder) for folder in item["folders"])
            self.notes.update({remap(note): content for note, content in item["notes"].items()})
        else:
            self.notes[destination] = item["notes"][original]
        self.trash.pop(operation_id)
        return None

def _json(route: Route, body: object, status: int = 200) -> None:
    route.fulfill(status=status, content_type="application/json", body=json.dumps(body))


@pytest.fixture
def mocked_workspace(page: Page, base_url: str) -> FakeVault:
    vault = FakeVault()

    def handle(route: Route) -> None:
        request = route.request
        url = request.url.split(base_url, 1)[-1]
        path, _, query = url.partition("?")
        if path == "/bff/session":
            return _json(route, {"authenticated": True, "subject": "synthetic-user", "name": "Pessoa de teste"})
        if path == "/api/v1/vaults":
            return _json(route, {"items": [{"id": "lab-0001", "name": "Vault sintético"}], "nextCursor": None})
        if path == "/api/v1/vaults/lab-0001/notes":
            return _json(route, [{"path": note, "eTag": "v1"} for note in sorted(vault.notes)])
        if path == "/api/v1/vaults/lab-0001/trash":
            from urllib.parse import parse_qs
            query_values = parse_qs(query)
            limit = int(query_values.get("limit", ["20"])[0])
            cursor = query_values.get("cursor", [None])[0]
            items = vault.trash_listing()
            start = next((index + 1 for index, item in enumerate(items) if item["id"] == cursor), 0)
            page = items[start:start + limit]
            next_cursor = page[-1]["id"] if start + len(page) < len(items) and page else None
            return _json(route, {"items": page, "nextCursor": next_cursor})
        if path.startswith("/api/v1/vaults/lab-0001/trash/") and request.method == "PATCH":
            operation_id = path.rsplit("/", 1)[-1]
            target = json.loads(request.post_data or "{}").get("path", "")
            error = vault.restore_from_trash(operation_id, target)
            if error == "not-found":
                return route.fulfill(status=404)
            if error:
                return route.fulfill(status=409, content_type="application/problem+json",
                                     body=json.dumps({"status": 409, "code": error}))
            return route.fulfill(status=204)
        if path == "/api/v1/vaults/lab-0001/entries":
            parent = ""
            if query.startswith("parentPath="):
                from urllib.parse import unquote
                parent = unquote(query.removeprefix("parentPath="))
            return _json(route, vault.children(parent))
        if path.startswith("/api/v1/vaults/lab-0001/folders/"):
            from urllib.parse import unquote
            folder = unquote(path.removeprefix("/api/v1/vaults/lab-0001/folders/"))
            if request.method == "PUT":
                existed = folder in vault.folders
                vault.folders.add(folder)
                return route.fulfill(status=204 if existed else 201)
            if request.method == "DELETE":
                vault.requests.append(("DELETE-FOLDER", folder, None))
                if not vault.move_to_trash(folder, "folder"):
                    return route.fulfill(status=404)
                return route.fulfill(status=204)
            if request.method == "PATCH":
                target = json.loads(request.post_data or "{}")["path"]
                vault.requests.append(("PATCH-FOLDER", folder, target))
                if target in vault.folders or target in vault.notes:
                    return route.fulfill(status=409, content_type="application/problem+json",
                                         body=json.dumps({"status": 409, "code": "folder-destination-exists"}))
                vault.move_folder(folder, target)
                return _json(route, {"path": target})
        if path.startswith("/hubs/"):
            return route.fulfill(status=404)
        if path.startswith("/api/v1/vaults/lab-0001/notes/"):
            from urllib.parse import unquote
            note = unquote(path.removeprefix("/api/v1/vaults/lab-0001/notes/"))
            vault.requests.append((request.method, note, request.headers.get("if-match")))
            if request.method == "GET" and note in vault.notes:
                return route.fulfill(status=200, content_type="application/json", headers={"ETag": '"v1"'},
                                     body=json.dumps({"path": note, "content": vault.notes[note]}))
            if request.method == "PUT" and note in vault.notes:
                vault.notes[note] = json.loads(request.post_data or "{}").get("content", "")
                return route.fulfill(status=204)
            if request.method == "PATCH" and note in vault.notes:
                target = json.loads(request.post_data or "{}")["path"]
                if target in vault.notes:
                    return route.fulfill(status=409, content_type="application/problem+json",
                                         body=json.dumps({"status": 409, "code": "note-destination-exists"}))
                vault.notes[target] = vault.notes.pop(note)
                return _json(route, {"path": target})
            if request.method == "DELETE" and note in vault.notes:
                vault.requests.append(("DELETE-NOTE", note, request.headers.get("if-match")))
                if request.headers.get("if-match") != '"v1"':
                    return route.fulfill(status=409, content_type="application/problem+json",
                                         body=json.dumps({"status": 409, "code": "note-version-conflict"}))
                vault.move_to_trash(note, "note")
                return route.fulfill(status=204)
            return route.fulfill(status=404)
        return route.continue_()

    page.route("**/bff/**", handle)
    page.route("**/api/**", handle)
    page.route("**/hubs/**", handle)
    return vault


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_nodra_brand_keeps_workspace_identity(
    page: Page, base_url: str, mocked_workspace: FakeVault, width: int,
) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 900 if width > 480 else 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    expect(page).to_have_title("Nodra Workspace")
    expect(page.get_by_role("contentinfo")).to_contain_text("Altivy Nodra")
    expect(page.get_by_text("Altivy Notes", exact=False)).to_have_count(0)
    if width <= 480:
        page.get_by_role("button", name="Recolher explorador").click()
        page.get_by_role("button", name="Recolher painel contextual").click()
        expect(page.get_by_role("heading", name="Abra uma nota para começar.")).to_be_in_viewport()
    assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth")
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"nodra-brand-{width}.png"))
    assert not errors


@pytest.mark.parametrize("viewport", [
    {"width": 1440, "height": 900},
    {"width": 390, "height": 844},
], ids=["desktop", "mobile"])
def test_create_and_restore_empty_and_nonempty_folders(page: Page, base_url: str, mocked_workspace: FakeVault,
                                                       viewport: dict[str, int]) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size(viewport)
    page.goto(f"{base_url}/workspace/lab-0001/files")

    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    expect(tree.get_by_role("treeitem", name="projetos")).to_be_visible()

    tree.get_by_role("treeitem", name="projetos").click()
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    expect(tree.get_by_role(
        "treeitem", name="sem-metadados.md (sem classificação, metadados mínimos ausentes)"
    )).to_be_visible()

    page.get_by_role("button", name="Nova pasta").click()
    expect(page.get_by_text("Nova pasta em: projetos")).to_be_visible()
    page.get_by_label("Nome da nova pasta").fill("rascunhos")
    page.get_by_role("button", name="Criar pasta").click()

    created = tree.get_by_role("treeitem", name="rascunhos")
    expect(created).to_be_visible()
    assert "projetos/rascunhos" in mocked_workspace.folders

    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-explorer-created-{viewport['width']}.png"))

    created.click()
    page.get_by_role("button", name="Mover para a lixeira").click()
    page.get_by_role("button", name="Confirmar movimentação").click()
    expect(tree.get_by_role("treeitem", name="rascunhos")).to_have_count(0)
    assert "projetos/rascunhos" not in mocked_workspace.folders
    page.get_by_role("button", name="Lixeira", exact=True).click()
    expect(page.get_by_text("projetos/rascunhos")).to_be_visible()
    page.screenshot(path=str(REPORTS / f"trash-empty-folder-{viewport['width']}.png"))
    restore = page.get_by_role("button", name="Restaurar")
    restore.click()
    expect(page.get_by_label("Novo destino")).to_have_value("projetos/rascunhos")
    page.get_by_role("button", name="Restaurar").click()
    expect(page.get_by_text("projetos/rascunhos")).to_have_count(0)
    page.get_by_role("button", name="Voltar ao explorador", exact=True).click()
    expect(tree.get_by_role("treeitem", name="rascunhos")).to_be_visible()

    project = tree.get_by_role("treeitem", name="projetos")
    project.click()
    page.get_by_role("button", name="Mover para a lixeira").click()
    page.get_by_role("button", name="Confirmar movimentação").click()
    expect(tree.get_by_role("treeitem", name="projetos")).to_have_count(0)
    assert "projetos" not in mocked_workspace.folders
    page.get_by_role("button", name="Lixeira", exact=True).click()
    expect(page.locator(".trash-entry-path").filter(has_text="projetos")).to_be_visible()
    page.screenshot(path=str(REPORTS / f"trash-nonempty-folder-{viewport['width']}.png"))
    page.get_by_role("button", name="Restaurar").click()
    expect(page.get_by_label("Novo destino")).to_have_value("projetos")
    page.get_by_role("button", name="Restaurar").click()
    expect(page.locator(".trash-entry-path").filter(has_text="projetos")).to_have_count(0)
    page.get_by_role("button", name="Voltar ao explorador", exact=True).click()
    expect(tree.get_by_role("treeitem", name="projetos")).to_be_visible()
    assert "projetos/plano.md" in mocked_workspace.notes

    page.screenshot(path=str(REPORTS / f"folder-explorer-conflict-{viewport['width']}.png"))
    body_width = page.evaluate("document.body.scrollWidth")
    assert body_width <= viewport["width"]
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_deleted_note_with_reused_original_path_requires_alternate_restore_destination(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    tree.get_by_role("treeitem", name="projetos").click()
    note = tree.get_by_role("treeitem", name="plano.md")
    note.click()
    page.get_by_role("button", name="Excluir", exact=True).click()

    expect(page.get_by_label("Conteúdo Markdown")).to_have_count(0)
    assert "projetos/plano.md" not in mocked_workspace.notes
    assert ("DELETE-NOTE", "projetos/plano.md", '"v1"') in mocked_workspace.requests
    mocked_workspace.notes["projetos/plano.md"] = "# Nota concorrente\n"
    mocked_workspace.folders.add("arquivo")

    page.get_by_role("button", name="Lixeira", exact=True).click()
    warning = page.locator(".trash-destination-warning")
    expect(warning).to_be_visible()
    expect(warning).to_have_attribute("title", "O caminho original está ocupado. Informe outro destino para restaurar.")
    page.screenshot(path=str(REPORTS / f"trash-note-collision-{width}.png"))
    page.get_by_role("button", name="Restaurar").click()
    destination = page.get_by_label("Novo destino")
    expect(destination).to_have_value("")
    destination.fill("arquivo/plano-restaurado.md")
    page.get_by_role("button", name="Restaurar").click()

    expect(page.locator(".trash-row")).to_have_count(0)
    assert mocked_workspace.notes["projetos/plano.md"] == "# Nota concorrente\n"
    assert mocked_workspace.notes["arquivo/plano-restaurado.md"] == "# Plano sintético\n"
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_folder_creation_appears_pending_and_rolls_back_on_conflict(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    expect(tree.get_by_role("treeitem", name="projetos")).to_be_visible()
    REPORTS.mkdir(parents=True, exist_ok=True)
    observed = []

    def conflict_after_preview(route: Route) -> None:
        pending = tree.get_by_role("treeitem", name="rascunhos (criando)")
        expect(pending).to_be_visible()
        expect(pending).to_be_disabled()
        observed.append(pending.get_attribute("aria-label"))
        page.screenshot(path=str(REPORTS / f"folder-pending-{width}.png"))
        route.fulfill(status=409, content_type="application/problem+json",
                      body=json.dumps({"title": "Conflito", "status": 409}))

    page.route("**/api/v1/vaults/lab-0001/folders/rascunhos", conflict_after_preview)
    page.get_by_role("button", name="Nova pasta").click()
    page.get_by_label("Nome da nova pasta").fill("rascunhos")
    page.get_by_role("button", name="Criar pasta").click()

    expect(page.get_by_role("alert")).to_contain_text("Já existe um arquivo")
    expect(tree.get_by_role("treeitem", name="rascunhos (criando)")).to_have_count(0)
    expect(tree.get_by_role("treeitem", name="projetos")).to_be_visible()
    assert observed == ["rascunhos (criando)"]
    assert "rascunhos" not in mocked_workspace.folders
    page.screenshot(path=str(REPORTS / f"folder-rollback-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_folder_trash_move_hides_until_conflict_then_restores(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    folder = tree.get_by_role("treeitem", name="projetos")
    folder.click()
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    REPORTS.mkdir(parents=True, exist_ok=True)
    observed = []

    def reject_after_preview(route: Route) -> None:
        expect(folder).to_have_count(0)
        expect(page.get_by_text("Movendo pasta para a lixeira...")).to_be_visible()
        observed.append(route.request.method)
        page.screenshot(path=str(REPORTS / f"folder-trash-pending-{width}.png"))
        route.fulfill(status=409, content_type="application/problem+json",
                      body=json.dumps({"title": "Conflito", "status": 409}))

    page.route("**/api/v1/vaults/lab-0001/folders/projetos", reject_after_preview)
    page.get_by_role("button", name="Mover para a lixeira").click()
    page.get_by_role("button", name="Confirmar movimentação").click()

    expect(folder).to_be_visible()
    expect(page.get_by_role("alert")).to_contain_text("Não foi possível mover a pasta")
    expect(page.get_by_text("Movendo pasta para a lixeira...")).to_have_count(0)
    assert observed == ["DELETE"]
    assert "projetos" in mocked_workspace.folders
    page.screenshot(path=str(REPORTS / f"folder-trash-rollback-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_bff_unavailable_state_keeps_side_margins(page: Page, base_url: str, width: int) -> None:
    page.set_viewport_size({"width": width, "height": 800})
    page.route("**/bff/session", lambda route: route.fulfill(status=503))
    page.goto(base_url)

    state = page.get_by_text("Não foi possível consultar o BFF.")
    expect(state).to_be_visible()
    box = page.locator(".state").bounding_box()
    assert box is not None and box["x"] >= 16
    assert box["x"] + box["width"] <= width - 16
    expect(page.get_by_role("button", name="Tentar novamente")).to_be_visible()

    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"bff-unavailable-{width}.png"))


def test_rename_saved_note_moves_it_without_autosaving_to_new_path(
        page: Page, base_url: str, mocked_workspace: FakeVault) -> None:
    page.set_viewport_size({"width": 1440, "height": 900})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    tree.get_by_role("treeitem", name="projetos").click()
    tree.get_by_role("treeitem", name="plano.md").click()

    path_field = page.get_by_label("Caminho da nota")
    expect(path_field).to_have_value("projetos/plano.md")
    path_field.fill("projetos/roadmap")
    path_field.press("Enter")

    expect(path_field).to_have_value("projetos/roadmap.md")
    expect(tree.get_by_role("treeitem", name="roadmap.md")).to_be_visible()
    assert "projetos/roadmap.md" in mocked_workspace.notes
    assert "projetos/plano.md" not in mocked_workspace.notes
    assert ("PATCH", "projetos/plano.md", '"v1"') in mocked_workspace.requests
    assert not any(method == "PUT" and path.startswith("projetos/roadmap") for method, path, _ in mocked_workspace.requests)

    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / "note-renamed-1440.png"))


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_rename_folder_keeps_open_note_and_expansion(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    tree.get_by_role("treeitem", name="projetos").click()
    tree.get_by_role("treeitem", name="plano.md").click()
    expect(page.get_by_label("Caminho da nota")).to_have_value("projetos/plano.md")

    tree.get_by_role("treeitem", name="projetos").focus()
    page.get_by_role("button", name="Renomear ou mover").click()
    target = page.get_by_label("Novo caminho da pasta")
    expect(target).to_have_value("projetos")
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-move-form-{width}.png"))
    target.fill("produto")
    page.get_by_role("button", name="Mover pasta").click()

    expect(tree.get_by_role("treeitem", name="produto")).to_be_visible()
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    expect(tree.get_by_role("treeitem", name="projetos")).to_have_count(0)
    expect(page.get_by_label("Caminho da nota")).to_have_value("produto/plano.md")
    assert "produto/plano.md" in mocked_workspace.notes
    assert ("PATCH-FOLDER", "projetos", "produto") in mocked_workspace.requests

    page.screenshot(path=str(REPORTS / f"folder-moved-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_drag_folder_into_another_folder_keeps_open_note(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    page.get_by_role("button", name="Nova pasta").click()
    page.get_by_label("Nome da nova pasta").fill("arquivo")
    page.get_by_role("button", name="Criar pasta").click()
    target = tree.get_by_role("treeitem", name="arquivo")
    expect(target).to_be_visible()
    tree.get_by_role("treeitem", name="projetos").click()
    tree.get_by_role("treeitem", name="plano.md").click()

    source = tree.get_by_role("treeitem", name="projetos")
    expect(source).to_have_attribute("draggable", "true")
    source.drag_to(target)
    expect(tree.get_by_role("treeitem", name="projetos")).to_have_count(0)
    assert ("PATCH-FOLDER", "projetos", "arquivo/projetos") in mocked_workspace.requests

    target.click()
    expect(tree.get_by_role("treeitem", name="projetos")).to_be_visible()
    expect(page.get_by_label("Caminho da nota")).to_have_value("arquivo/projetos/plano.md")
    assert ("PATCH-FOLDER", "projetos", "arquivo/projetos") in mocked_workspace.requests
    assert "arquivo/projetos/plano.md" in mocked_workspace.notes

    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-dragged-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_drag_saved_note_into_folder_uses_if_match(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    page.get_by_role("button", name="Nova pasta").click()
    page.get_by_label("Nome da nova pasta").fill("arquivo")
    page.get_by_role("button", name="Criar pasta").click()
    target = tree.get_by_role("treeitem", name="arquivo")
    expect(target).to_be_visible()
    tree.get_by_role("treeitem", name="projetos").click()
    note = tree.get_by_role("treeitem", name="plano.md")
    note.click()
    expect(page.get_by_label("Caminho da nota")).to_have_value("projetos/plano.md")

    note.drag_to(target)

    expect(tree.get_by_role("treeitem", name="plano.md")).to_have_count(1)
    expect(target).to_have_attribute("aria-expanded", "true")
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    expect(page.get_by_label("Caminho da nota")).to_have_value("arquivo/plano.md")
    assert ("PATCH", "projetos/plano.md", '"v1"') in mocked_workspace.requests
    assert "arquivo/plano.md" in mocked_workspace.notes
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"note-dragged-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_drag_note_conflict_restores_source_after_optimistic_preview(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    page.get_by_role("button", name="Nova pasta").click()
    page.get_by_label("Nome da nova pasta").fill("arquivo")
    page.get_by_role("button", name="Criar pasta").click()
    target = tree.get_by_role("treeitem", name="arquivo")
    tree.get_by_role("treeitem", name="projetos").click()
    note = tree.get_by_role("treeitem", name="plano.md")

    def reject_move(route: Route) -> None:
        if route.request.method == "PATCH":
            expect(tree.get_by_role("treeitem", name="plano.md (movendo)")).to_be_visible()
            expect(tree.get_by_role("treeitem", name="plano.md (movendo)")).to_be_disabled()
            page.screenshot(path=str(REPORTS / f"note-move-pending-{width}.png"))
            return route.fulfill(status=409, content_type="application/problem+json",
                                 body=json.dumps({"status": 409, "code": "note-destination-exists"}))
        return route.fallback()

    page.route("**/api/v1/vaults/lab-0001/notes/projetos/plano.md", reject_move)
    note.drag_to(target)

    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    expect(tree.get_by_role("treeitem", name="plano.md (movendo)")).to_have_count(0)
    expect(page.get_by_role("alert")).to_contain_text("Já existe um item")
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"note-move-rollback-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_folder_context_menu_supports_pointer_and_keyboard(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    folder = tree.get_by_role("treeitem", name="projetos")
    expect(folder).to_be_visible()

    folder.click(button="right")
    menu = page.get_by_role("menu", name="Ações para projetos")
    expect(menu.get_by_role("menuitem", name="Renomear ou mover")).to_be_visible()
    expect(menu.get_by_role("menuitem", name="Mover para a lixeira")).to_be_visible()
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-context-menu-{width}.png"))

    menu.get_by_role("menuitem", name="Nova nota").press("Shift+Tab")
    expect(menu.get_by_role("menuitem", name="Mover para a lixeira")).to_be_focused()
    menu.get_by_role("menuitem", name="Mover para a lixeira").press("Tab")
    expect(menu.get_by_role("menuitem", name="Nova nota")).to_be_focused()
    menu.get_by_role("menuitem", name="Nova nota").press("Escape")
    expect(menu).to_have_count(0)
    expect(folder).to_be_focused()
    folder.press("Shift+F10")
    expect(menu.get_by_role("menuitem", name="Nova nota")).to_be_focused()
    menu.get_by_role("menuitem", name="Nova nota").click()

    expect(page.get_by_label("Caminho da nota")).to_have_value("projetos/nova-nota.md")
    assert "projetos" in mocked_workspace.folders
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_explorer_denied_root_shows_permission_error(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.route("**/api/v1/vaults/lab-0001/entries", lambda route: route.fulfill(status=403))
    page.goto(f"{base_url}/workspace/lab-0001/files")

    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    expect(tree.get_by_role("treeitem")).to_have_count(0)
    alert = page.get_by_role("alert")
    expect(alert).to_be_visible()
    expect(alert).to_contain_text("Raiz do vault: Você não tem permissão")
    alert_box = alert.bounding_box()
    assert alert_box is not None and alert_box["y"] + alert_box["height"] <= 844
    expect(page.get_by_role("button", name="Tentar novamente")).to_be_visible()
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-denied-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_explorer_offline_child_keeps_root_and_recovers_on_retry(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    failures = 0

    def fail_child_once(route: Route) -> None:
        nonlocal failures
        if "parentPath=projetos" in route.request.url and failures == 0:
            failures += 1
            route.abort("failed")
        else:
            route.fallback()

    page.route("**/api/v1/vaults/lab-0001/entries*", fail_child_once)
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    folder = tree.get_by_role("treeitem", name="projetos")
    folder.click()

    expect(folder).to_be_visible()
    alert = page.get_by_role("alert")
    expect(alert).to_be_visible()
    expect(alert).to_contain_text("projetos: Sem conexão")
    alert_box = alert.bounding_box()
    assert alert_box is not None and alert_box["y"] + alert_box["height"] <= 844
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-offline-{width}.png"))
    page.get_by_role("button", name="Tentar novamente").click()
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    expect(page.get_by_role("alert")).to_have_count(0)
    assert failures == 1
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_external_refresh_keeps_expanded_tree_visible(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    folder = tree.get_by_role("treeitem", name="projetos")
    folder.click()
    original = tree.get_by_role("treeitem", name="plano.md")
    expect(original).to_be_visible()
    mocked_workspace.notes["projetos/externa.md"] = "# Externa sintética\n"
    observed = []
    REPORTS.mkdir(parents=True, exist_ok=True)

    def update_child(route: Route) -> None:
        if "parentPath=projetos" not in route.request.url:
            return route.fallback()
        expect(folder).to_have_attribute("aria-expanded", "true")
        expect(original).to_be_visible()
        observed.append(folder.get_attribute("aria-expanded"))
        page.screenshot(path=str(REPORTS / f"folder-refresh-pending-{width}.png"))
        _json(route, mocked_workspace.children("projetos"))

    page.route("**/api/v1/vaults/lab-0001/entries*", update_child)
    page.get_by_role("button", name="Atualizar arquivos").click()

    expect(tree.get_by_role("treeitem", name="externa.md")).to_be_visible()
    expect(original).to_be_visible()
    expect(folder).to_have_attribute("aria-expanded", "true")
    assert observed == ["true"]
    page.screenshot(path=str(REPORTS / f"folder-refresh-updated-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_expanded_tree_survives_application_reload(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    folder = tree.get_by_role("treeitem", name="projetos")
    folder.click()
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()

    page.reload()

    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    folder = tree.get_by_role("treeitem", name="projetos")
    expect(folder).to_have_attribute("aria-expanded", "true")
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()
    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-expansion-restored-{width}.png"))
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors


@pytest.mark.parametrize("width", [1440, 390], ids=["desktop", "mobile"])
def test_large_root_keeps_20k_entries_virtualized(
        page: Page, base_url: str, mocked_workspace: FakeVault, width: int) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size({"width": width, "height": 844})
    entries = [
        {"path": f"nota-{index:05d}.md", "name": f"nota-{index:05d}.md", "kind": "note", "eTag": "v1"}
        for index in range(20_000)
    ]
    page.route("**/api/v1/vaults/lab-0001/entries", lambda route: _json(route, entries))
    page.goto(f"{base_url}/workspace/lab-0001/files")
    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    first = tree.get_by_role("treeitem", name="nota-00000.md")
    expect(first).to_be_visible()
    first.focus()
    first.press("End")

    last = tree.get_by_role("treeitem", name="nota-19999.md")
    expect(last).to_be_focused()
    assert tree.get_by_role("treeitem").count() < 40

    latencies_ms: list[float] = []
    for index in range(110):
        key = "Home" if index % 2 == 0 else "End"
        started = time.perf_counter()
        page.keyboard.press(key)
        expect(first if key == "Home" else last).to_be_focused()
        if index >= 10:
            latencies_ms.append((time.perf_counter() - started) * 1000)

    ordered = sorted(latencies_ms)
    p95 = ordered[math.ceil(len(ordered) * 0.95) - 1]
    p99 = ordered[math.ceil(len(ordered) * 0.99) - 1]
    assert p95 <= 500, f"p95 da navegação foi {p95:.1f} ms (limite: 500 ms)"
    assert p99 <= 2000, f"p99 da navegação foi {p99:.1f} ms (limite: 2000 ms)"

    REPORTS.mkdir(parents=True, exist_ok=True)
    page.screenshot(path=str(REPORTS / f"folder-20k-{width}.png"))
    (REPORTS / f"folder-20k-performance-{width}.json").write_text(json.dumps({
        "application": "altivy-notes",
        "team": "product",
        "environment": "local-containerized-e2e",
        "version": os.getenv("ALTIVY_VERSION", "local"),
        "commit": os.getenv("GITHUB_SHA", "local"),
        "scenario": "folder-tree-keyboard-20k",
        "pipeline_run_id": os.getenv("GITHUB_RUN_ID", "local"),
        "viewport_width": width,
        "entry_count": len(entries),
        "samples": len(latencies_ms),
        "p95_ms": round(p95, 2),
        "p99_ms": round(p99, 2),
        "thresholds_ms": {"p95": 500, "p99": 2000}
    }, indent=2) + "\n", encoding="utf-8")
    assert page.evaluate("document.body.scrollWidth") <= width
    assert not errors

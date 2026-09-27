"""Jornada do explorador de pastas com a API simulada no browser.

Executa contra o servidor Angular de desenvolvimento (sem BFF, API ou Keycloak),
interceptando as rotas HTTP com dados sintéticos. Não usa credenciais.
"""

import json
import os
from pathlib import Path

import pytest
from playwright.sync_api import Page, Route, expect

pytestmark = [pytest.mark.e2e, pytest.mark.mocked]

REPORTS = Path(os.getenv("ALTIVY_E2E_REPORTS", "/app/reports"))


class FakeVault:
    def __init__(self) -> None:
        self.folders: set[str] = {"projetos"}
        self.notes: dict[str, str] = {"projetos/plano.md": "# Plano sintético\n"}
        self.requests: list[tuple[str, str, str | None]] = []

    def children(self, parent: str) -> list[dict[str, object]]:
        prefix = f"{parent}/" if parent else ""
        entries: list[dict[str, object]] = []
        for folder in sorted(self.folders):
            if folder.startswith(prefix) and "/" not in folder[len(prefix):]:
                entries.append({"path": folder, "name": folder[len(prefix):], "kind": "folder", "eTag": None})
        for note in sorted(self.notes):
            if note.startswith(prefix) and "/" not in note[len(prefix):]:
                entries.append({"path": note, "name": note[len(prefix):], "kind": "note", "eTag": "v1"})
        return entries

    def is_empty(self, path: str) -> bool:
        prefix = f"{path}/"
        return not any(item.startswith(prefix) for item in [*self.folders, *self.notes])

    def move_folder(self, source: str, target: str) -> None:
        def remap(item: str) -> str:
            return target + item[len(source):] if item == source or item.startswith(f"{source}/") else item
        self.folders = {remap(folder) for folder in self.folders}
        self.notes = {remap(note): content for note, content in self.notes.items()}


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
                if folder in vault.folders and not vault.is_empty(folder):
                    return route.fulfill(status=409, content_type="application/problem+json",
                                         body=json.dumps({"title": "Conflito", "status": 409}))
                vault.folders.discard(folder)
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
            return route.fulfill(status=404)
        return route.continue_()

    page.route("**/bff/**", handle)
    page.route("**/api/**", handle)
    page.route("**/hubs/**", handle)
    return vault


@pytest.mark.parametrize("viewport", [
    {"width": 1440, "height": 900},
    {"width": 390, "height": 844},
], ids=["desktop", "mobile"])
def test_create_and_delete_empty_folder(page: Page, base_url: str, mocked_workspace: FakeVault,
                                        viewport: dict[str, int]) -> None:
    errors: list[str] = []
    page.on("pageerror", lambda error: errors.append(str(error)))
    page.set_viewport_size(viewport)
    page.goto(f"{base_url}/workspace/lab-0001/files")

    tree = page.get_by_role("tree", name="Pastas e notas do vault")
    expect(tree.get_by_role("treeitem", name="projetos")).to_be_visible()

    tree.get_by_role("treeitem", name="projetos").click()
    expect(tree.get_by_role("treeitem", name="plano.md")).to_be_visible()

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
    page.get_by_role("button", name="Excluir pasta vazia").click()
    page.get_by_role("button", name="Confirmar exclusão").click()
    expect(tree.get_by_role("treeitem", name="rascunhos")).to_have_count(0)
    assert "projetos/rascunhos" not in mocked_workspace.folders

    tree.get_by_role("treeitem", name="projetos").click()
    tree.get_by_role("treeitem", name="projetos").click()
    page.get_by_role("button", name="Excluir pasta vazia").click()
    page.get_by_role("button", name="Confirmar exclusão").click()
    expect(page.get_by_role("alert").first).to_contain_text("não pode ser excluída")
    expect(page.get_by_role("button", name="Tentar novamente")).to_have_count(0)
    assert "projetos" in mocked_workspace.folders

    page.screenshot(path=str(REPORTS / f"folder-explorer-conflict-{viewport['width']}.png"))
    body_width = page.evaluate("document.body.scrollWidth")
    assert body_width <= viewport["width"]
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

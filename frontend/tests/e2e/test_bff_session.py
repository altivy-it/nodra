import pytest
from playwright.sync_api import Page, expect
from urllib.parse import urlsplit
from uuid import uuid4


@pytest.mark.e2e
def test_anonymous_browser_does_not_receive_tokens(page: Page, base_url: str) -> None:
    page.goto(base_url, wait_until="networkidle")

    expect(page.get_by_role("heading", name="Seu conhecimento, em um só lugar.")).to_be_visible()
    login_path = f"{urlsplit(base_url).path.rstrip('/')}/bff/login"
    expect(page.get_by_role("link", name="Entrar no Altivy Notes")).to_have_attribute(
        "href", login_path)
    assert page.evaluate("Object.keys(localStorage).length") == 0
    assert page.evaluate("Object.keys(sessionStorage).length") == 0


@pytest.mark.e2e
def test_anonymous_cannot_negotiate_signalr_hub(page: Page, base_url: str) -> None:
    page.goto(base_url, wait_until="networkidle")
    result = page.evaluate("""async (url) => {
        const response = await fetch(url, { method: 'POST' });
        return { status: response.status, redirected: response.redirected };
    }""", f"{base_url}/hubs/vaults/negotiate?negotiateVersion=1")

    assert result["status"] == 401 or result["redirected"]


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_can_access_authorized_vault(authenticated_page: Page, base_url: str) -> None:
    response = authenticated_page.goto(f"{base_url}/api/v1/vaults/lab-0001")

    assert response is not None
    assert response.status == 200
    data = response.json()
    assert data["id"] == "lab-0001"
    assert data["scope"] == "vault:read"


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_can_create_and_autosave_note_in_write_vault(
    authenticated_page: Page,
    base_url: str,
    pytestconfig: pytest.Config,
) -> None:
    vault_id = pytestconfig.getoption("write_vault_id")
    if not vault_id:
        pytest.skip("Defina --write-vault-id ou ALTIVY_E2E_WRITE_VAULT_ID para usar um vault isolado.")

    catalog = authenticated_page.request.get(f"{base_url}/api/v1/vaults")
    assert catalog.status == 200
    assert vault_id in {item["id"] for item in catalog.json()["items"]}

    authenticated_page.goto(base_url, wait_until="networkidle")
    selector = authenticated_page.get_by_role("combobox", name="Vault ativo")
    expect(selector).to_be_visible()
    selector.select_option(vault_id)
    expect(authenticated_page).to_have_url(f"{base_url}/workspace/{vault_id}/files")
    expect(selector).to_have_value(vault_id)

    note_path = f"autosave-{uuid4().hex}.md"
    note_url = f"{base_url}/api/v1/vaults/{vault_id}/notes/{note_path}"
    initial_content = "# Autosave E2E\nPrimeira gravação pelo navegador."
    updated_content = "# Autosave E2E\nAtualização com ETag pelo navegador."
    authenticated_page.get_by_role("button", name="Nova nota").click()
    authenticated_page.get_by_role("textbox", name="Caminho da nota").fill(note_path)
    content = authenticated_page.get_by_role("textbox", name="Conteúdo Markdown")

    with authenticated_page.expect_response(
        lambda result: result.url == note_url and result.request.method == "GET"
    ) as read_response:
        with authenticated_page.expect_response(
            lambda result: result.url == note_url and result.request.method == "POST"
        ) as create_response:
            content.fill(initial_content)

    assert create_response.value.status == 201
    assert read_response.value.status == 200
    assert read_response.value.headers.get("etag")
    assert read_response.value.json()["content"] == initial_content

    authenticated_page.wait_for_load_state("networkidle")
    with authenticated_page.expect_response(
        lambda result: result.url == note_url and result.request.method == "PUT"
    ) as update_response:
        content.fill(updated_content)

    assert update_response.value.status == 204
    assert update_response.value.request.headers.get("if-match")

    persisted = authenticated_page.request.get(note_url)
    assert persisted.status == 200
    assert persisted.headers.get("etag")
    assert persisted.json()["content"] == updated_content


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_can_read_persisted_note_after_container_recreation(
    authenticated_page: Page,
    base_url: str,
    pytestconfig: pytest.Config,
) -> None:
    vault_id = pytestconfig.getoption("write_vault_id")
    note_path = pytestconfig.getoption("persisted_note_path")
    if not vault_id or not note_path:
        pytest.skip("Defina os IDs do vault de escrita e da nota persistida para validar recriação.")

    response = authenticated_page.request.get(
        f"{base_url}/api/v1/vaults/{vault_id}/notes/{note_path}"
    )

    assert response.status == 200
    assert response.headers.get("etag")
    assert response.json()["content"] == "# Autosave E2E\nAtualização com ETag pelo navegador."


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_can_negotiate_signalr_hub(authenticated_page: Page, base_url: str) -> None:
    result = authenticated_page.evaluate("""async (url) => {
        const response = await fetch(url, { method: 'POST', credentials: 'include' });
        return { status: response.status, body: await response.json() };
    }""", f"{base_url}/hubs/vaults/negotiate?negotiateVersion=1")

    assert result["status"] == 200
    data = result["body"]
    assert "connectionId" in data or "connectionToken" in data
    assert "availableTransports" in data


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_browser_can_log_out(authenticated_page: Page, base_url: str) -> None:
    authenticated_page.get_by_role("button", name="Encerrar sessão").click()

    expect(authenticated_page.get_by_role("heading", name="Seu conhecimento, em um só lugar.")).to_be_visible()
    login_path = f"{urlsplit(base_url).path.rstrip('/')}/bff/login"
    expect(authenticated_page.get_by_role("link", name="Entrar no Altivy Notes")).to_have_attribute(
        "href", login_path)
    assert not authenticated_page.evaluate("""() => {
        const entries = [...Object.entries(localStorage), ...Object.entries(sessionStorage)];
        const jwt = /eyJ[A-Za-z0-9_-]+\\.eyJ[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+/;
        return entries.some(([key, value]) => /token/i.test(key) || jwt.test(value));
    }""")
    assert authenticated_page.evaluate("Object.keys(sessionStorage).length") == 0


@pytest.mark.e2e
@pytest.mark.authenticated
def test_denied_vault_is_not_exposed_through_bff_proxy(
    authenticated_page: Page,
    base_url: str,
    pytestconfig: pytest.Config,
) -> None:
    vault_id = pytestconfig.getoption("denied_vault_id")
    if not vault_id:
        pytest.skip("Defina --denied-vault-id ou ALTIVY_E2E_DENIED_VAULT_ID.")

    response = authenticated_page.goto(f"{base_url}/api/v1/vaults/{vault_id}")

    assert response is not None
    assert response.status == 403
    assert response.headers["content-type"].startswith("application/problem+json")


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_catalog_contains_only_authorized_vaults(
    authenticated_page: Page,
    base_url: str,
    pytestconfig: pytest.Config,
) -> None:
    response = authenticated_page.request.get(f"{base_url}/api/v1/vaults")

    assert response.status == 200
    data = response.json()
    expected_vaults = {"lab-0001", "lab-0002"}
    write_vault_id = pytestconfig.getoption("write_vault_id")
    if write_vault_id:
        expected_vaults.add(write_vault_id)
    assert {item["id"] for item in data["items"]} == expected_vaults
    assert all(set(item) == {"id", "name"} for item in data["items"])


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_can_switch_vault_without_full_reload(
    authenticated_page: Page,
    base_url: str,
) -> None:
    response = authenticated_page.request.get(f"{base_url}/api/v1/vaults")
    assert response.status == 200
    vaults = response.json()["items"]
    if len(vaults) < 2:
        pytest.skip("O usuário de laboratório precisa de acesso a dois vaults.")

    selector = authenticated_page.get_by_role("combobox", name="Vault ativo")
    expect(selector).to_be_visible()
    module = authenticated_page.url.rsplit("/", 1)[-1]
    target_id = next(vault["id"] for vault in vaults if vault["id"] != selector.input_value())
    before_url = authenticated_page.url

    selector.select_option(target_id)

    expect(authenticated_page).to_have_url(f"{base_url}/workspace/{target_id}/{module}")
    assert authenticated_page.url != before_url
    expect(authenticated_page.get_by_role("combobox", name="Vault ativo")).to_have_value(target_id)


@pytest.mark.e2e
@pytest.mark.authenticated
def test_authenticated_reload_keeps_authorized_vault_and_module(
    authenticated_page: Page,
    base_url: str,
) -> None:
    selector = authenticated_page.get_by_role("combobox", name="Vault ativo")
    vault_id = selector.input_value()
    module = authenticated_page.url.rsplit("/", 1)[-1]

    authenticated_page.reload(wait_until="networkidle")

    expect(authenticated_page).to_have_url(f"{base_url}/workspace/{vault_id}/{module}")
    expect(authenticated_page.get_by_role("combobox", name="Vault ativo")).to_have_value(vault_id)
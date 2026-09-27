import os
import re

import pytest
from playwright.sync_api import Page, TimeoutError, expect


def pytest_addoption(parser: pytest.Parser) -> None:
    parser.addoption(
        "--denied-vault-id",
        action="store",
        default=os.getenv("ALTIVY_E2E_DENIED_VAULT_ID"),
        help="Vault que o usuário de laboratório não pode ler, ou ALTIVY_E2E_DENIED_VAULT_ID.",
    )


def pytest_configure(config: pytest.Config) -> None:
    config.addinivalue_line("markers", "e2e: jornada executada em browser contra um ambiente Altivy acessível")
    config.addinivalue_line(
        "markers",
        "authenticated: jornada que exige credenciais de laboratório fornecidas em runtime",
    )
    config.addinivalue_line(
        "markers",
        "mocked: jornada com API simulada no browser, sem BFF, API ou Keycloak",
    )

    if not config.option.base_url:
        config.option.base_url = os.getenv("ALTIVY_E2E_BASE_URL")


@pytest.fixture(scope="session")
def base_url(pytestconfig: pytest.Config) -> str:
    value = pytestconfig.getoption("base_url")
    if not value:
        pytest.fail("Informe --base-url ou ALTIVY_E2E_BASE_URL; consulte frontend/tests/e2e/README.md.")

    return value.rstrip("/")


@pytest.fixture(scope="session")
def browser_context_args(browser_context_args: dict[str, object]) -> dict[str, object]:
    return {**browser_context_args, "ignore_https_errors": True}


@pytest.fixture(scope="session")
def credentials() -> tuple[str, str]:
    username = os.getenv("ALTIVY_E2E_USERNAME")
    password = os.getenv("ALTIVY_E2E_PASSWORD")
    if not username or not password:
        pytest.skip("Defina ALTIVY_E2E_USERNAME e ALTIVY_E2E_PASSWORD para jornadas autenticadas.")

    return username, password


@pytest.fixture
def authenticated_page(page: Page, base_url: str, credentials: tuple[str, str]) -> Page:
    username, password = credentials
    page.goto(base_url, wait_until="networkidle")
    page.get_by_role("link", name="Entrar no Altivy Notes").click()
    page.locator("#username").fill(username)
    page.locator("#password").fill(password)
    page.locator("#kc-login").click()
    try:
        page.wait_for_url(re.compile(rf"^{re.escape(base_url)}(?:/.*)?$"), timeout=10_000)
    except TimeoutError as error:
        feedback = page.locator(".alert-error, #input-error, .pf-v5-c-alert__title").all_inner_texts()
        message = " ".join(text.strip() for text in feedback if text.strip())
        if message:
            raise AssertionError(f"O Keycloak rejeitou o login: {message}") from error

        raise AssertionError("O Keycloak não retornou ao callback OIDC após 10 segundos.") from error

    expect(page.get_by_role("button", name="Encerrar sessão")).to_be_visible()

    return page
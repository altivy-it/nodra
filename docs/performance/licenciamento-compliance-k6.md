# Análise de Licenciamento e Compliance - k6

**Documento:** Complemento do ADR 2026-0001 - Padronizacao de testes de carga com k6  
**Data:** 30/07/2026  
**Status:** accepted  

---

## 1. Objetivo

Este documento define os aspectos de licenciamento, compliance e governança para adoção do k6 como ferramenta corporativa de testes de carga, performance e capacidade de APIs.

---

## 2. Licenciamento

O k6 open source é licenciado sob **GNU Affero General Public License v3.0 (AGPLv3)**. A licença permite uso, modificação, distribuição e uso privado, porém impõe obrigações relevantes quando há distribuição de versões modificadas ou disponibilização de uma versão modificada como serviço de rede.

### Implicações práticas

Para o uso corporativo padrão, onde o k6 é executado como ferramenta interna de teste, sem modificação do código-fonte do k6 e sem redistribuição de uma versão alterada, o risco de compliance é considerado **baixo a moderado**.

### Regras recomendadas

- Utilizar preferencialmente a distribuição oficial do k6 sem alteração do código-fonte.
- Não modificar, recompilar ou redistribuir binários customizados do k6 sem revisão jurídica e de open source compliance.
- Manter arquivo de atribuição/licenças quando o k6 for incluído em imagens Docker internas.
- Avaliar separadamente extensões `xk6`, pois podem possuir licenças próprias.
- Registrar a ferramenta no inventário corporativo de open source.
- Revisar o uso com o time jurídico caso a organização deseje oferecer uma plataforma interna baseada em k6 para múltiplos consumidores via rede.

---

## 3. Dependências relacionadas

A stack recomendada pode envolver:

- k6
- Grafana
- Prometheus
- Docker
- Azure DevOps
- GitHub Actions

O Grafana também possui componentes licenciados sob AGPLv3 em seu core open source. O Prometheus é amplamente conhecido por sua adoção no ecossistema cloud native e uso em monitoramento e alertas.

---

## 4. Classificação de risco

| Item | Risco | Comentário |
|---|---:|---|
| Uso interno do binário oficial do k6 | Baixo | Uso como ferramenta de execução de testes. |
| Execução em pipeline CI/CD | Baixo | Ferramenta é chamada como processo externo. |
| Scripts de teste escritos pela empresa | Baixo | Scripts são propriedade da organização. |
| Imagem Docker interna com k6 oficial | Moderado | Manter notices e rastreabilidade da origem. |
| Modificação do código-fonte do k6 | Alto | Pode disparar obrigações AGPLv3. |
| Distribuição de k6 modificado | Alto | Requer revisão jurídica. |
| Plataforma SaaS baseada em k6 modificado | Alto | Requer revisão jurídica e avaliação de copyleft de rede. |

---

## 5. Controles de compliance

- Aprovação da ferramenta no processo de open source review.
- Registro da versão utilizada.
- Registro de origem do binário ou imagem Docker.
- Revisão de imagens base usadas nos pipelines.
- Armazenamento dos scripts de teste em repositório Git corporativo.
- Separação clara entre código da aplicação e ferramenta k6.
- Proibição de alterações no código-fonte do k6 sem aprovação formal.

---

## 6. Decisão recomendada

A adoção do k6 é permitida para uso interno em testes de carga e performance, desde que a organização utilize o binário oficial ou imagem oficial, mantenha rastreabilidade de versões e submeta customizações ou redistribuições à revisão de licenciamento.

## 7. Relacao com a politica do repositório

Este complemento deve ser lido em conjunto com as regras de dependencias e licenciamento do monorepo. Como k6 e AGPLv3, a adocao depende de excecao formal aprovada pelas areas responsaveis (arquitetura e juridico/compliance), com registro explicito do escopo permitido de uso.

## 8. Fontes de referencia

- k6 GitHub License: AGPLv3.
- Grafana Labs licensing page.
- Prometheus FAQ / documentação oficial.

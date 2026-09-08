# Política de Segurança

> **Canal ainda não operacional:** o GitHub Private Vulnerability Reporting deve ser habilitado antes da divulgação pública desta política. Até a confirmação, não há um canal oficial de reporte; não publique detalhes de vulnerabilidades em issues.

## Versões suportadas

Antes da primeira versão estável, somente a linha de desenvolvimento atual recebe correções. Após `1.0.0`, a matriz de versões suportadas será publicada aqui a cada release.

## Reportar uma vulnerabilidade

Não abra issue pública para vulnerabilidades. Use **GitHub Private Vulnerability Reporting** no repositório.

Inclua, sem dados reais ou secrets:

- componente e versão afetados;
- pré-condições e impacto;
- passos mínimos para reprodução;
- prova de conceito sanitizada;
- mitigação conhecida, se houver.

Não acesse dados de terceiros, não mantenha persistência e não execute testes destrutivos em ambientes que não sejam seus.

## Resposta esperada

- confirmação inicial em até 3 dias úteis;
- triagem e severidade em até 7 dias úteis;
- comunicação de correção coordenada conforme impacto e complexidade;
- crédito ao pesquisador quando solicitado e permitido.

Prazos são objetivos operacionais, não garantia contratual.

## Escopo prioritário

- perda ou exposição de notas e anexos;
- bypass de autenticação/autorização;
- escape do vault por path traversal, symlink ou reparse point;
- XSS, CSRF, SSRF e conteúdo ativo;
- upload malicioso e execução de código;
- secrets em imagens, logs, artifacts ou configuração;
- vulnerabilidades de supply chain e containers.

## Divulgação e correção

Correções devem incluir teste de regressão, análise de variantes, atualização de dependências/SBOM quando aplicável e orientação de mitigação. A publicação ocorre depois de versão corrigida e tempo razoável para atualização, salvo risco ativo que exija comunicação imediata.
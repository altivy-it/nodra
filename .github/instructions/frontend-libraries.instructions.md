---
name: "Bibliotecas Frontend"
description: "Use ao criar ou alterar bibliotecas Angular compartilhadas, design system, componentes base, utilitários ou data access em frontend/libs/frontend."
applyTo: "frontend/libs/frontend/**"
---

# Bibliotecas frontend

- Cada biblioteca tem responsabilidade única, API pública explícita e nenhuma importação por caminho interno.
- Não importe `frontend/web` nem dependa de configuração específica do host.
- Separe bibliotecas de UI, data access, editor, graph e utilitários quando houver consumidores reais.
- Componentes do design system não contêm regra de negócio.
- Bibliotecas de UI compõem componentes PO UI; não reimplementam controle já coberto pelo portfólio, exceto o editor Markdown e o grafo de notas.
- Tokens de design são CSS custom properties versionadas; não espalhe valores literais.
- Toda API pública deve ser tipada, documentada quando não óbvia e coberta por testes.
- Evite barrel files profundos e ciclos; exporte somente o contrato suportado.
- Dependências peer são usadas quando o host deve controlar a versão; justifique novas dependências.
- Mudança incompatível exige migração coordenada dos consumidores e registro no changelog interno.
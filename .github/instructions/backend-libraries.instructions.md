---
name: "Bibliotecas Backend"
description: "Use ao criar ou alterar projetos .NET compartilhados de domínio, aplicação e infraestrutura em libs/backend."
applyTo: "libs/backend/**"
---

# Bibliotecas backend

- Um projeto representa uma responsabilidade arquitetural estável, não uma pasta arbitrária.
- `Domain` só depende da BCL e de pacotes de domínio inevitáveis.
- `Application` depende de `Domain`; define casos de uso e portas, não adapters.
- `Infrastructure` depende de `Application` e implementa adapters.
- Evite projeto `Common` genérico. Nomeie a capacidade real ou mantenha o código no módulo proprietário.
- Não compartilhe entidades persistidas ou modelos HTTP entre módulos por conveniência.
- Exponha APIs públicas mínimas; use `internal` por padrão dentro do assembly.
- Adicione testes de arquitetura para regras de referência e visibilidade.
- Para testes de arquitetura em .NET, use preferencialmente `TngTech.ArchUnitNET`.
- Mudança em abstração compartilhada exige avaliar todos os consumidores e validar a solução inteira.
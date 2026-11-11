# Avaliacao de Bug: permissoes de build da imagem web

- **Slug**: web-container-permissions
- **Criada em**: 2026-11-11
- **Origem**: Qualificacao local autorizada de T031 na feature 010-renomeacao-nodra.
- **Veredito**: valid
- **Severidade**: high

## Relato

O build do Dockerfile web falha em npm ci com EACCES ao criar /src/node_modules.
A imagem Angular executa como usuario sem permissao de escrita no WORKDIR
criado pela construcao da imagem.

## Sintoma e Reproducao

Executar podman build --format docker --pull=never --tag
nodra-web:qualification-010 --file frontend/web/Dockerfile . na raiz.
O passo RUN npm ci falha com exit status 243, sem gerar a imagem web.

## Caminhos de Codigo Suspeitos

- frontend/web/Dockerfile: stage build herda o usuario da imagem Angular,
  cria WORKDIR /src e executa npm ci sem preparar permissoes.
- O runtime executa apt-get/chown durante a preparacao e ja declara USER nginx
  antes de expor a aplicacao; essa fronteira deve permanecer explicita.

## Hipotese de Causa Raiz

Usuario herdado da imagem de build nao possui /src. Confianca high: EACCES
reproduzido no passo npm ci, antes da compilacao Angular.

## Correcao Proposta

Declarar USER root nas etapas de preparacao de build e runtime, mantendo
USER nginx como usuario final. Nao alterar dependencias, layout, permissoes
de dados do host ou configuracao de identidade.

**Arquivos Provaveis de Alteracao**: frontend/web/Dockerfile.

**Testes a Adicionar ou Atualizar**: repetir o mesmo build completo; verificar
que o usuario final e nginx, que os assets Nodra existem e que o health check
HTTP funciona na propria imagem nginx, sem acessar Angular diretamente.

## Riscos e Consideracoes

Nao publicar imagens, executar deploy ou acessar secrets reais. O privilegio
de preparacao nao deve chegar ao processo nginx final. Reutiliza imagens e
versoes ja aprovadas, sem nova dependencia.

## Perguntas em Aberto

Nenhuma para a correcao minima delimitada no arquivo proprietario.
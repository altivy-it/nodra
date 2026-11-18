# Avisos de Terceiros

Os componentes gerenciados de CI/CD vêm do catálogo Altivy.Groundwork 0.11.3,
tag v0.11.3, commit c848afb4fa0b43cb9227abd2c347ab4ecccd6296, do repositório
altivy-it/altivy-groundwork. Não foram copiados do repositório Dokpod AGPL.
O aviso legal obrigatório da origem é preservado abaixo, sem reatribuição de
autoria. O código próprio do Nodra continua sob sua licença MIT.

## Altivy.Groundwork - MIT

```text
MIT License

Copyright (c) 2026 Altivy (Lincoln Zocateli)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Ferramentas de Build

- Scriban 7.5.3: BSD-2-Clause, verificada no nuspec oficial.
- System.CommandLine 2.0.14: MIT, verificada no nuspec oficial.
- Trivy 0.72.0: Apache-2.0, verificada no LICENSE da tag oficial; usado apenas
  como ferramenta externa em container, sem redistribuir seu binário no produto.
- YamlDotNet 18.1.0: dependência MIT já existente no backend, reutilizada para
  os testes locais estruturados de YAML, sem pacote adicional.
# Changelog

## [0.2.0](https://github.com/altivy-it/nodra/compare/v0.1.0...v0.2.0) (2026-11-24)


### ⚠ BREAKING CHANGES

* a URL pública muda de /notes para /nodra. Atualize o proxy reverso para encaminhar /nodra com X-Forwarded-Prefix /nodra, defina ALTIVY_PUBLIC_ORIGIN e WEB_BASE_HREF com /nodra e reexecute configure-keycloak.ps1 com -BffBaseUrl https://<host>/nodra. Um redirect 301 de /notes para /nodra no proxy preserva links antigos.
* **repo:** use Nodra.slnx, os projetos e assemblies Nodra.*, nodra-ops.sh e as imagens nodra-*. Preserve os dados e configurações legados conforme deploy/self-hosted/README.md.

### Adicionado

* add Dockerfile for migrations and update deployment documentation ([7670f9f](https://github.com/altivy-it/nodra/commit/7670f9fb7100eb1e3045b8da4a0a269d2279b9cb))
* Add guidelines for table partitioning and retention strategies in architecture and database instructions ([9496d40](https://github.com/altivy-it/nodra/commit/9496d40a62139e9b78b9a3d72608edbfa3661129))
* Add issue templates for bug reports, feature requests, technical tasks, and architectural spikes ([c2df8a4](https://github.com/altivy-it/nodra/commit/c2df8a4650f7ee050161e534df3ca1df3819f72a))
* add master plan document for Altivy solution and update related links ([818c719](https://github.com/altivy-it/nodra/commit/818c719d7699cdd4f4e0e704acdd28e97c10d893))
* add runbook for self-hosted recovery procedures ([79e372c](https://github.com/altivy-it/nodra/commit/79e372cb81db1b0a5fdeb223ae14144fd73b3fd6))
* add script for generating local production certificates ([8e1f41d](https://github.com/altivy-it/nodra/commit/8e1f41df0cc60eb77d4f759f6fffded064b97f13))
* add script for generating local production certificates ([c6e9c62](https://github.com/altivy-it/nodra/commit/c6e9c6205c1ea9258fcb5f7dc4ff795fa03346a4))
* add script for generating local production certificates ([79e372c](https://github.com/altivy-it/nodra/commit/79e372cb81db1b0a5fdeb223ae14144fd73b3fd6))
* create script to start self-hosted Altivy Notes in production ([8e1f41d](https://github.com/altivy-it/nodra/commit/8e1f41df0cc60eb77d4f759f6fffded064b97f13))
* create script to start self-hosted Altivy Notes in production ([c6e9c62](https://github.com/altivy-it/nodra/commit/c6e9c6205c1ea9258fcb5f7dc4ff795fa03346a4))
* create script to start self-hosted Altivy Notes in production ([79e372c](https://github.com/altivy-it/nodra/commit/79e372cb81db1b0a5fdeb223ae14144fd73b3fd6))
* Deploy self-hosted ([#13](https://github.com/altivy-it/nodra/issues/13)) ([c6e9c62](https://github.com/altivy-it/nodra/commit/c6e9c6205c1ea9258fcb5f7dc4ff795fa03346a4))
* enhance E2E tests for note persistence ([8e1f41d](https://github.com/altivy-it/nodra/commit/8e1f41df0cc60eb77d4f759f6fffded064b97f13))
* enhance E2E tests for note persistence ([c6e9c62](https://github.com/altivy-it/nodra/commit/c6e9c6205c1ea9258fcb5f7dc4ff795fa03346a4))
* enhance E2E tests for note persistence ([79e372c](https://github.com/altivy-it/nodra/commit/79e372cb81db1b0a5fdeb223ae14144fd73b3fd6))
* enhance folder explorer with trash management and expanded paths ([b160fb8](https://github.com/altivy-it/nodra/commit/b160fb853db4ebd3733d7753deb3e98bba8f2adf))
* Enhance frontend guidelines and introduce PO UI component usage instructions ([e48d431](https://github.com/altivy-it/nodra/commit/e48d431814129a38c964d5459203eb087cf2c2ae))
* Enhance self-hosted setup and recovery scripts ([583d18a](https://github.com/altivy-it/nodra/commit/583d18ac5d840330826caff99c636c06179d0a01))
* implement folder explorer service and component ([fe6ee38](https://github.com/altivy-it/nodra/commit/fe6ee38fe8903cfba3ec99b3c32b18928f90e754))
* Implement migration script loader and update migration handling for PostgreSQL ([f87da32](https://github.com/altivy-it/nodra/commit/f87da32c7547a11c75eed3e51610fe4506a31ad0))
* Implement Note Indexing and Projection System ([d8fb405](https://github.com/altivy-it/nodra/commit/d8fb405a6e54c52edf921efa45132aba22a766e9))
* Implement temporal partitioning for note index jobs and vault provisioning operations ([7f472d9](https://github.com/altivy-it/nodra/commit/7f472d9b3a8cca125597d37c135275ed8eb19a4b))
* implement trash retention settings and enhance empty trash functionality ([5a89eec](https://github.com/altivy-it/nodra/commit/5a89eec05c1067cdb5ba27ff912ad7cbca5a9e19))
* Refactor E2E and Keycloak configurations to use shared nginx-proxy and update documentation ([2d960ee](https://github.com/altivy-it/nodra/commit/2d960ee6dfabce62762d367453cd6b469eacf495))
* **repo:** adiciona indicadores de metadados ao explorador ([69431f0](https://github.com/altivy-it/nodra/commit/69431f03b72703983c3e2ad0df49cf1a9b5385c1))
* **repo:** migra auxiliares para skills ([dd588ea](https://github.com/altivy-it/nodra/commit/dd588ea8b0e3d02ae7ec0a7c5ab1b9fc67e4d4f5))
* **repo:** migra auxiliares para skills ([#26](https://github.com/altivy-it/nodra/issues/26)) ([2222662](https://github.com/altivy-it/nodra/commit/22226620304cd46ea31ceec8fedbd2c6b97caab7))
* Update E2E and NGINX configurations to use user secrets for certificate management ([651966e](https://github.com/altivy-it/nodra/commit/651966ef0c529e8f1664b9044bd40c1767fc36b1))
* Update issue template for plan slice and enhance Copilot instructions ([86c099f](https://github.com/altivy-it/nodra/commit/86c099f0db0568aec7088632083ea76c3b325fe6))
* Update Keycloak integration to use provisioner client credentials and adjust related scripts ([48baa25](https://github.com/altivy-it/nodra/commit/48baa25e278cb6d9c0f7e072131e538d338aa2cb))
* Update status to blocked and add waiting notes for Teams integration plans ([5b6ab3a](https://github.com/altivy-it/nodra/commit/5b6ab3a8ffd643fdc0bf4a530b9b681e27de43c0))
* vault de exemplo somente leitura e publicação em /nodra ([#35](https://github.com/altivy-it/nodra/issues/35)) ([bf8c19c](https://github.com/altivy-it/nodra/commit/bf8c19ce4b484e71673a85afd06f0083416410c5))
* **web:** aplique identidade visual Nodra e laboratório local Podman ([#34](https://github.com/altivy-it/nodra/issues/34)) ([886d885](https://github.com/altivy-it/nodra/commit/886d88588c3109a3cc9ad1a6f3d22460b12a93c3))


### Corrigido

* **backend:** alinhe teste e snapshot EF ao schema atual ([fe9530e](https://github.com/altivy-it/nodra/commit/fe9530e89f5dbbcaf66abf4f7e3c45a14523e975))
* **deploy:** force LF nos scripts Bash da VPS ([0a1bb55](https://github.com/altivy-it/nodra/commit/0a1bb5511725a978553fb545dd6f3ed595fab12a))
* improve session logout functionality in app ([8e1f41d](https://github.com/altivy-it/nodra/commit/8e1f41df0cc60eb77d4f759f6fffded064b97f13))
* improve session logout functionality in app ([c6e9c62](https://github.com/altivy-it/nodra/commit/c6e9c6205c1ea9258fcb5f7dc4ff795fa03346a4))
* improve session logout functionality in app ([79e372c](https://github.com/altivy-it/nodra/commit/79e372cb81db1b0a5fdeb223ae14144fd73b3fd6))
* **performance:** update p95 and p99 metrics for 20k performance tests; add new test cases for NoteDocument and LocalRedirect ([9630d8b](https://github.com/altivy-it/nodra/commit/9630d8bf45d32e4f0875ed987ef28f1f606631dd))
* **repo:** corrige execução sob o prefixo /notes ([800cdec](https://github.com/altivy-it/nodra/commit/800cdecfae80f0cb78cc09d01b3d0848029ad58f))
* **repo:** corrige logout autenticado sob /notes ([9247696](https://github.com/altivy-it/nodra/commit/9247696fe64033141baf233d916483b19c68d1fa))
* **tests:** add newline at end of NoteProjectionBuilderTests.cs ([d3c8997](https://github.com/altivy-it/nodra/commit/d3c8997813cc0d923d572c3a9a4ed3f9df42e438))


### Refatoração

* **repo:** renomeie o produto para Nodra ([f66f39d](https://github.com/altivy-it/nodra/commit/f66f39d4986c0aa1c53ee31023c5904577997773))

## Changelog

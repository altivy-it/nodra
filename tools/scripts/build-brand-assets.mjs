import { createHash } from "node:crypto";
import { mkdir, readFile, rm, writeFile } from "node:fs/promises";
import { createRequire } from "node:module";
import path from "node:path";

const args = process.argv.slice(2);

if (args.includes("--help")) {
  console.log(`Produz SVGs otimizados, PNGs e checksums da marca Altivy.

Dependências: Node.js 24, SVGO 3.3.2 e sharp 0.33.5.
Uso: node tools/scripts/build-brand-assets.mjs <repository-root>
Exemplo: node tools/scripts/build-brand-assets.mjs /workspace
Saída: docs/brand/assets/dist
Documentação: docs/brand/guide.md`);
  process.exit(0);
}

if (args.length !== 1) {
  console.error("Informe <repository-root>. Use --help para detalhes.");
  process.exit(2);
}

const require = createRequire(import.meta.url);
const sharp = require("sharp");
const { optimize } = require("svgo");

const repositoryRoot = path.resolve(args[0]);
const sourceDirectory = path.join(repositoryRoot, "docs", "brand", "assets");
const outputDirectory = path.join(sourceDirectory, "dist");

const vectorAssets = [
  ["altivy-symbol.svg", "symbol.svg"],
  ["altivy-symbol-dark.svg", "symbol-dark.svg"],
  ["altivy-symbol-monochrome.svg", "symbol-monochrome.svg"],
  ["altivy-logo.svg", "logo.svg"],
  ["altivy-logo-dark.svg", "logo-dark.svg"],
];

const rasterSizes = [16, 32, 48, 64, 192, 400, 512];

await rm(outputDirectory, { force: true, recursive: true });
await mkdir(outputDirectory, { recursive: true });

for (const [sourceName, outputName] of vectorAssets) {
  const source = await readFile(path.join(sourceDirectory, sourceName), "utf8");
  const result = optimize(source, {
    path: sourceName,
    multipass: true,
    plugins: [
      "preset-default",
      "removeDimensions",
      { name: "removeAttrs", params: { attrs: "data-*" } },
    ],
  });
  await writeFile(path.join(outputDirectory, outputName), result.data);
}

const symbol = await readFile(path.join(outputDirectory, "symbol.svg"));
for (const size of rasterSizes) {
  await sharp(symbol)
    .resize(size, size, { fit: "contain" })
    .png({ compressionLevel: 9 })
    .toFile(path.join(outputDirectory, `icon-${size}.png`));
}

const outputNames = [
  ...vectorAssets.map(([, outputName]) => outputName),
  ...rasterSizes.map((size) => `icon-${size}.png`),
].sort();

const checksums = [];
for (const outputName of outputNames) {
  const contents = await readFile(path.join(outputDirectory, outputName));
  checksums.push(`${createHash("sha256").update(contents).digest("hex")}  ${outputName}`);
}

await writeFile(path.join(outputDirectory, "SHA256SUMS"), `${checksums.join("\n")}\n`);
console.log(`Assets produzidos: ${outputNames.length}; sharp=${sharp.versions.sharp}`);
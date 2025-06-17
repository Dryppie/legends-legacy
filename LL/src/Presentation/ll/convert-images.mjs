import sharp from "sharp";
import fs from "fs-extra";
import { globby } from "globby";
import path from "path";

const inputFolder = "./src/assets/entities";
const outputFolder = "./src/assets/entities-optimized";

const files = await globby([`${inputFolder}/**/*.png`]);

for (const file of files) {
  const relativePath = path.relative(inputFolder, file);
  const outputPath = path
    .join(outputFolder, relativePath)
    .replace(/\.png$/, ".webp");

  await fs.ensureDir(path.dirname(outputPath));

  await sharp(file).resize(512).webp({ quality: 80 }).toFile(outputPath);

  console.log(`✔ Converted: ${relativePath}`);
}

console.log("✅ All PNGs converted to WebP and resized.");

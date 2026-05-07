/**
 * Creates test fixture files needed by the Newman collection.
 * Run automatically via `npm test` before Newman starts.
 */
const fs = require('fs');
const path = require('path');

const fixturesDir = path.join(__dirname, 'fixtures');
const resultsDir = path.join(__dirname, 'results');

fs.mkdirSync(fixturesDir, { recursive: true });
fs.mkdirSync(resultsDir, { recursive: true });

// Minimal valid 1×1 white PNG (PNG spec: signature + IHDR + IDAT + IEND)
const PNG_1x1 = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwADhQGAWjR9awAAAABJRU5ErkJggg==',
  'base64'
);

fs.writeFileSync(path.join(fixturesDir, 'test-image.png'), PNG_1x1);
console.log('✓ fixtures/test-image.png written');
console.log('✓ results/ directory ready');

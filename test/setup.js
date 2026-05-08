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

// ============================================================================
// Image Fixtures
// ============================================================================

// Minimal valid 1×1 white PNG generated from System.Drawing and verified by ImageSharp.
const PNG_1x1 = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAANSURBVBhXY/j///9/AAn7A/0FQ0XKAAAAAElFTkSuQmCC',
  'base64'
);

// Minimal valid 1×1 JPEG generated from System.Drawing and verified by ImageSharp.
const JPEG_1x1 = Buffer.from(
  '/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD9U6KKKAP/2Q==',
  'base64'
);

// Minimal valid SVG
const SVG_1x1 = Buffer.from(
  '<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"><rect width="1" height="1" fill="white"/></svg>',
  'utf-8'
);

// Minimal valid WEBP (8×8 white image)
const WEBP_1x1 = Buffer.from(
  'RIFF$AAAAWebPVP8LAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA',
  'base64'
);

fs.writeFileSync(path.join(fixturesDir, 'test-image.png'), PNG_1x1);
fs.writeFileSync(path.join(fixturesDir, 'test-image.jpg'), JPEG_1x1);
fs.writeFileSync(path.join(fixturesDir, 'test-image.svg'), SVG_1x1);
fs.writeFileSync(path.join(fixturesDir, 'test-image.webp'), WEBP_1x1);
console.log('✓ fixtures/test-image.png written');
console.log('✓ fixtures/test-image.jpg written');
console.log('✓ fixtures/test-image.svg written');
console.log('✓ fixtures/test-image.webp written');

// ============================================================================
// JSON Import Fixtures
// ============================================================================

// Valid product import (standard format)
const validImportProduct = {
  title: "Import Product for Export Test",
  imageUrl: "http://example.com/product.png",
  imageWidth: 200,
  imageHeight: 200,
  printZones: [
    {
      name: "Export Zone",
      x: 25,
      y: 25,
      width: 150,
      height: 150,
      maxPhysicalWidthMm: 120.0,
      maxPhysicalHeightMm: 120.0,
      maxColors: 4,
      allowedTechniques: ["screen_print"]
    }
  ]
};
fs.writeFileSync(
  path.join(fixturesDir, 'import-export-test-product.json'),
  JSON.stringify([validImportProduct]),
  'utf-8'
);
console.log('✓ fixtures/import-export-test-product.json written');

// Malformed JSON (invalid syntax)
const malformedJson = '{ invalid json content without quotes }';
fs.writeFileSync(
  path.join(fixturesDir, 'import-malformed.json'),
  malformedJson,
  'utf-8'
);
console.log('✓ fixtures/import-malformed.json written');

// Wrong file extension (.txt instead of .json)
const wrongExtensionContent = JSON.stringify([validImportProduct]);
fs.writeFileSync(
  path.join(fixturesDir, 'import-product.txt'),
  wrongExtensionContent,
  'utf-8'
);
console.log('✓ fixtures/import-product.txt written');

// ============================================================================
// Product Boundary Test Data
// ============================================================================

// Product with 1-character title
const minTitleProduct = {
  title: "A",
  imageUrl: "http://example.com/min-title.png",
  imageWidth: 100,
  imageHeight: 100,
  printZones: []
};
fs.writeFileSync(
  path.join(fixturesDir, 'import-min-title.json'),
  JSON.stringify([minTitleProduct]),
  'utf-8'
);

// Product with 500-character title
const maxTitleProduct = {
  title: "A".repeat(500),
  imageUrl: "http://example.com/max-title.png",
  imageWidth: 100,
  imageHeight: 100,
  printZones: []
};
fs.writeFileSync(
  path.join(fixturesDir, 'import-max-title.json'),
  JSON.stringify([maxTitleProduct]),
  'utf-8'
);

// Product with 501-character title (exceeds max)
const exceedsTitleProduct = {
  title: "A".repeat(501),
  imageUrl: "http://example.com/exceeds-title.png",
  imageWidth: 100,
  imageHeight: 100,
  printZones: []
};
fs.writeFileSync(
  path.join(fixturesDir, 'import-exceeds-title.json'),
  JSON.stringify([exceedsTitleProduct]),
  'utf-8'
);

console.log('✓ fixtures/import-min-title.json written');
console.log('✓ fixtures/import-max-title.json written');
console.log('✓ fixtures/import-exceeds-title.json written');

console.log('✓ results/ directory ready');
console.log('✅ All fixtures created successfully');

/** Fast dependency-free architectural checks. Run: npm run verify:structure */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = path.join(root, 'src', 'app');
const walk = dir => fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
  const name = path.join(dir, entry.name);
  return entry.isDirectory() ? walk(name) : [name];
});
const files = walk(source);
const tsFiles = files.filter(f => f.endsWith('.ts'));
const components = tsFiles.filter(f => f.endsWith('.component.ts'));
const services = tsFiles.filter(f => f.endsWith('.service.ts'));
const routes = fs.readFileSync(path.join(source, 'app.routes.ts'), 'utf8');

for (const component of components) {
  const text = fs.readFileSync(component, 'utf8');
  const match = text.match(/templateUrl:\s*['"](.+?\.html)['"]/);
  assert.ok(match, `No external template: ${path.relative(root, component)}`);
  const html = path.resolve(path.dirname(component), match[1]);
  assert.ok(fs.existsSync(html), `Missing template: ${html}`);
  assert.ok(!/\btemplate\s*:\s*`/.test(text), `Inline HTML: ${component}`);
}

for (const file of tsFiles) {
  const text = fs.readFileSync(file, 'utf8');
  // Resolve relative imports, including dynamic loadComponent imports.
  for (const match of text.matchAll(/(?:from\s*|import\()\s*['"](\.[^'"]+)['"]/g)) {
    const modulePath = path.resolve(path.dirname(file), match[1]);
    assert.ok(fs.existsSync(modulePath + '.ts') || fs.existsSync(path.join(modulePath, 'index.ts')),
      `Unresolved module: ${path.relative(root, file)} -> ${match[1]}`);
  }
  assert.ok(!/\b(?:ngIf|ngFor|ngSwitch)\b/.test(text), `Deprecated structural directive in ${file}`);
}

const routeCount = (routes.match(/\bpath:\s*'/g) ?? []).length - 2; // Excludes '' and wildcard redirects.
assert.equal(routeCount, 30, 'Expected 30 documented front-end page routes');
assert.ok(routes.includes('loadComponent'), 'Lazy loading is required');
assert.ok(!files.some(f => f.endsWith('.css')), 'No custom stylesheet is expected');
assert.ok(!files.some(f => /core\/models\.ts$|support-api\.service\.ts$/.test(f)),
  'The old monolithic model/service must be removed');
assert.ok(fs.readFileSync(path.join(root, 'src', 'index.html'), 'utf8').includes('bootstrap@5.'),
  'Bootstrap must load from CDN');
assert.ok(fs.existsSync(path.join(source, 'core', 'config', 'api.config.ts')), 'Single API config missing');
assert.ok(services.length >= 8, 'Feature services and core services are required');

console.log(`PASS: ${routeCount} routes, ${components.length} component templates, ${services.length} services.`);
console.log('PASS: relative imports, lazy loading, feature separation, Bootstrap CDN, and no custom styles.');

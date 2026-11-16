// Erzeugt das Theme der Mockups aus den Stilen der App (Standard Seitenliste, mockup-erstellen):
// liest die WPF-Ressourcen Colors.xaml und Typography.xaml und schreibt CSS-Variablen mit denselben Namen wie die
// XAML-Keys (z. B. --BpmBgBase, --BpmFontSizeBody). Seiten nutzen nur diese Variablen, nie feste Werte.
// Aufruf: node tools/mockup_theme.mjs – nach jeder Änderung an Colors.xaml oder Typography.xaml.
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const wurzel = join(dirname(fileURLToPath(import.meta.url)), '..');
const themes = join(wurzel, 'src/BauProjektManager.App/Themes');
const ziel = join(wurzel, 'Docs/Mockups/theme-app.css');

const farbenXaml = readFileSync(join(themes, 'Colors.xaml'), 'utf8');
const typoXaml = readFileSync(join(themes, 'Typography.xaml'), 'utf8');

// WPF #AARRGGBB -> CSS #RRGGBBAA
const css = (hex) => (!hex.startsWith('#') ? hex : hex.length === 9 ? `#${hex.slice(3)}${hex.slice(1, 3)}` : hex);

// <Color x:Key="BpmColorBgBase">#1E1E1E</Color>
const farben = new Map();
for (const m of farbenXaml.matchAll(/<Color\s+x:Key="(\w+)"\s*>\s*(#[0-9A-Fa-f]{6,8})\s*<\/Color>/g)) {
  farben.set(m[1], m[2].toUpperCase());
}

const zeilen = [];
for (const [k, v] of farben) zeilen.push(`  --${k}: ${css(v)};`);

// <SolidColorBrush x:Key="BpmBgBase" Color="{StaticResource BpmColorBgBase}"/> oder Color="#..."
for (const m of farbenXaml.matchAll(/<SolidColorBrush\s+x:Key="(\w+)"([^>]*?)\/?>/g)) {
  const attr = m[2];
  const ref = attr.match(/Color="\{(?:StaticResource|DynamicResource)\s+(\w+)\}"/);
  const roh = attr.match(/Color="(#[0-9A-Fa-f]{6,8})"/);
  const name = attr.match(/Color="([A-Za-z]+)"/); // benannte WPF-Farbe, z. B. Transparent
  const wert = ref ? farben.get(ref[1]) : roh?.[1]?.toUpperCase() ?? name?.[1]?.toLowerCase();
  if (!wert) throw new Error(`Brush ${m[1]}: Farbe nicht auflösbar`);
  const deck = attr.match(/Opacity="([\d.]+)"/);
  zeilen.push(deck
    ? `  --${m[1]}: color-mix(in srgb, ${css(wert)} ${Math.round(Number(deck[1]) * 100)}%, transparent);`
    : `  --${m[1]}: ${css(wert)};`);
}

// <sys:Double x:Key="BpmFontSizeBody">13</sys:Double>, <FontFamily x:Key="BpmFontFamily">Segoe UI</FontFamily>
for (const m of typoXaml.matchAll(/<sys:Double\s+x:Key="(\w+)"\s*>\s*([\d.]+)\s*<\/sys:Double>/g)) {
  zeilen.push(`  --${m[1]}: ${m[2]}px;`);
}
const familie = typoXaml.match(/<FontFamily\s+x:Key="BpmFontFamily"\s*>([^<]+)<\/FontFamily>/);
if (familie) zeilen.push(`  --BpmFontFamily: "${familie[1].trim()}", system-ui, sans-serif;`);

const text = `/* Erzeugt von tools/mockup_theme.mjs aus Colors.xaml und Typography.xaml – nicht von Hand ändern. */
:root {
${zeilen.join('\n')}
}
.rahmen { background: var(--BpmBgBase); color: var(--BpmTextPrimary); font-family: var(--BpmFontFamily); font-size: var(--BpmFontSizeBody); }
`;
writeFileSync(ziel, text);

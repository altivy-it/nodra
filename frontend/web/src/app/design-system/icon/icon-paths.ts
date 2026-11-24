/**
 * Geometria própria dos ícones do Nodra (spec 007): grade 24×24, traço de 1.75,
 * terminações arredondadas, no estilo de linha usado pelo Obsidian. Cada ícone é
 * uma lista de elementos SVG simples desenhados para este projeto.
 */
export type IconShape =
  | { path: string }
  | { circle: [cx: number, cy: number, r: number] }
  | { rect: [x: number, y: number, width: number, height: number, radius: number] };

export const ICONS = {
  'file-text': [{ path: 'M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z' }, { path: 'M14 3v5h5M9 13h6M9 17h4' }],
  'file-plus': [{ path: 'M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z' }, { path: 'M14 3v5h5M12 11v6M9 14h6' }],
  folder: [{ path: 'M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z' }],
  'folder-plus': [{ path: 'M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z' }, { path: 'M12 10v6M9 13h6' }],
  files: [{ path: 'M15 3H9a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V7z' }, { path: 'M15 3v4h4M5 8v11a2 2 0 0 0 2 2h8' }],
  search: [{ circle: [11, 11, 6.5] }, { path: 'm20 20-4.2-4.2' }],
  tag: [{ path: 'M3 12V5a2 2 0 0 1 2-2h7l9 9-9 9z' }, { circle: [8, 8, 1.4] }],
  graph: [{ circle: [6, 6, 2.5] }, { circle: [18, 8, 2.5] }, { circle: [9, 18, 2.5] }, { path: 'M8.4 6.6 15.5 7.6M7 8.3l1.4 7.3M16.6 10.1l-5.6 6.2' }],
  help: [{ circle: [12, 12, 9] }, { path: 'M9.5 9.2a2.6 2.6 0 0 1 5 .9c0 1.8-2.5 2.3-2.5 3.9M12 17.2v.1' }],
  settings: [{ path: 'M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M20 18h0' }, { circle: [16, 6, 2] }, { circle: [10, 12, 2] }, { circle: [18, 18, 2] }],
  trash: [{ path: 'M4 7h16M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V4h6v3' }],
  refresh: [{ path: 'M20 12a8 8 0 0 1-14.3 4.9M4 12A8 8 0 0 1 18.3 7.1' }, { path: 'M18.5 3v4.2h-4.2M5.5 21v-4.2h4.2' }],
  plus: [{ path: 'M12 5v14M5 12h14' }],
  x: [{ path: 'M6 6l12 12M18 6 6 18' }],
  'chevron-right': [{ path: 'm9 6 6 6-6 6' }],
  'chevron-down': [{ path: 'm6 9 6 6 6-6' }],
  'arrow-right': [{ path: 'M5 12h14M13 6l6 6-6 6' }],
  'panel-left': [{ rect: [3, 4, 18, 16, 2] }, { path: 'M9 4v16' }],
  'panel-right': [{ rect: [3, 4, 18, 16, 2] }, { path: 'M15 4v16' }],
  contrast: [{ circle: [12, 12, 9] }, { path: 'M12 3a9 9 0 0 1 0 18z' }],
  sun: [{ circle: [12, 12, 4] }, { path: 'M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41' }],
  moon: [{ path: 'M20.5 14.5A8.5 8.5 0 0 1 9.5 3.5a8.5 8.5 0 1 0 11 11z' }],
  monitor: [{ rect: [3, 4, 18, 12, 2] }, { path: 'M8 20h8M12 16v4' }],
  bell: [{ path: 'M18 9a6 6 0 0 0-12 0c0 6-2.5 7.5-2.5 7.5h17S18 15 18 9M10 20a2.2 2.2 0 0 0 4 0' }],
  user: [{ circle: [12, 8, 4] }, { path: 'M4.5 20a7.5 7.5 0 0 1 15 0' }],
  'log-in': [{ path: 'M14 4h4a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-4M10 8l4 4-4 4M14 12H3' }],
  'log-out': [{ path: 'M10 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4M16 8l4 4-4 4M20 12H9' }],
  lock: [{ rect: [5, 10, 14, 10, 2] }, { path: 'M8 10V7a4 4 0 0 1 8 0v3' }],
  clock: [{ circle: [12, 12, 9] }, { path: 'M12 7v5l3 2' }],
  alert: [{ path: 'M12 4 2.8 19.5h18.4z' }, { path: 'M12 10v4M12 17v.1' }],
  info: [{ circle: [12, 12, 9] }, { path: 'M12 11v5M12 8v.1' }],
  'check-circle': [{ circle: [12, 12, 9] }, { path: 'm8 12.5 2.7 2.7L16 10' }],
  'x-circle': [{ circle: [12, 12, 9] }, { path: 'm9 9 6 6M15 9l-6 6' }],
  'wifi-off': [{ path: 'M3 3l18 18M8.5 16.5a5 5 0 0 1 7 0M5 13a10 10 0 0 1 5-2.7M19 13a10 10 0 0 0-2.4-1.7M2 9.5a15 15 0 0 1 4.2-2.6M22 9.5A15 15 0 0 0 11 5.6M12 20v.1' }],
  circle: [{ circle: [12, 12, 8] }],
  edit: [{ path: 'M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16z' }, { path: 'M13.5 6.5l4 4' }],
  eye: [{ path: 'M2.5 12S6 5 12 5s9.5 7 9.5 7-3.5 7-9.5 7-9.5-7-9.5-7' }, { circle: [12, 12, 3] }],
  columns: [{ rect: [3, 4, 18, 16, 2] }, { path: 'M12 4v16' }],
  'more-horizontal': [{ circle: [6, 12, 1] }, { circle: [12, 12, 1] }, { circle: [18, 12, 1] }],
} satisfies Record<string, IconShape[]>;

export type IconName = keyof typeof ICONS;

export function isIconName(name: string): name is IconName {
  return Object.prototype.hasOwnProperty.call(ICONS, name);
}

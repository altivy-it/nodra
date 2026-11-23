import DOMPurify, { type Config, type DOMPurify as DOMPurifyInstance } from 'dompurify';

/**
 * Política única do preview Markdown: preserva toda a marcação (HTML, SVG,
 * MathML, checkboxes, `details`, `style`, `id`, links relativos, âncoras,
 * `mailto:` e imagens `data:`) e remove somente o que executa código na origem
 * do Nodra: `<script>`, handlers `on*` e URLs `javascript:`/`vbscript:`.
 * O arquivo `.md` nunca é alterado; a política age só no HTML renderizado.
 */
export const MARKDOWN_PREVIEW_POLICY: Config = {
  USE_PROFILES: { html: true, svg: true, svgFilters: true, mathMl: true },
  ADD_TAGS: ['input', 'iframe'],
  ADD_ATTR: [
    'type', 'checked', 'disabled', 'target', 'allow', 'allowfullscreen', 'frameborder',
    'loading', 'referrerpolicy', 'sandbox'
  ],
  ALLOW_DATA_ATTR: true,
  ALLOW_UNKNOWN_PROTOCOLS: false
};

/** Permissões dos iframes embutidos; sem `allow-same-origin` para origens do próprio Nodra. */
const EXTERNAL_FRAME_SANDBOX = 'allow-scripts allow-same-origin allow-popups allow-presentation';

let purifier: DOMPurifyInstance | null = null;

function isExternalHttpsFrame(source: string | null): boolean {
  if (!source) return false;
  try {
    const url = new URL(source, document.baseURI);
    return url.protocol === 'https:' && url.origin !== window.location.origin;
  } catch {
    return false;
  }
}

function createPurifier(): DOMPurifyInstance {
  const instance = DOMPurify(window);
  instance.addHook('afterSanitizeAttributes', node => {
    if (node.tagName === 'IFRAME') {
      node.removeAttribute('srcdoc');
      if (!isExternalHttpsFrame(node.getAttribute('src'))) node.removeAttribute('src');
      node.setAttribute('sandbox', EXTERNAL_FRAME_SANDBOX);
      node.setAttribute('referrerpolicy', 'no-referrer');
    }
    if (node.tagName === 'A' && node.getAttribute('target') === '_blank') {
      node.setAttribute('rel', 'noopener noreferrer');
    }
  });
  return instance;
}

/** Aplica {@link MARKDOWN_PREVIEW_POLICY} ao HTML gerado pelo parser Markdown. */
export function sanitizeMarkdownPreview(html: string): string {
  purifier ??= createPurifier();
  return purifier.sanitize(html, { ...MARKDOWN_PREVIEW_POLICY, RETURN_TRUSTED_TYPE: false }) as string;
}

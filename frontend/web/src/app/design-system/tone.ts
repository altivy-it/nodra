import type { IconName } from './icon/icon-paths';

export type Tone = 'success' | 'warning' | 'error' | 'info' | 'accent' | 'neutral';

export interface ToneSpec {
  color: string;
  background: string;
  icon: IconName;
}

/** Tom semântico → tokens. Todo estado comunica por texto ou ícone, nunca só por cor. */
export const TONES: Record<Tone, ToneSpec> = {
  success: { color: 'var(--signal-success)', background: 'var(--success-tint)', icon: 'check-circle' },
  warning: { color: 'var(--signal-warning)', background: 'var(--warning-tint)', icon: 'alert' },
  error: { color: 'var(--signal-error)', background: 'var(--error-tint)', icon: 'x-circle' },
  info: { color: 'var(--signal-info)', background: 'var(--info-tint)', icon: 'info' },
  accent: { color: 'var(--accent)', background: 'var(--accent-tint)', icon: 'graph' },
  neutral: { color: 'var(--text-muted)', background: 'var(--hover-wash)', icon: 'circle' },
};

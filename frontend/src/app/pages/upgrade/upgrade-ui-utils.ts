export function downloadTextFileJson(fileName: string, text: string) {
  const blob = new Blob([text], { type: 'application/json;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function downloadTextFilePlain(fileName: string, text: string, mime: string) {
  const blob = new Blob([text], { type: mime });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function formatDuration(ms: number | null): string {
  if (ms === null || ms < 0) return '—';

  const totalSeconds = Math.floor(ms / 1000);
  const seconds = totalSeconds % 60;
  const totalMinutes = Math.floor(totalSeconds / 60);
  const minutes = totalMinutes % 60;
  const hours = Math.floor(totalMinutes / 60);

  if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
  if (minutes > 0) return `${minutes}m ${seconds}s`;
  return `${seconds}s`;
}

export function formatTimeAgo(dt: Date, nowServerMs: number): string {
  const ms = nowServerMs - dt.getTime();
  if (ms < 0) return '0s';

  const totalSeconds = Math.floor(ms / 1000);
  const seconds = totalSeconds % 60;
  const totalMinutes = Math.floor(totalSeconds / 60);
  const minutes = totalMinutes % 60;
  const hours = Math.floor(totalMinutes / 60);

  if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
  if (minutes > 0) return `${minutes}m ${seconds}s`;
  return `${seconds}s`;
}

export function badgeStyle(stateRaw: string): Record<string, string> {
  const state = (stateRaw ?? '').toLowerCase().trim();

  let bg = '#eee';
  let fg = '#111';
  let border = '#bbb';

  if (state === 'pending') { bg = '#fff4d6'; fg = '#6a4b00'; border = '#ffcd5b'; }
  if (state === 'running') { bg = '#e6f0ff'; fg = '#0b3d91'; border = '#93b7ff'; }
  if (state === 'succeeded') { bg = '#e7f6ec'; fg = '#0f5b2b'; border = '#7ad19a'; }
  if (state === 'failed') { bg = '#fde8ea'; fg = '#8a0f1a'; border = '#f0a4aa'; }
  if (state === 'canceled') { bg = '#f0f0f0'; fg = '#444'; border = '#cfcfcf'; }

  return {
    'display': 'inline-block',
    'padding': '2px 8px',
    'border': `1px solid ${border}`,
    'border-radius': '999px',
    'background': bg,
    'color': fg,
    'font-size': '12px',
    'line-height': '16px',
    'font-weight': '600',
    'text-transform': 'lowercase'
  };
}

export function enforcementBadgeStyle(stateRaw: string): Record<string, string> {
  const state = (stateRaw ?? '').toLowerCase().trim();

  let bg = '#eee';
  let fg = '#222';

  if (state === 'ok') { bg = '#d6f5dd'; fg = '#0f5b2b'; }
  else if (state === 'warn') { bg = '#fff0cc'; fg = '#7a4a00'; }
  else if (state === 'soft_block') { bg = '#ffe0b2'; fg = '#7a4a00'; }
  else if (state === 'hard_block') { bg = '#ffd6d6'; fg = '#b00020'; }

  return {
    'display': 'inline-block',
    'padding': '2px 8px',
    'border-radius': '999px',
    'background': bg,
    'color': fg,
    'font-size': '12px',
    'line-height': '16px',
    'font-weight': '600',
  };
}

export function shortId(id: string): string {
  const v = (id ?? '').trim();
  if (!v) return '';
  return v.length <= 8 ? v : v.slice(0, 8);
}

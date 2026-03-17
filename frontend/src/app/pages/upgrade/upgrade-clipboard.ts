export async function copyTextWithFallback(args: {
  text: string;
  setCopyStatus: (msg: string) => void;
}): Promise<void> {
  try {
    await navigator.clipboard.writeText(args.text);
    args.setCopyStatus('Copied to clipboard.');
  } catch {
    const el = document.createElement('textarea');
    el.value = args.text;
    el.style.position = 'fixed';
    el.style.opacity = '0';
    document.body.appendChild(el);
    el.focus();
    el.select();
    document.execCommand('copy');
    document.body.removeChild(el);
    args.setCopyStatus('Copied to clipboard.');
  }
}

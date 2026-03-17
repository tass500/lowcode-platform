export function copyAbsoluteUrlImpl(args: {
  url: string;
  toAbsoluteUrl: (url: string) => string;
  copyText: (text: string) => void | Promise<void>;
}) {
  const abs = args.toAbsoluteUrl(args.url);
  if (!abs) return;
  void args.copyText(abs);
}

export function copyAuditListUrlImpl(args: {
  buildAuditUrl: () => string;
  copyAbsoluteUrl: (url: string) => void;
}) {
  args.copyAbsoluteUrl(args.buildAuditUrl());
}

export function copyAuditExportUrlImpl(args: {
  buildAuditExportUrl: () => string;
  copyAbsoluteUrl: (url: string) => void;
}) {
  args.copyAbsoluteUrl(args.buildAuditExportUrl());
}

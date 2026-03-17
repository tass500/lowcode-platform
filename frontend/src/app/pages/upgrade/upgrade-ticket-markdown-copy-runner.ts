export async function copyTicketMarkdownImpl(args: {
  text: string;
  copyText: (text: string) => Promise<void>;
}): Promise<void> {
  if (!args.text) return;
  await args.copyText(args.text);
}

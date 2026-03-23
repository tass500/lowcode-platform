/** Prettify / minify workflow definition JSON strings (iter 45 QoL). */

export function prettifyWorkflowDefinitionJson(raw: string): string {
  return JSON.stringify(JSON.parse(raw), null, 2);
}

export function minifyWorkflowDefinitionJson(raw: string): string {
  return JSON.stringify(JSON.parse(raw));
}

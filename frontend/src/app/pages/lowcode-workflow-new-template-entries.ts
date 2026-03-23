/**
 * Executable workflow templates for the "New workflow" page (single source for buttons + apply).
 */

export type WorkflowNewPageTemplateKind =
  | 'noop'
  | 'delay250'
  | 'delay3'
  | 'timeoutDelay'
  | 'set'
  | 'map'
  | 'merge'
  | 'foreach'
  | 'switch'
  | 'retryUpdateById'
  | 'require'
  | 'domainEcho'
  | 'domainCreateRecord'
  | 'domainUpdateRecord'
  | 'domainDeleteRecord'
  | 'domainUpsertRecord'
  | 'createAndUpdateById'
  | 'setAndUpdateById';

export type WorkflowNewTemplateEntry = {
  kind: WorkflowNewPageTemplateKind;
  /** Button label */
  label: string;
  /** Default workflow name */
  name: string;
  json: string;
};

export const WORKFLOW_NEW_TEMPLATE_ENTRIES: readonly WorkflowNewTemplateEntry[] = [
  { kind: 'noop', label: 'No-op', name: 'wf-noop', json: '{"steps":[{"type":"noop"}]}' },
  { kind: 'delay250', label: 'Delay 250ms', name: 'wf-delay-250ms', json: '{"steps":[{"type":"delay","ms":250}]}' },
  {
    kind: 'delay3',
    label: '3x Delay',
    name: 'wf-3x-delay',
    json: '{"steps":[{"type":"delay","ms":100},{"type":"delay","ms":200},{"type":"delay","ms":300}]}',
  },
  {
    kind: 'timeoutDelay',
    label: 'Timeout (delay)',
    name: 'wf-timeout-delay',
    json: '{"steps":[{"type":"delay","ms":200,"timeoutMs":50},{"type":"noop"}]}',
  },
  {
    kind: 'set',
    label: 'Set (seed output)',
    name: 'wf-set-seed',
    json: '{"steps":[{"type":"set","output":{"recordId":"<RECORD_ID_GUID>","note":"seeded value"}},{"type":"noop"}]}',
  },
  {
    kind: 'map',
    label: 'Map (projection)',
    name: 'wf-map-projection',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Ltd","status":"active"}},{"type":"map","mappings":{"recordId":"000.entityRecordId"}},{"type":"noop"}]}',
  },
  {
    kind: 'merge',
    label: 'Merge (combine objects)',
    name: 'wf-merge-objects',
    json: '{"steps":[{"type":"set","output":{"a":1,"b":2}},{"type":"merge","sources":[{"b":99,"c":3},"000"]},{"type":"noop"}]}',
  },
  {
    kind: 'foreach',
    label: 'Foreach (iterate)',
    name: 'wf-foreach-items',
    json:
      '{"steps":[{"type":"set","output":{"items":[{"n":1},{"n":2}]}},{"type":"foreach","items":"000.items","do":{"type":"map","mappings":{"n":"item.n"}}},{"type":"noop"}]}',
  },
  {
    kind: 'switch',
    label: 'Switch (branch)',
    name: 'wf-switch-branch',
    json:
      '{"steps":[{"type":"set","output":{"kind":"a"}},{"type":"switch","value":"000.kind","cases":[{"when":"a","do":{"type":"set","output":{"result":1}}},{"when":"b","do":{"type":"set","output":{"result":2}}}],"default":{"type":"set","output":{"result":99}}},{"type":"noop"}]}',
  },
  {
    kind: 'retryUpdateById',
    label: 'Retry (updateById)',
    name: 'wf-retry-update-by-id',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.updateById","recordId":"<RECORD_ID_GUID>","data":{"status":"active"},"retry":{"maxAttempts":5,"delayMs":200,"backoffFactor":2,"maxDelayMs":2000}},{"type":"noop"}]}',
  },
  {
    kind: 'require',
    label: 'Require (guard)',
    name: 'wf-require-guard',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Ltd","status":"active"}},{"type":"require","path":"000.entityRecordId"},{"type":"noop"}]}',
  },
  { kind: 'domainEcho', label: 'Domain: echo', name: 'wf-domain-echo', json: '{"steps":[{"type":"domainCommand","command":"echo"}]}' },
  {
    kind: 'domainCreateRecord',
    label: 'Domain: create record',
    name: 'wf-domain-create-record',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Ltd","status":"active"}}]}',
  },
  {
    kind: 'domainUpdateRecord',
    label: 'Domain: update record',
    name: 'wf-domain-update-record',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.updateById","recordId":"<RECORD_ID_GUID>","data":{"name":"Acme Updated","status":"inactive"}}]}',
  },
  {
    kind: 'domainDeleteRecord',
    label: 'Domain: delete record',
    name: 'wf-domain-delete-record',
    json: '{"steps":[{"type":"domainCommand","command":"entityRecord.deleteById","recordId":"<RECORD_ID_GUID>"}]}',
  },
  {
    kind: 'domainUpsertRecord',
    label: 'Domain: upsert record',
    name: 'wf-domain-upsert-record',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.upsertByEntityName","entityName":"Company","uniqueKey":"externalId","uniqueValue":"c-1","data":{"externalId":"c-1","name":"Acme Upsert","status":"active"}}]}',
  },
  {
    kind: 'createAndUpdateById',
    label: 'Create + updateById (demo)',
    name: 'wf-create-update-by-id',
    json:
      '{"steps":[{"type":"domainCommand","command":"entityRecord.createByEntityName","entityName":"Company","data":{"name":"Acme Seed","status":"active"}},{"type":"domainCommand","command":"entityRecord.updateById","recordId":"${000.entityRecordId}","data":{"name":"Acme Updated","status":"inactive"}}]}',
  },
  {
    kind: 'setAndUpdateById',
    label: 'Set + updateById (manual GUID)',
    name: 'wf-set-update-by-id',
    json:
      '{"steps":[{"type":"set","output":{"recordId":"<RECORD_ID_GUID>"}},{"type":"domainCommand","command":"entityRecord.updateById","recordId":"${000.recordId}","data":{"name":"Acme Updated","status":"inactive"}}]}',
  },
];

export function filterWorkflowNewTemplateEntries(
  entries: readonly WorkflowNewTemplateEntry[],
  query: string
): WorkflowNewTemplateEntry[] {
  const q = query.trim().toLowerCase();
  if (!q) return [...entries];
  return entries.filter(e => {
    const blob = `${e.kind} ${e.label} ${e.name} ${e.json}`.toLowerCase();
    return blob.includes(q);
  });
}

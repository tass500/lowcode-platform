import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { buildIncidentBundleImpl } from './upgrade/upgrade-incident-bundle-builder';
import { buildCurlSnippetsText } from './upgrade/upgrade-curl-snippets';
import { curlSnippetsTextImpl } from './upgrade/upgrade-curl-snippets-actions';
import type {
  AuditLogItem,
  InstallationStatus,
  ObservabilitySnapshot,
  QueueUpgradeRun,
  RecentUpgradeRun,
  ServerTimedResponse,
  ServerTimedSingleResponse,
  StartUpgradeResponse,
  UpgradeRun,
  UpgradeRunStep,
} from './upgrade/upgrade-types';

@Component({
  selector: 'app-upgrade-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  template: `
    <main style="padding: 16px; max-width: 980px; margin: 0 auto;">
      <h2>Upgrade</h2>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="display:flex; gap: 12px; align-items: end; flex-wrap: wrap;">
          <label>
            Client TraceId
            <input [value]="clientTraceId" (input)="clientTraceId = (($any($event.target).value ?? '').trim())" placeholder="(optional)" style="min-width: 360px;" />
          </label>
          <button type="button" (click)="generateClientTraceId()">Generate</button>
          <button type="button" (click)="clearClientTraceId()" [disabled]="!clientTraceId">Clear</button>
          <button type="button" (click)="refreshAll()">Refresh all</button>

          <div style="display:flex; gap: 8px; flex-wrap: wrap; align-items: center;">
            <button type="button" (click)="copyClientTraceHeaderArg()" [disabled]="!clientTraceId">Copy header</button>
            <button type="button" (click)="copyTraceId()" [disabled]="!run?.traceId">Copy run trace</button>
            <button type="button" (click)="copyCurlSnippets()" [disabled]="!runId">Copy curls</button>
            <button type="button" (click)="copyIncidentBundle()" [disabled]="copyBundleLoading">Copy incident bundle</button>
          </div>
        </div>
        <div style="margin-top: 8px; color:#444;">
          If set, all admin API calls will send this as the <code>X-Trace-Id</code> header.
        </div>
      </section>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="display:flex; gap: 12px; align-items:center; flex-wrap: wrap;">
          <h3 style="margin:0;">Queue (pending/running)</h3>
          <button type="button" (click)="loadQueue()">Refresh queue</button>
          <div *ngIf="queueLoading">Loading...</div>
          <div *ngIf="queueError" style="color:#b00020;">{{ queueError }}</div>
        </div>

        <div *ngIf="lastQueueRefreshedAt" style="margin-top: 8px; color:#444;">
          <span><b>Last refreshed</b>: {{ lastQueueRefreshedAt | date: 'medium' }}</span>
          <span> ({{ formatTimeAgo(lastQueueRefreshedAt) }} ago)</span>
        </div>

        <div style="margin-top: 10px;" *ngIf="queueRuns.length === 0 && !queueLoading">
          No active runs.
        </div>

        <table *ngIf="queueRuns.length > 0" style="width:100%; border-collapse: collapse; margin-top: 10px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">State</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Target</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Run</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Started</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Duration</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Action</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let q of queueRuns">
              <td style="border-bottom:1px solid #eee; padding: 6px;"><span [ngStyle]="badgeStyle(q.state)">{{ q.state }}</span></td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ q.targetVersion }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ shortId(q.upgradeRunId) }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ q.startedAtUtc | date: 'medium' }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ formatDuration(queueRunDurationMs(q)) }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <button type="button" (click)="onSelectRecent(q.upgradeRunId)">Open</button>
              </td>
            </tr>
          </tbody>
        </table>
      </section>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="display:flex; align-items:center; gap: 12px; flex-wrap: wrap;">
          <button type="button" (click)="loadStatus()">Refresh status</button>
          <div *ngIf="statusLoading">Loading...</div>
          <div *ngIf="statusError" style="color:#b00020;">{{ statusError }}</div>
        </div>

        <div *ngIf="clockDriftWarning" style="margin-top: 8px; color:#8a2a00;">
          {{ clockDriftWarning }}
        </div>

        <div style="margin-top: 8px; color:#444; display:flex; gap: 10px; flex-wrap: wrap; align-items: end;">
          <label>
            Dev: simulate client clock drift (minutes)
            <input type="number" [value]="simulatedClientDriftMinutes" (input)="setSimulatedClientDriftMinutes(toNumber(($any($event.target).value ?? 0), 0))" style="width: 90px;" />
          </label>
          <button type="button" (click)="setSimulatedClientDriftMinutes(0)" [disabled]="simulatedClientDriftMinutes === 0">Clear drift</button>
        </div>

        <div *ngIf="lastServerTimeUtcApplied" style="margin-top: 8px; color:#444; font-family: monospace;">
          <div><b>serverTimeUtc</b>: {{ lastServerTimeUtcApplied }}</div>
          <div><b>offset</b>: {{ serverNowOffsetMs }}ms (~{{ offsetMinutesAbs }}m {{ serverNowOffsetMs >= 0 ? 'behind' : 'ahead of' }} server)</div>
          <div><b>source</b>: {{ lastServerTimeUtcSource }}</div>
        </div>

        <div *ngIf="status" style="margin-top: 12px;">
          <div><b>Current</b>: {{ status.currentVersion }}</div>
          <div><b>Supported</b>: {{ status.supportedVersion }}</div>
          <div *ngIf="status.releaseDateUtc"><b>Release date</b>: {{ status.releaseDateUtc | date: 'medium' }}</div>
          <div *ngIf="status.upgradeWindowDays"><b>Upgrade window days</b>: {{ status.upgradeWindowDays }}</div>
          <div><b>Enforcement</b>: {{ status.enforcementState }} (daysOutOfSupport={{ status.daysOutOfSupport }})</div>
        </div>
      </section>

      <section style="margin-top: 12px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="display:flex; gap: 12px; align-items:center; flex-wrap: wrap;">
          <h3 style="margin:0;">Observability snapshot</h3>
          <button type="button" (click)="loadObservability()">Refresh</button>
          <div *ngIf="observabilityLoading">Loading...</div>
          <div *ngIf="observabilityError" style="color:#b00020;">{{ observabilityError }}</div>
        </div>

        <div *ngIf="lastObservabilityRefreshedAt" style="margin-top: 8px; color:#444;">
          <span><b>Last refreshed</b>: {{ lastObservabilityRefreshedAt | date: 'medium' }}</span>
          <span> ({{ formatTimeAgo(lastObservabilityRefreshedAt) }} ago)</span>
        </div>

        <div *ngIf="observability" style="margin-top: 10px;">
          <div><b>Enforcement</b>: {{ observability.enforcementState }} (daysOutOfSupport={{ observability.daysOutOfSupport }})</div>

          <div style="margin-top: 8px;"><b>Active runs</b>: {{ observability.activeRuns.length }}</div>
          <table *ngIf="observability.activeRuns.length > 0" style="width:100%; border-collapse: collapse; margin-top: 8px;">
            <thead>
              <tr>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">State</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Target</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Run</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Started</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Trace</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of observability.activeRuns">
                <td style="border-bottom:1px solid #eee; padding: 6px;"><span [ngStyle]="badgeStyle(r.state)">{{ r.state }}</span></td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.targetVersion }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ shortId(r.upgradeRunId) }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ r.startedAtUtc | date: 'medium' }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ shortId(r.traceId) }}</td>
              </tr>
            </tbody>
          </table>

          <div style="margin-top: 10px;">
            <div><b>Last audit</b>:</div>
            <div *ngIf="!observability.lastAudit" style="color:#444;">—</div>
            <div *ngIf="observability.lastAudit" style="margin-top: 4px;">
              <div><b>At</b>: {{ observability.lastAudit.timestampUtc | date: 'medium':'UTC' }}</div>
              <div><b>Actor</b>: {{ observability.lastAudit.actor }}</div>
              <div><b>Action</b>: {{ observability.lastAudit.action }}</div>
              <div><b>Target</b>: {{ observability.lastAudit.target }}</div>
              <div><b>Trace</b>: {{ observability.lastAudit.traceId }}</div>
            </div>
          </div>
        </div>
      </section>

      <section style="margin-top: 16px; padding: 12px; border: 1px dashed #bbb; border-radius: 8px;">
        <h3 style="margin-top:0;">Dev: simulate enforcement</h3>

        <form [formGroup]="devForm" (ngSubmit)="devSetInstallation()" style="display:flex; gap: 12px; align-items: end; flex-wrap: wrap;">
          <label>
            Current
            <input formControlName="currentVersion" placeholder="0.1.0" />
          </label>
          <label>
            Supported
            <input formControlName="supportedVersion" placeholder="0.1.0" />
          </label>
          <label>
            Release date (UTC)
            <input formControlName="releaseDateUtc" placeholder="2026-03-01T00:00:00Z" style="min-width: 260px;" />
          </label>
          <label>
            Window days
            <input formControlName="upgradeWindowDays" type="number" placeholder="60" style="width: 90px;" />
          </label>

          <button type="submit" [disabled]="devForm.invalid || devLoading">Apply</button>

          <button type="button" (click)="presetOk()" [disabled]="devLoading">Preset ok</button>
          <button type="button" (click)="presetWarn()" [disabled]="devLoading">Preset warn</button>
          <button type="button" (click)="presetSoftBlock()" [disabled]="devLoading">Preset soft</button>
          <button type="button" (click)="presetHardBlock()" [disabled]="devLoading">Preset hard</button>

          <div *ngIf="devLoading">Applying...</div>
          <div *ngIf="devError" style="color:#b00020;">{{ devError }}</div>
        </form>
      </section>

      <section style="margin-top: 16px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <h3 style="margin-top:0;">Start upgrade run</h3>

        <div *ngIf="isBlocked" style="margin-bottom: 10px; color:#b00020;">
          Upgrade operations are blocked until the installation is upgraded.
        </div>

        <div *ngIf="!isBlocked && hasAnyActiveRun" style="margin-bottom: 10px; color:#b00020;">
          An upgrade run is already active. Cancel it or wait until it finishes.
        </div>

        <form [formGroup]="form" (ngSubmit)="startUpgrade()" style="display:flex; gap: 12px; align-items: end; flex-wrap: wrap;">
          <label>
            Target version
            <input formControlName="targetVersion" placeholder="0.2.0" />
          </label>

          <button type="submit" [disabled]="isBlocked || hasAnyActiveRun || !form.controls.targetVersion.value.trim() || startLoading">Start</button>

          <div *ngIf="startLoading">Starting...</div>
          <div *ngIf="startError" style="color:#b00020;">{{ startError }}</div>
          <div *ngIf="lastRunId">Last run id: {{ lastRunId }}</div>
        </form>
      </section>

      <section style="margin-top: 16px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <h3 style="margin-top:0;">Upgrade run details</h3>

        <div style="display:flex; gap: 12px; align-items: end; flex-wrap: wrap;">
          <label>
            Recent
            <select [value]="runId" (change)="onSelectRecent(($any($event.target).value ?? '').trim())">
              <option value="">--</option>
              <option *ngFor="let r of recentRuns" [value]="r.upgradeRunId">
                {{ recentLabel(r) }}
              </option>
            </select>
          </label>

          <label>
            Run id
            <input [value]="runId" (input)="runId = ($any($event.target).value ?? '').trim()" placeholder="guid" style="min-width: 340px;" />
          </label>

          <button type="button" (click)="loadLatest()" [disabled]="runLoading">Load latest</button>
          <button type="button" (click)="loadRun()" [disabled]="!runId || runLoading">Load</button>
          <button type="button" (click)="retryRun()" [disabled]="isBlocked || hasOtherActiveRun || !runId || retryLoading">Retry failed</button>
          <button type="button" (click)="cancelRun()" [disabled]="isBlocked || !runId || cancelLoading || !isCancelable">Cancel</button>
          <button type="button" (click)="togglePolling()" [disabled]="!runId">{{ pollingEnabled ? 'Stop polling' : 'Poll' }}</button>

          <button type="button" (click)="copyRunId()" [disabled]="!runId">Copy run id</button>

          <div *ngIf="runLoading">Loading...</div>
          <div *ngIf="retryLoading">Retrying...</div>
          <div *ngIf="cancelLoading">Canceling...</div>
          <div *ngIf="copyBundleLoading">Bundling...</div>
          <div *ngIf="runError" style="color:#b00020;">{{ runError }}</div>
          <div *ngIf="copyStatus" style="color:#0f5b2b;">{{ copyStatus }}</div>
        </div>

        <div *ngIf="!isBlocked && hasOtherActiveRun" style="margin-top: 10px; color:#b00020;">
          Retry is blocked because another upgrade run is active.
        </div>

        <div *ngIf="run" style="margin-top: 12px;">
          <div><b>State</b>: <span [ngStyle]="badgeStyle(run.state)">{{ run.state }}</span></div>
          <div><b>Target</b>: {{ run.targetVersion }}</div>
          <div><b>TraceId</b>: {{ run.traceId }}</div>
          <div><b>Duration</b>: {{ formatDuration(runDurationMs(run)) }}</div>
          <div *ngIf="lastRunRefreshedAt"><b>Last refreshed</b>: {{ lastRunRefreshedAt | date: 'medium' }}</div>
          <div *ngIf="lastRunRefreshedAt"><b>Last refreshed (ago)</b>: {{ formatTimeAgo(lastRunRefreshedAt) }}</div>
          <div *ngIf="run.errorCode || run.errorMessage"><b>Error</b>: {{ run.errorCode }} {{ run.errorMessage }}</div>

          <div style="margin-top: 10px; padding: 10px; border: 1px dashed #bbb; border-radius: 8px;">
            <div style="font-weight: 600; margin-bottom: 6px;">Debug bundle</div>

            <div style="display:flex; gap: 8px; flex-wrap: wrap; align-items: center;">
              <button type="button" (click)="copyTicketHeader()" [disabled]="!run">Copy ticket header</button>
              <button type="button" (click)="copyShortTicketHeader()" [disabled]="!run">Copy short header</button>
              <button type="button" (click)="copyTicketMarkdown()" [disabled]="!run">Copy ticket (Markdown)</button>
              <button type="button" (click)="copyClientTraceHeaderArg()" [disabled]="!clientTraceId">Copy X-Trace-Id header</button>
              <button type="button" (click)="copyCurlSnippets()" [disabled]="!runId">Copy curls</button>
              <button type="button" (click)="copyIncidentBundle()" [disabled]="copyBundleLoading">Copy incident bundle</button>
            </div>

            <div style="margin-top: 8px; font-family: monospace; color:#333; white-space: pre-wrap;">
{{ ticketHeaderPreview() }}
            </div>

            <div style="margin-top: 6px; font-family: monospace; color:#777; font-size: 12px;">UI rev: {{ uiRev }}</div>
          </div>

          <div style="margin-top: 10px; padding: 10px; border: 1px dashed #bbb; border-radius: 8px;">
            <div style="font-weight: 600; margin-bottom: 6px;">Dev: force fail step (once)</div>
            <div style="display:flex; gap: 10px; flex-wrap: wrap; align-items: center;">
              <button type="button" (click)="devFailStep('canary-migrate')" [disabled]="devFailLoading">Fail canary</button>
              <button type="button" (click)="devFailStep('wave1-migrate')" [disabled]="devFailLoading">Fail wave1</button>
              <div *ngIf="devFailLoading">Requesting...</div>
              <div *ngIf="devFailError" style="color:#b00020;">{{ devFailError }}</div>
            </div>
          </div>

          <h4>Steps</h4>
          <table style="width:100%; border-collapse: collapse;">
            <thead>
              <tr>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Step</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">State</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Attempt</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Duration</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Next retry</th>
                <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Last error</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let s of run.steps">
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ s.stepKey }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;"><span [ngStyle]="badgeStyle(s.state)">{{ s.state }}</span></td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ s.attempt }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ formatDuration(stepDurationMs(s)) }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ s.nextRetryAtUtc | date: 'medium' }}</td>
                <td style="border-bottom:1px solid #eee; padding: 6px;">{{ s.lastErrorCode }} {{ s.lastErrorMessage }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section style="margin-top: 16px; padding: 12px; border: 1px solid #ddd; border-radius: 8px;">
        <div style="display:flex; gap: 12px; align-items:center; flex-wrap: wrap;">
          <h3 style="margin:0;">Audit log</h3>
          <button type="button" (click)="applyAuditFilters()">Apply</button>
          <button type="button" (click)="clearAuditFilters()">Clear</button>
          <button type="button" (click)="loadAudit()">Refresh audit</button>
          <button type="button" (click)="auditOnlyUpgrades()">Only upgrade events</button>
          <button type="button" (click)="auditOnlyCurrentRun()" [disabled]="!run?.traceId">Only current run</button>
          <div *ngIf="auditLoading">Loading...</div>
          <div *ngIf="auditError" style="color:#b00020;">{{ auditError }}</div>
        </div>

        <div *ngIf="lastAuditRefreshedAt" style="margin-top: 8px; color:#444;">
          <span><b>Last refreshed</b>: {{ lastAuditRefreshedAt | date: 'medium' }}</span>
          <span> ({{ formatTimeAgo(lastAuditRefreshedAt) }} ago)</span>
        </div>

        <div style="margin-top: 10px; display:flex; gap: 12px; flex-wrap: wrap; align-items: end;">
          <label>
            Take
            <input type="number" [value]="auditTake" (input)="auditTake = toNumber(($any($event.target).value ?? 50), 50)" style="width: 90px;" />
          </label>

          <label>
            Actor
            <input [value]="auditActor" (input)="auditActor = (($any($event.target).value ?? '').trim())" placeholder="admin" style="min-width: 120px;" />
          </label>

          <label>
            Action contains
            <input [value]="auditActionContains" (input)="auditActionContains = (($any($event.target).value ?? '').trim())" placeholder="upgrade_" style="min-width: 180px;" />
          </label>

          <label>
            TraceId
            <input [value]="auditTraceId" (input)="auditTraceId = (($any($event.target).value ?? '').trim())" placeholder="..." style="min-width: 200px;" />
          </label>

          <label>
            Since (UTC)
            <input [value]="auditSinceUtc" (input)="auditSinceUtc = (($any($event.target).value ?? '').trim())" placeholder="2026-03-08T07:00:00Z" style="min-width: 240px;" />
          </label>
        </div>

        <div style="margin-top: 10px;" *ngIf="auditItems.length === 0 && !auditLoading">
          No audit items.
        </div>

        <table *ngIf="auditItems.length > 0" style="width:100%; border-collapse: collapse; margin-top: 10px;">
          <thead>
            <tr>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Time (UTC)</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Actor</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Action</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Target</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Trace</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Ops</th>
              <th style="text-align:left; border-bottom:1px solid #ddd; padding: 6px;">Details</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let a of auditItems">
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ a.timestampUtc | date: 'medium':'UTC' }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ a.actor }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ a.action }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">{{ a.target }}</td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <button type="button" (click)="auditTraceId = a.traceId; applyAuditFilters()" style="padding: 0; border: none; background: transparent; cursor: pointer;">
                  {{ shortId(a.traceId) }}
                </button>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px;">
                <div style="display:flex; gap: 8px; flex-wrap: wrap;">
                  <button type="button" (click)="copyAuditTraceId(a.traceId)" style="padding: 2px 6px;">Copy trace</button>
                  <button type="button" (click)="copyAuditCurlByTraceId(a.traceId)" style="padding: 2px 6px;">Copy curl</button>
                </div>
              </td>
              <td style="border-bottom:1px solid #eee; padding: 6px; font-family: monospace; max-width: 360px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" [title]="a.detailsJson ?? ''">
                <button type="button" (click)="copyAuditDetails(a.detailsJson)" [disabled]="!a.detailsJson" style="padding: 0; border: none; background: transparent; cursor: pointer; font-family: monospace;">
                  {{ a.detailsJson ?? '' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  `
})
export class UpgradePageComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);

  readonly uiRev = '2026-03-08.01';

  form = new FormGroup({
    targetVersion: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
  });

  devForm = new FormGroup({
    currentVersion: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    supportedVersion: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    releaseDateUtc: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    upgradeWindowDays: new FormControl<number>(60, { nonNullable: true, validators: [Validators.required] }),
  });

  runId = '';
  run: UpgradeRun | null = null;
  runLoading = false;
  runError = '';

  recentRuns: RecentUpgradeRun[] = [];
  recentLoading = false;
  recentError = '';

  startLoading = false;
  startError = '';
  lastRunId = '';

  retryLoading = false;
  cancelLoading = false;

  devLoading = false;
  devError = '';

  devFailLoading = false;
  devFailError = '';

  pollingEnabled = true;
  private pollHandle: number | null = null;
  private pollInFlight = false;
  private pollTick = 0;

  copyStatus = '';
  private copyStatusHandle: number | null = null;

  copyBundleLoading = false;

  clientTraceId = '';

  clearClientTraceId() {
    this.clientTraceId = '';
  }

  generateClientTraceId() {
    const anyCrypto = (globalThis as any).crypto;
    if (anyCrypto?.randomUUID)
      this.clientTraceId = anyCrypto.randomUUID();
    else
      this.clientTraceId = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }

  copyClientTraceHeaderArg() {
    const t = (this.clientTraceId ?? '').trim();
    if (!t) return;
    void this.copyText(`-H 'X-Trace-Id: ${t}'`);
  }

  private ticketHeaderTextUnified(args: {
    runId: string;
    run: UpgradeRun | null;
    status: InstallationStatus | null;
    observability: ObservabilitySnapshot | null;
    clientTraceId: string;
  }): string {
    const id = (args.runId ?? '').trim();
    const st = (args.run?.state ?? '').trim();
    const tgt = (args.run?.targetVersion ?? '').trim();
    const trace = (args.run?.traceId ?? '').trim();
    const clientTraceId = (args.clientTraceId ?? '').trim();
    const serverTime = (this.lastServerTimeUtcApplied ?? '').trim();

    const installationId = (args.status?.installationId ?? args.observability?.installationId ?? '').trim();
    const enforcementState = (args.status?.enforcementState ?? args.observability?.enforcementState ?? '').trim();
    const daysOutOfSupportRaw = args.status?.daysOutOfSupport ?? args.observability?.daysOutOfSupport;
    const daysOutOfSupport = daysOutOfSupportRaw === 0 || !!daysOutOfSupportRaw ? String(daysOutOfSupportRaw) : '';

    const headerArg = clientTraceId ? `-H 'X-Trace-Id: ${clientTraceId}'` : '';

    const parts: string[] = [];
    parts.push('=== UPGRADE DEBUG HEADER ===');
    if (installationId) parts.push(`installationId: ${installationId}`);
    if (enforcementState) parts.push(`enforcement: ${enforcementState} (daysOutOfSupport=${daysOutOfSupport || '?'})`);
    if (id) parts.push(`runId: ${id}`);
    if (st) parts.push(`state: ${st}`);
    if (tgt) parts.push(`target: ${tgt}`);
    if (trace) parts.push(`traceId: ${trace}`);
    if (serverTime) parts.push(`serverTimeUtc: ${serverTime}`);
    if (serverTime) parts.push(`clientOffset: ${this.serverNowOffsetMs}ms`);
    if (headerArg) parts.push(`curlHeader: ${headerArg}`);
    return parts.join('\n');
  }

  ticketHeaderPreview(): string {
    return this.ticketHeaderTextUnified({
      runId: this.runId,
      run: this.run,
      status: this.status,
      observability: this.observability,
      clientTraceId: this.clientTraceId,
    });
  }

  copyTicketHeader() {
    if (!this.run) return;
    void this.copyText(this.ticketHeaderPreview());
  }

  private shortTicketHeaderText(args: {
    runId: string;
    run: UpgradeRun | null;
    status: InstallationStatus | null;
    observability: ObservabilitySnapshot | null;
  }): string {
    const installationId = (args.status?.installationId ?? args.observability?.installationId ?? '').trim();
    const enforcementState = (args.status?.enforcementState ?? args.observability?.enforcementState ?? '').trim();
    const id = (args.runId ?? '').trim();
    const st = (args.run?.state ?? '').trim();
    const tgt = (args.run?.targetVersion ?? '').trim();
    const trace = (args.run?.traceId ?? '').trim();

    const parts: string[] = [];
    if (installationId) parts.push(`inst=${this.shortId(installationId)}`);
    if (enforcementState) parts.push(`enf=${enforcementState}`);
    if (id) parts.push(`run=${this.shortId(id)}`);
    if (st) parts.push(`st=${st}`);
    if (tgt) parts.push(`tgt=${tgt}`);
    if (trace) parts.push(`trace=${this.shortId(trace)}`);
    return parts.join(' | ');
  }

  copyShortTicketHeader() {
    if (!this.run) return;
    const t = this.shortTicketHeaderText({
      runId: this.runId,
      run: this.run,
      status: this.status,
      observability: this.observability,
    });
    if (!t) return;
    void this.copyText(t);
  }

  copyTicketMarkdown() {
    if (!this.run) return;

    const shortHeader = this.shortTicketHeaderText({
      runId: this.runId,
      run: this.run,
      status: this.status,
      observability: this.observability,
    });

    const fullHeader = this.ticketHeaderPreview();
    const traceHeader = this.clientTraceHeaderArg();
    const curls = this.curlSnippetsText();

    const lines: string[] = [];
    if (shortHeader) lines.push(`**${shortHeader}**`);
    lines.push('');
    lines.push('### Debug header');
    lines.push('```');
    lines.push(fullHeader);
    lines.push('```');
    lines.push('');
    if (traceHeader) {
      lines.push('### Trace header');
      lines.push('```');
      lines.push(traceHeader);
      lines.push('```');
      lines.push('');
    }
    if (curls) {
      lines.push('### Curl snippets');
      lines.push('```bash');
      lines.push(curls);
      lines.push('```');
      lines.push('');
    }

    void this.copyText(lines.join('\n'));
  }

  private clientTraceHeaderArg(): string {
    const t = (this.clientTraceId ?? '').trim();
    if (!t) return '';
    return `-H 'X-Trace-Id: ${t}'`;
  }

  private requestOptions(): { headers: HttpHeaders } | undefined {
    const t = (this.clientTraceId ?? '').trim();
    if (!t) return undefined;
    return { headers: new HttpHeaders({ 'X-Trace-Id': t }) };
  }

  lastRunRefreshedAt: Date | null = null;
  private nowTick = Date.now();
  private nowTickHandle: number | null = null;

  serverNowOffsetMs = 0;
  lastServerTimeUtcApplied = '';
  lastServerTimeUtcSource = '';

  get offsetMinutesAbs(): number {
    return Math.round(Math.abs(this.serverNowOffsetMs) / 60_000);
  }

  simulatedClientDriftMinutes = 0;
  private get simulatedClientDriftMs(): number {
    return Math.trunc(this.simulatedClientDriftMinutes) * 60_000;
  }

  private clientNowMs(): number {
    return Date.now() + this.simulatedClientDriftMs;
  }

  private applyServerTimeUtc(v: string | null | undefined, source: string) {
    const serverTime = this.parseDateUtc(v);
    if (!serverTime) return;
    this.serverNowOffsetMs = serverTime.getTime() - this.clientNowMs();
    this.lastServerTimeUtcApplied = serverTime.toISOString();
    this.lastServerTimeUtcSource = source;
  }

  private isActiveRunState(stateRaw: string | null | undefined): boolean {
    const st = (stateRaw ?? '').toLowerCase().trim();
    return st === 'pending' || st === 'running';
  }

  refreshAll() {
    this.loadStatus();
    this.loadObservability();
    this.loadQueue();
    this.loadAudit();
    if ((this.runId ?? '').trim())
      this.loadRun();
  }

  get isBlocked(): boolean {
    const s = this.status?.enforcementState;
    return s === 'soft_block' || s === 'hard_block';
  }

  get hasAnyActiveRun(): boolean {
    return (this.queueRuns ?? []).some(x => {
      const st = (x.state ?? '').toLowerCase();
      return st === 'pending' || st === 'running';
    });
  }

  get hasOtherActiveRun(): boolean {
    const id = (this.runId ?? '').trim();
    return (this.queueRuns ?? []).some(x => {
      const st = (x.state ?? '').toLowerCase();
      if (st !== 'pending' && st !== 'running') return false;
      if (!id) return true;
      return x.upgradeRunId !== id;
    });
  }

  get isCancelable(): boolean {
    const st = (this.run?.state ?? '').toLowerCase();
    return st === 'pending' || st === 'running';
  }

  ngOnInit() {
    this.loadQueue();
    this.loadStatus();
    this.loadRecentRuns();
    this.loadAudit();
    this.loadObservability();

    if (this.nowTickHandle === null)
      this.nowTickHandle = window.setInterval(() => this.nowTick = this.clientNowMs(), 1000);

    if (!this.runId)
      this.loadLatest(true);
  }

  ngOnDestroy() {
    this.stopPolling();
    if (this.nowTickHandle !== null) {
      window.clearInterval(this.nowTickHandle);
      this.nowTickHandle = null;
    }
  }

  private startPolling() {
    if (!this.pollingEnabled) return;
    if (this.pollHandle !== null) return;
    if (!this.isActiveRunState(this.run?.state)) return;
    this.pollTick = 0;
    this.pollHandle = window.setInterval(() => {
      if (!this.pollingEnabled) return;
      if (!this.runId) return;
      if (this.pollInFlight) return;
      this.pollTick++;
      this.pollInFlight = true;
      this.loadRun(true);
      if (this.pollTick % 3 === 0) {
        this.loadQueue();
        this.loadObservability();
      }
    }, 5000);
  }

  private stopPolling() {
    if (this.pollHandle !== null) {
      window.clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
    this.pollTick = 0;
  }

  togglePolling() {
    this.pollingEnabled = !this.pollingEnabled;
    if (this.pollingEnabled)
      this.startPolling();
    else
      this.stopPolling();
  }

  loadStatus() {
    this.statusLoading = true;
    this.statusError = '';

    this.http.get<InstallationStatus>('/api/admin/installation/status', this.requestOptions()).subscribe({
      next: (res) => {
        this.status = res;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'installation/status');
        this.statusLoading = false;
      },
      error: (e) => {
        this.status = null;
        this.statusLoading = false;
        this.statusError = e?.error?.message ?? e?.message ?? 'Failed to load status.';
      }
    });
  }

  loadRecentRuns() {
    this.recentLoading = true;
    this.recentError = '';

    this.http.get<ServerTimedResponse<RecentUpgradeRun>>('/api/admin/upgrade-runs/recent?take=20', this.requestOptions()).subscribe({
      next: (res) => {
        this.recentRuns = res.items ?? [];
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/recent');
        this.recentLoading = false;
      },
      error: (e) => {
        this.recentRuns = [];
        this.recentLoading = false;
        this.recentError = e?.error?.message ?? e?.message ?? 'Failed to load recent runs.';
      }
    });
  }

  onSelectRecent(id: string) {
    this.runId = (id ?? '').trim();
    if (!this.runId) {
      this.run = null;
      this.stopPolling();
      return;
    }
    this.loadRun();
  }

  loadLatest(silent = false) {
    if (!silent) {
      this.runLoading = true;
      this.runError = '';
    }

    this.http.get<ServerTimedSingleResponse<UpgradeRun>>('/api/admin/upgrade-runs/latest', this.requestOptions()).subscribe({
      next: (res) => {
        this.run = res;
        this.runId = (res?.upgradeRunId ?? '').trim();
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/latest');
        this.lastRunRefreshedAt = this.serverNowDate();
        this.pollInFlight = false;

        if (!this.runId) {
          this.runLoading = false;
          this.stopPolling();
          return;
        }

        this.runLoading = false;
        if (this.isActiveRunState(this.run?.state))
          this.startPolling();
        else
          this.stopPolling();
      },
      error: (e) => {
        this.runLoading = false;
        this.pollInFlight = false;
        this.runError = e?.error?.message ?? e?.message ?? 'Failed to load latest run.';
      }
    });
  }

  loadRun(fromPoll = false) {
    const id = (this.runId ?? '').trim();
    if (!id) return;
    if (!fromPoll) {
      this.runLoading = true;
      this.runError = '';
    }

    this.http.get<ServerTimedSingleResponse<UpgradeRun>>(`/api/admin/upgrade-runs/${id}`, this.requestOptions()).subscribe({
      next: (res) => {
        const prevState = (this.run?.state ?? '').toLowerCase().trim();
        this.run = res;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/get');
        this.lastRunRefreshedAt = this.serverNowDate();
        this.runLoading = false;
        this.pollInFlight = false;
        const nextState = (this.run?.state ?? '').toLowerCase().trim();
        if (this.isActiveRunState(nextState))
          this.startPolling();
        else
          this.stopPolling();

        if (this.isActiveRunState(prevState) && !this.isActiveRunState(nextState)) {
          this.loadQueue();
          this.loadObservability();
          this.loadRecentRuns();
          this.loadAudit();
        }
      },
      error: (e) => {
        this.run = null;
        this.runLoading = false;
        this.pollInFlight = false;
        this.runError = e?.error?.message ?? e?.message ?? 'Failed to load run.';
      }
    });
  }

  loadAudit() {
    this.auditLoading = true;
    this.auditError = '';
    const url = this.buildAuditUrl();

    this.http.get<ServerTimedResponse<AuditLogItem>>(url, this.requestOptions()).subscribe({
      next: (res) => {
        this.auditItems = res.items ?? [];
        this.lastAuditRefreshedAt = this.serverNowDate();
        this.applyServerTimeUtc(res?.serverTimeUtc, 'audit');
        this.auditLoading = false;
      },
      error: (e) => {
        this.auditItems = [];
        this.auditLoading = false;
        this.auditError = e?.error?.message ?? e?.message ?? 'Failed to load audit.';
      }
    });
  }

  startUpgrade() {
    if (this.isBlocked) return;
    const targetVersion = (this.form.controls.targetVersion.value ?? '').trim();
    if (!targetVersion) return;

    this.startLoading = true;
    this.startError = '';
    this.lastRunId = '';

    this.http.post<StartUpgradeResponse>('/api/admin/upgrade-runs', { targetVersion }, this.requestOptions()).subscribe({
      next: (res) => {
        this.startLoading = false;
        this.run = null;
        this.runId = (res?.upgradeRunId ?? '').trim();
        this.lastRunId = this.runId;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/start');
        this.lastRunRefreshedAt = this.serverNowDate();
        this.loadQueue();
        this.loadRecentRuns();
        this.loadAudit();
        this.loadRun();
      },
      error: (e) => {
        this.startLoading = false;
        this.startError = e?.error?.message ?? e?.message ?? 'Start failed.';
      }
    });
  }

  devSetInstallation() {
    this.devLoading = true;
    this.devError = '';

    const body = {
      currentVersion: this.devForm.controls.currentVersion.value,
      supportedVersion: this.devForm.controls.supportedVersion.value,
      releaseDateUtc: this.devForm.controls.releaseDateUtc.value,
      upgradeWindowDays: this.devForm.controls.upgradeWindowDays.value,
    };

    this.http.post<{ serverTimeUtc?: string; status: string }>('/api/admin/installation/dev-set', body, this.requestOptions()).subscribe({
      next: (res) => {
        this.devLoading = false;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'installation/dev-set');
        this.loadStatus();
        this.loadObservability();
        this.loadQueue();
        this.loadAudit();
      },
      error: (e) => {
        this.devLoading = false;
        this.devError = e?.error?.message ?? e?.message ?? 'Dev set failed.';
      }
    });
  }

  presetOk() {
    this.devForm.patchValue({ currentVersion: '0.1.0', supportedVersion: '0.1.0', upgradeWindowDays: 60 });
  }

  presetWarn() {
    this.devForm.patchValue({ currentVersion: '0.1.0', supportedVersion: '0.2.0', upgradeWindowDays: 60 });
  }

  presetSoftBlock() {
    this.devForm.patchValue({ currentVersion: '0.1.0', supportedVersion: '0.3.0', upgradeWindowDays: 60 });
  }

  presetHardBlock() {
    this.devForm.patchValue({ currentVersion: '0.1.0', supportedVersion: '1.0.0', upgradeWindowDays: 60 });
  }

  setSimulatedClientDriftMinutes(mins: number) {
    this.simulatedClientDriftMinutes = Math.trunc(mins || 0);
    this.nowTick = this.clientNowMs();

    this.applyServerTimeUtc(this.status?.serverTimeUtc, 'installation/status (cached)');

    this.loadStatus();
  }

  get clockDriftWarning(): string {
    const abs = Math.abs(this.serverNowOffsetMs);
    if (abs < 120_000) return '';
    const dir = this.serverNowOffsetMs > 0 ? 'behind' : 'ahead of';
    const mins = Math.round(abs / 60_000);
    return `Client clock is ~${mins}m ${dir} server time. 'ago' values are server-calibrated.`;
  }

  private parseDateUtc(v: string | null | undefined): Date | null {
    if (!v) return null;
    const dt = new Date(v);
    if (Number.isNaN(dt.getTime())) return null;
    return dt;
  }

  private serverNowMs(): number {
    return this.nowTick + this.serverNowOffsetMs;
  }

  private serverNowDate(): Date {
    return new Date(this.serverNowMs());
  }

  toNumber(v: unknown, fallback: number): number {
    const n = Number(v);
    return Number.isFinite(n) ? n : fallback;
  }

  private setCopyStatus(msg: string) {
    this.copyStatus = msg;
    if (this.copyStatusHandle !== null)
      window.clearTimeout(this.copyStatusHandle);
    this.copyStatusHandle = window.setTimeout(() => this.copyStatus = '', 2500);
  }

  private async copyText(text: string) {
    try {
      await navigator.clipboard.writeText(text);
      this.setCopyStatus('Copied to clipboard.');
    } catch {
      const el = document.createElement('textarea');
      el.value = text;
      el.style.position = 'fixed';
      el.style.opacity = '0';
      document.body.appendChild(el);
      el.focus();
      el.select();
      document.execCommand('copy');
      document.body.removeChild(el);
      this.setCopyStatus('Copied to clipboard.');
    }
  }

  copyRunId() {
    const id = (this.runId ?? '').trim();
    if (!id) return;
    void this.copyText(id);
  }

  copyTraceId() {
    const t = (this.run?.traceId ?? '').trim();
    if (!t) return;
    void this.copyText(t);
  }

  private curlSnippetsText(): string {
    return curlSnippetsTextImpl({
      runId: this.runId,
      traceId: this.run?.traceId,
      clientTraceId: this.clientTraceId,
      buildCurlSnippetsText,
    });
  }

  copyCurlSnippets() {
    const t = this.curlSnippetsText();
    if (!t) return;
    void this.copyText(t);
  }

  copyAuditTraceId(traceId: string) {
    const t = (traceId ?? '').trim();
    if (!t) return;
    void this.copyText(t);
  }

  copyAuditCurlByTraceId(traceId: string) {
    const t = (traceId ?? '').trim();
    if (!t) return;
    const clientTrace = (this.clientTraceId ?? '').trim();
    const headerArg = clientTrace ? ` -H 'X-Trace-Id: ${clientTrace}'` : '';
    const take = this.auditTake || 50;
    const url = `http://localhost:5002/api/admin/audit?take=${encodeURIComponent(String(take))}&traceId=${encodeURIComponent(t)}`;
    void this.copyText(`curl -sS "${url}"${headerArg}`);
  }

  copyAuditDetails(detailsJson: string | null) {
    const t = (detailsJson ?? '').trim();
    if (!t) return;
    void this.copyText(t);
  }

  async copyIncidentBundle() {
    if (this.copyBundleLoading) return;

    this.copyBundleLoading = true;

    try {
      const opts = this.requestOptions();
      const runId = (this.runId ?? '').trim();

      const capturedAtUtc = new Date(this.clientNowMs()).toISOString();

      const output = await buildIncidentBundleImpl({
        get: {
          auditTake: this.auditTake || 50,
          clientTraceId: (this.clientTraceId ?? '').trim(),
          runId,
        },
        time: {
          capturedAtUtc,
          timeDiagnostics: {
            simulatedClientDriftMinutes: this.simulatedClientDriftMinutes,
            lastServerTimeUtcApplied: this.lastServerTimeUtcApplied || null,
            lastServerTimeUtcSource: this.lastServerTimeUtcSource || null,
            serverNowOffsetMs: this.serverNowOffsetMs,
            clockDriftWarning: this.clockDriftWarning || null,
          },
        },
        http: {
          fetchRun: (id: string) => firstValueFrom(this.http.get<ServerTimedSingleResponse<UpgradeRun>>(`/api/admin/upgrade-runs/${id}`, opts)),
          fetchStatus: () => firstValueFrom(this.http.get<InstallationStatus>('/api/admin/installation/status', opts)),
          fetchQueue: () => firstValueFrom(this.http.get<ServerTimedResponse<QueueUpgradeRun>>('/api/admin/upgrade-runs/queue', opts)),
          fetchObservability: () => firstValueFrom(this.http.get<ObservabilitySnapshot>('/api/admin/observability', opts)),
          fetchAudit: (auditUrl: string) => firstValueFrom(this.http.get<ServerTimedResponse<AuditLogItem>>(auditUrl, opts)),
        },
        build: {
          auditPanelSnapshot: () => undefined,
          headerTextUnified: (a: any) => this.ticketHeaderTextUnified(a as any),
        }
      });

      await this.copyText(output.text);
    } catch (e: any) {
      this.setCopyStatus(e?.message ? `Bundle failed: ${e.message}` : 'Bundle failed.');
    } finally {
      this.copyBundleLoading = false;
    }
  }

  formatDuration(ms: number | null): string {
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

  queueRunDurationMs(q: QueueUpgradeRun): number | null {
    const started = this.parseDateUtc(q.startedAtUtc);
    if (!started) return null;
    const nowServerMs = this.nowTick + this.serverNowOffsetMs;
    return nowServerMs - started.getTime();
  }

  recentRunDurationMs(r: RecentUpgradeRun): number | null {
    const started = this.parseDateUtc(r.startedAtUtc);
    if (!started) return null;
    const finished = this.parseDateUtc(r.finishedAtUtc);
    const nowServerMs = this.nowTick + this.serverNowOffsetMs;
    const endMs = finished ? finished.getTime() : nowServerMs;
    return endMs - started.getTime();
  }

  runDurationMs(run: UpgradeRun): number | null {
    const started = this.parseDateUtc(run.startedAtUtc);
    if (!started) return null;
    const finished = this.parseDateUtc(run.finishedAtUtc);
    const nowServerMs = this.nowTick + this.serverNowOffsetMs;
    const endMs = finished ? finished.getTime() : nowServerMs;
    return endMs - started.getTime();
  }

  stepDurationMs(step: UpgradeRunStep): number | null {
    const started = this.parseDateUtc(step.startedAtUtc);
    if (!started) return null;
    const finished = this.parseDateUtc(step.finishedAtUtc);
    const nowServerMs = this.nowTick + this.serverNowOffsetMs;
    const endMs = finished ? finished.getTime() : nowServerMs;
    return endMs - started.getTime();
  }

  formatTimeAgo(dt: Date): string {
    const nowServer = this.nowTick + this.serverNowOffsetMs;
    const ms = nowServer - dt.getTime();
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

  badgeStyle(stateRaw: string): Record<string, string> {
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

  shortId(id: string): string {
    const v = (id ?? '').trim();
    if (!v) return '';
    return v.length <= 8 ? v : v.slice(0, 8);
  }

  loadQueue() {
    this.queueLoading = true;
    this.queueError = '';

    this.http.get<ServerTimedResponse<QueueUpgradeRun>>('/api/admin/upgrade-runs/queue', this.requestOptions()).subscribe({
      next: (res) => {
        this.queueRuns = res.items ?? [];
        this.lastQueueRefreshedAt = this.serverNowDate();

        this.applyServerTimeUtc(res.serverTimeUtc, 'upgrade-runs/queue');

        this.queueLoading = false;
      },
      error: (e) => {
        this.queueRuns = [];
        this.queueLoading = false;
        this.queueError = e?.error?.message ?? e?.message ?? 'Failed to load queue.';
      }
    });
  }

  loadObservability() {
    this.observabilityLoading = true;
    this.observabilityError = '';

    this.http.get<ObservabilitySnapshot>('/api/admin/observability', this.requestOptions()).subscribe({
      next: (res) => {
        this.observability = res;
        this.lastObservabilityRefreshedAt = this.serverNowDate();
        this.applyServerTimeUtc(res?.serverTimeUtc, 'observability');
        this.observabilityLoading = false;
      },
      error: (e) => {
        this.observability = null;
        this.observabilityLoading = false;
        this.observabilityError = e?.error?.message ?? e?.message ?? 'Failed to load observability snapshot.';
      }
    });
  }

  cancelRun() {
    if (this.isBlocked) {
      this.runError = 'Upgrade operations are blocked until the installation is upgraded.';
      return;
    }
    if (!this.runId) return;

    this.cancelLoading = true;
    this.runError = '';

    this.http.post<{ serverTimeUtc?: string; status: string }>(`/api/admin/upgrade-runs/${this.runId}/cancel`, {}, this.requestOptions()).subscribe({
      next: (res) => {
        this.cancelLoading = false;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/cancel');
        this.loadRun();
        this.loadRecentRuns();
        this.loadQueue();
        this.loadAudit();
      },
      error: (e) => {
        this.cancelLoading = false;
        this.runError = e?.error?.message ?? e?.message ?? 'Cancel failed.';
      }
    });
  }

  recentLabel(r: RecentUpgradeRun): string {
    const started = r.startedAtUtc ? new Date(r.startedAtUtc).toLocaleString() : '—';
    const finished = r.finishedAtUtc ? new Date(r.finishedAtUtc).toLocaleString() : '—';
    const dur = this.formatDuration(this.recentRunDurationMs(r));
    return `${r.targetVersion} | ${r.state} | ${this.shortId(r.upgradeRunId)} | ${dur} | ${started} → ${finished}`;
  }

  status: InstallationStatus | null = null;
  statusLoading = false;
  statusError = '';

  queueRuns: QueueUpgradeRun[] = [];
  queueLoading = false;
  queueError = '';

  observability: ObservabilitySnapshot | null = null;
  observabilityLoading = false;
  observabilityError = '';
  lastObservabilityRefreshedAt: Date | null = null;

  auditItems: AuditLogItem[] = [];
  auditLoading = false;
  auditError = '';

  lastQueueRefreshedAt: Date | null = null;
  lastAuditRefreshedAt: Date | null = null;

  auditTake = 50;
  auditActor = '';
  auditActionContains = '';
  auditTraceId = '';
  auditSinceUtc = '';

  private buildAuditUrl(): string {
    const params = new URLSearchParams();
    params.set('take', String(this.auditTake || 50));

    if (this.auditActor) params.set('actor', this.auditActor);
    if (this.auditActionContains) params.set('actionContains', this.auditActionContains);
    if (this.auditTraceId) params.set('traceId', this.auditTraceId);
    if (this.auditSinceUtc) params.set('sinceUtc', this.auditSinceUtc);

    return `/api/admin/audit?${params.toString()}`;
  }

  applyAuditFilters() {
    this.loadAudit();
  }

  clearAuditFilters() {
    this.auditTake = 50;
    this.auditActor = '';
    this.auditActionContains = '';
    this.auditTraceId = '';
    this.auditSinceUtc = '';
    this.loadAudit();
  }

  auditOnlyUpgrades() {
    this.auditActionContains = 'upgrade_';
    this.applyAuditFilters();
  }

  auditOnlyCurrentRun() {
    const t = (this.run?.traceId ?? '').trim();
    if (!t) return;
    this.auditTraceId = t;
    this.auditActionContains = '';
    this.applyAuditFilters();
  }

  retryRun() {
    if (this.isBlocked) {
      this.runError = 'Upgrade operations are blocked until the installation is upgraded.';
      return;
    }
    if (!this.runId) return;

    this.retryLoading = true;
    this.runError = '';

    this.http.post<{ serverTimeUtc?: string; status: string }>(`/api/admin/upgrade-runs/${this.runId}/retry`, {}, this.requestOptions()).subscribe({
      next: (res) => {
        this.retryLoading = false;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/retry');
        this.loadRun();
        this.loadRecentRuns();
        this.loadQueue();
        this.loadAudit();
      },
      error: (e) => {
        this.retryLoading = false;
        this.runError = e?.error?.message ?? e?.message ?? 'Retry failed.';
      }
    });
  }

  devFailStep(stepKey: string) {
    if (!this.runId) return;
    this.devFailLoading = true;
    this.devFailError = '';

    this.http.post<{ serverTimeUtc?: string; status: string }>(`/api/admin/upgrade-runs/${this.runId}/dev-fail-step`, { stepKey }, this.requestOptions()).subscribe({
      next: (res) => {
        this.devFailLoading = false;
        this.applyServerTimeUtc(res?.serverTimeUtc, 'upgrade-runs/dev-fail-step');
        this.loadRun();
        this.loadAudit();
      },
      error: (e) => {
        this.devFailLoading = false;
        this.devFailError = e?.error?.message ?? e?.message ?? 'Failed to request dev fail step.';
      }
    });
  }
}

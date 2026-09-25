import { StatusBadge } from './StatusBadge'
import { agentStages, explainStep, stageKey, stageStatusLabel } from '../../utils/agentExplain'
import type { WorkflowStepDto } from '../../types/api'

export function AgentTimeline({ steps, audience = 'customer' }: { steps: WorkflowStepDto[]; audience?: 'customer' | 'admin' }) {
  const grouped = agentStages.map((stage) => ({
    ...stage,
    items: steps.filter((step) => stageKey(step.agentRole) === stage.key),
  }))

  return (
    <div className="space-y-4">
      <p className="text-sm leading-6 text-[#6d6a64]">
        Here is what FixFlow did, in plain language. AI can recommend. You always make the final booking choice.
      </p>
      <ol className="space-y-4">
        {grouped.map((group, index) => {
          const done = group.items.length > 0
          const latest = group.items[group.items.length - 1]
          const explained = latest ? explainStep(latest) : null
          return (
            <li key={group.key} className="rounded-2xl border border-black/8 bg-white p-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-xs font-semibold tracking-wide text-[#c4a574]">Step {index + 1}</p>
                  <h3 className="mt-1 font-semibold text-slate-900">{group.title}</h3>
                  <p className="mt-1 text-sm text-[#6d6a64]">{group.purpose}</p>
                </div>
                <StatusBadge status={done ? 'COMPLETED' : 'PENDING'} />
              </div>
              {!done ? (
                <p className="mt-3 text-sm text-slate-500">This step has not run yet.</p>
              ) : (
                <div className="mt-3 rounded-xl bg-[#f4efe6] p-4 text-sm">
                  <p className="font-medium text-slate-800">{explained?.headline}</p>
                  {explained?.points.length ? (
                    <ul className="mt-2 list-disc space-y-1 pl-5 text-slate-700">
                      {explained.points.map((point) => (
                        <li key={point}>{point}</li>
                      ))}
                    </ul>
                  ) : null}
                  {audience === 'admin' ? (
                    <details className="mt-3 text-xs text-slate-500">
                      <summary className="cursor-pointer font-medium text-slate-600">Technical details</summary>
                      <dl className="mt-2 grid gap-1">
                        {group.items.map((step) => (
                          <div key={step.id}>
                            <dt>{step.inputSummary || step.toolName || stageStatusLabel(true)}</dt>
                            <dd className="break-all text-slate-400">{step.toolName ? `Tool ${step.toolName}` : null}</dd>
                          </div>
                        ))}
                      </dl>
                    </details>
                  ) : null}
                </div>
              )}
            </li>
          )
        })}
      </ol>
    </div>
  )
}

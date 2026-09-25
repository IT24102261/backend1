import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { workflowsApi } from '../../api/workflows'
import { AgentTimeline } from '../../components/ui/AgentTimeline'
import { Button } from '../../components/ui/Button'
import { ConfirmationModal } from '../../components/ui/ConfirmationModal'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, TextArea } from '../../components/ui/FormField'
import { LoadingSpinner } from '../../components/ui/LoadingSpinner'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { WorkflowDto, WorkflowStepDto } from '../../types/api'
import { formatDate, formatDuration } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminWorkflowDetailPage() {
  const { id } = useParams()
  const push = useToastStore((state) => state.push)
  const [workflow, setWorkflow] = useState<WorkflowDto | null>(null)
  const [steps, setSteps] = useState<WorkflowStepDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reason, setReason] = useState('')
  const [decision, setDecision] = useState<'approve' | 'reject' | 'revise' | null>(null)

  useEffect(() => {
    if (!id) return
    Promise.all([workflowsApi.get(id), workflowsApi.steps(id)])
      .then(([item, timeline]) => {
        setWorkflow(item)
        setSteps(timeline)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [id])

  if (loading) return <LoadingSpinner label="Loading workflow" />
  if (error) return <ErrorState message={error} />
  if (!workflow) return <ErrorState message="Workflow not found." />

  const duration =
    workflow.finishedAt != null
      ? new Date(workflow.finishedAt).getTime() - new Date(workflow.startedAt).getTime()
      : Date.now() - new Date(workflow.startedAt).getTime()

  return (
    <div className="space-y-5">
      <PageHeader
        title="Workflow details"
        description="What FixFlow did for this job, explained in everyday language."
        actions={
          <Link to="/admin/ai-workflows" className="text-sm font-medium text-[#c4a574] hover:text-[#171717]">
            Back to workflows
          </Link>
        }
      />
      <section className="rounded-2xl border border-black/8 bg-white p-5">
        <div className="flex flex-wrap items-center gap-3">
          <StatusBadge status={workflow.status} />
          <StatusBadge status={workflow.approvalStatus} />
        </div>
        <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-slate-400">Objective</dt>
            <dd className="font-medium">{workflow.objective}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Current step</dt>
            <dd>{workflow.currentStep}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Started</dt>
            <dd>{formatDate(workflow.startedAt)}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Duration</dt>
            <dd>{formatDuration(duration)}</dd>
          </div>
        </dl>
      </section>

      <AgentTimeline steps={steps} audience="admin" />

      <section className="rounded-2xl border border-black/8 bg-white p-5">
        <h2 className="font-semibold text-slate-900">Approval history</h2>
        <p className="mt-2 text-sm text-slate-600">
          Latest approval status: {workflow.approvalStatus}. Decisions are stored as structured approval records on the
          server.
        </p>
        <FormField label="Decision reason">
          <TextArea value={reason} onChange={(event) => setReason(event.target.value)} />
        </FormField>
        <div className="mt-4 flex flex-wrap gap-2">
          <Button onClick={() => setDecision('approve')}>Approve</Button>
          <Button variant="secondary" onClick={() => setDecision('revise')}>
            Revise
          </Button>
          <Button variant="danger" onClick={() => setDecision('reject')}>
            Reject
          </Button>
        </div>
      </section>

      <ConfirmationModal
        open={decision !== null}
        title="Confirm workflow decision"
        description="This records a structured approval against the workflow."
        busy={false}
        onClose={() => setDecision(null)}
        onConfirm={async () => {
          if (!id || !decision) return
          try {
            const updated =
              decision === 'approve'
                ? await workflowsApi.approve(id, reason)
                : decision === 'reject'
                  ? await workflowsApi.reject(id, reason)
                  : await workflowsApi.revise(id, reason)
            setWorkflow(updated)
            push('success', 'Workflow decision saved.')
            setDecision(null)
          } catch (err) {
            push('error', getApiError(err).error)
          }
        }}
      />
    </div>
  )
}

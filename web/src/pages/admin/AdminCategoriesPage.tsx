import { useEffect, useState, type FormEvent } from 'react'
import { categoriesApi } from '../../api/categories'
import { Button } from '../../components/ui/Button'
import { ConfirmationModal } from '../../components/ui/ConfirmationModal'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, SelectInput, TextArea, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { CategoryDto, CategoryRequirementDraft } from '../../types/api'
import { getApiError } from '../../utils/errors'

const evidenceTypes = ['IDENTITY', 'LICENSE', 'INSURANCE', 'CERTIFICATE', 'WORK_SAMPLE', 'OTHER']
const methods = ['MANUAL', 'AUTOMATED']

export function AdminCategoriesPage() {
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<CategoryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [editing, setEditing] = useState<CategoryDto | null>(null)
  const [removeId, setRemoveId] = useState<string | null>(null)
  const [requirements, setRequirements] = useState<CategoryRequirementDraft[]>([])
  const [selectedCategory, setSelectedCategory] = useState('')
  const [evidenceType, setEvidenceType] = useState('LICENSE')
  const [validationMethod, setValidationMethod] = useState('MANUAL')
  const [isRequired, setIsRequired] = useState(true)

  async function refresh() {
    setLoading(true)
    try {
      const items = await categoriesApi.list()
      setRows(items)
      setRequirements(
        items.flatMap((item) =>
          (item.requirements ?? []).map((rule) => ({
            id: rule.id,
            categoryId: item.id,
            evidenceType: rule.evidenceType,
            isRequired: rule.isRequired,
            validationMethod: rule.validationMethod,
            ruleVersion: rule.ruleVersion,
          })),
        ),
      )
      setError('')
    } catch (err) {
      setError(getApiError(err).error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void refresh()
  }, [])

  async function saveCategory(event: FormEvent) {
    event.preventDefault()
    if (!name.trim()) return
    try {
      const payload = { name: name.trim(), description, isActive: true }
      if (editing) await categoriesApi.update(editing.id, payload)
      else await categoriesApi.create(payload)
      push('success', editing ? 'Category updated.' : 'Category created.')
      setName('')
      setDescription('')
      setEditing(null)
      await refresh()
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  async function persistRequirements(next: CategoryRequirementDraft[]) {
    if (!selectedCategory) return
    setRequirements(next)
    await categoriesApi.replaceRequirements(
      selectedCategory,
      next
        .filter((item) => item.categoryId === selectedCategory)
        .map((item) => ({
          evidenceType: item.evidenceType,
          isRequired: item.isRequired,
          validationMethod: item.validationMethod,
        })),
    )
    await refresh()
  }

  async function addRequirement() {
    if (!selectedCategory) return
    await persistRequirements([
      ...requirements,
      {
        id: crypto.randomUUID(),
        categoryId: selectedCategory,
        evidenceType,
        isRequired,
        validationMethod,
        ruleVersion: '1.0',
      },
    ])
  }

  const columns: Column<CategoryDto>[] = [
    { key: 'name', header: 'Name', render: (row) => row.name },
    { key: 'description', header: 'Description', render: (row) => row.description || '—' },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.isActive ? 'ACTIVE' : 'CANCELLED'} /> },
    {
      key: 'actions',
      header: 'Actions',
      render: (row) => (
        <div className="flex gap-2">
          <button
            type="button"
            className="text-sm font-medium text-[#c4a574] hover:text-[#171717]"
            onClick={() => {
              setEditing(row)
              setName(row.name)
              setDescription(row.description ?? '')
              setSelectedCategory(row.id)
            }}
          >
            Edit
          </button>
          <button type="button" className="text-sm font-medium text-rose-600" onClick={() => setRemoveId(row.id)}>
            Deactivate
          </button>
        </div>
      ),
    },
  ]

  const categoryRequirements = requirements.filter((item) => item.categoryId === selectedCategory)

  return (
    <div className="space-y-5">
      <PageHeader title="Category management" description="Create and update service categories used for matching and verification." />
      {error ? <ErrorState message={error} /> : null}
      <form className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2" onSubmit={saveCategory}>
        <FormField label="Category name">
          <TextInput value={name} onChange={(event) => setName(event.target.value)} />
        </FormField>
        <FormField label="Description">
          <TextArea value={description} onChange={(event) => setDescription(event.target.value)} />
        </FormField>
        <div className="md:col-span-2">
          <Button type="submit">{editing ? 'Update category' : 'Create category'}</Button>
        </div>
      </form>

      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        page={1}
        pageSize={50}
        totalCount={rows.length}
        onPageChange={() => undefined}
      />

      <section className="rounded-2xl border border-black/8 bg-white p-5">
        <h2 className="font-semibold text-slate-900">Category verification requirements</h2>
        <p className="mt-1 text-sm text-slate-500">
          Evidence rules are stored on the category in PostgreSQL and used by Agent 1 when checking requirements.
        </p>
        <div className="mt-4 grid gap-3 md:grid-cols-4">
          <FormField label="Category">
            <SelectInput value={selectedCategory} onChange={(event) => setSelectedCategory(event.target.value)}>
              <option value="">Select category</option>
              {rows.map((row) => (
                <option key={row.id} value={row.id}>
                  {row.name}
                </option>
              ))}
            </SelectInput>
          </FormField>
          <FormField label="Evidence type">
            <SelectInput value={evidenceType} onChange={(event) => setEvidenceType(event.target.value)}>
              {evidenceTypes.map((item) => (
                <option key={item}>{item}</option>
              ))}
            </SelectInput>
          </FormField>
          <FormField label="Validation method">
            <SelectInput value={validationMethod} onChange={(event) => setValidationMethod(event.target.value)}>
              {methods.map((item) => (
                <option key={item}>{item}</option>
              ))}
            </SelectInput>
          </FormField>
          <FormField label="Required">
            <SelectInput value={isRequired ? 'yes' : 'no'} onChange={(event) => setIsRequired(event.target.value === 'yes')}>
              <option value="yes">Required</option>
              <option value="no">Optional</option>
            </SelectInput>
          </FormField>
        </div>
        <div className="mt-3">
          <Button onClick={addRequirement}>Add requirement</Button>
        </div>
        <ul className="mt-4 space-y-2">
          {categoryRequirements.map((item) => (
            <li key={item.id} className="flex items-center justify-between bg-[#f4efe6] px-4 py-3 text-sm">
              <span>
                {item.evidenceType} · {item.validationMethod} · {item.isRequired ? 'Required' : 'Optional'}
              </span>
              <button
                type="button"
                className="text-rose-600"
                onClick={() => void persistRequirements(requirements.filter((row) => row.id !== item.id))}
              >
                Remove
              </button>
            </li>
          ))}
        </ul>
      </section>

      <ConfirmationModal
        open={Boolean(removeId)}
        title="Deactivate category"
        description="The category will be marked inactive. Existing requests keep their historical category."
        tone="danger"
        confirmLabel="Deactivate"
        onClose={() => setRemoveId(null)}
        onConfirm={async () => {
          if (!removeId) return
          try {
            await categoriesApi.remove(removeId)
            push('success', 'Category deactivated.')
            setRemoveId(null)
            await refresh()
          } catch (err) {
            push('error', getApiError(err).error)
          }
        }}
      />
    </div>
  )
}

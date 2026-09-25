import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { adminApi } from '../../api/admin'
import { categoriesApi } from '../../api/categories'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { FormField, SelectInput, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import { useToastStore } from '../../store/toastStore'
import type { AdminUserDto, CategoryDto } from '../../types/api'
import { formatDate } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminUsersPage() {
  const query = usePagedQuery()
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<AdminUserDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [role, setRole] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [phone, setPhone] = useState('')
  const [createRole, setCreateRole] = useState<'CUSTOMER' | 'TECHNICIAN'>('CUSTOMER')
  const [address, setAddress] = useState('')
  const [serviceArea, setServiceArea] = useState('Jaffna')
  const [categoryId, setCategoryId] = useState('')
  const [categories, setCategories] = useState<CategoryDto[]>([])

  function load() {
    setLoading(true)
    adminApi
      .users({ ...query.params, role: role || undefined })
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(load, [query.page, query.search, query.status, role])

  useEffect(() => {
    categoriesApi
      .list()
      .then((items) => setCategories(items.filter((item) => item.isActive)))
      .catch(() => undefined)
  }, [])

  async function create(event: FormEvent) {
    event.preventDefault()
    try {
      await adminApi.createUser({
        email,
        password,
        displayName,
        phone: phone || undefined,
        role: createRole,
        address: createRole === 'TECHNICIAN' ? address || undefined : undefined,
        serviceArea: createRole === 'TECHNICIAN' ? serviceArea : undefined,
        categoryId: createRole === 'TECHNICIAN' && categoryId ? categoryId : undefined,
      })
      push('success', `${createRole === 'TECHNICIAN' ? 'Technician' : 'Customer'} account created.`)
      setEmail('')
      setPassword('')
      setDisplayName('')
      setPhone('')
      load()
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  const columns = useMemo<Column<AdminUserDto>[]>(
    () => [
      { key: 'displayName', header: 'Name', render: (row) => row.displayName },
      { key: 'email', header: 'Email', render: (row) => row.email },
      { key: 'role', header: 'Role', render: (row) => <StatusBadge status={row.role} /> },
      { key: 'phone', header: 'Phone', render: (row) => row.phone || '—' },
      { key: 'address', header: 'Address', render: (row) => row.address || '—' },
      { key: 'requestedCategory', header: 'Trade', render: (row) => row.requestedCategory || row.serviceArea || '—' },
      { key: 'loginStatus', header: 'Login', render: (row) => <StatusBadge status={row.loginStatus} /> },
      { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
      {
        key: 'action',
        header: 'Action',
        render: (row) =>
          row.role === 'ADMIN' ? (
            <span className="text-xs text-[#9a968e]">Protected</span>
          ) : row.loginStatus === 'PENDING' ? (
            <div className="flex flex-wrap gap-2">
              <Button
                onClick={async () => {
                  try {
                    await adminApi.setUserActive(row.id, true)
                    push('success', 'Technician login accepted.')
                    load()
                  } catch (err) {
                    push('error', getApiError(err).error)
                  }
                }}
              >
                Accept login
              </Button>
              <Button
                variant="danger"
                onClick={async () => {
                  try {
                    await adminApi.setUserActive(row.id, false)
                    push('success', 'Technician login left rejected.')
                    load()
                  } catch (err) {
                    push('error', getApiError(err).error)
                  }
                }}
              >
                Reject
              </Button>
            </div>
          ) : (
            <Button
              variant="ghost"
              onClick={async () => {
                try {
                  await adminApi.setUserActive(row.id, !row.isActive)
                  push('success', row.isActive ? 'User deactivated.' : 'User reactivated.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              {row.isActive ? 'Deactivate' : 'Activate'}
            </Button>
          ),
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader title="Users" description="Accept or reject technician login requests, and create customer or technician accounts." />
      <form className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2" onSubmit={create}>
        <FormField label="Full name">
          <TextInput value={displayName} onChange={(event) => setDisplayName(event.target.value)} required />
        </FormField>
        <FormField label="Email">
          <TextInput type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
        </FormField>
        <FormField label="Password">
          <TextInput type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
        </FormField>
        <FormField label="Phone">
          <TextInput value={phone} onChange={(event) => setPhone(event.target.value)} />
        </FormField>
        <FormField label="Account type">
          <SelectInput value={createRole} onChange={(event) => setCreateRole(event.target.value as 'CUSTOMER' | 'TECHNICIAN')}>
            <option value="CUSTOMER">Customer</option>
            <option value="TECHNICIAN">Technician</option>
          </SelectInput>
        </FormField>
        {createRole === 'TECHNICIAN' ? (
          <>
            <FormField label="Address">
              <TextInput value={address} onChange={(event) => setAddress(event.target.value)} />
            </FormField>
            <FormField label="Trade">
              <SelectInput value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
                <option value="">Select a trade</option>
                {categories.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </SelectInput>
            </FormField>
            <FormField label="Service area" hint="Admin-created technicians can sign in immediately. Category verification is still required before quoting.">
              <SelectInput value={serviceArea} onChange={(event) => setServiceArea(event.target.value)}>
                <option value="Jaffna">Jaffna</option>
                <option value="Colombo">Colombo</option>
                <option value="Kandy">Kandy</option>
                <option value="Negombo">Negombo</option>
              </SelectInput>
            </FormField>
          </>
        ) : null}
        <div className="flex items-end">
          <Button type="submit">Add {createRole === 'TECHNICIAN' ? 'technician' : 'customer'}</Button>
        </div>
      </form>
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search name or email" />
        <SelectFilter
          label="Role"
          value={role}
          onChange={setRole}
          options={[
            { value: '', label: 'All roles' },
            { value: 'CUSTOMER', label: 'Customers' },
            { value: 'TECHNICIAN', label: 'Technicians' },
            { value: 'ADMIN', label: 'Admins' },
          ]}
        />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'PENDING', label: 'Pending login' },
            { value: 'ACTIVE', label: 'Active' },
            { value: 'INACTIVE', label: 'Inactive' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No users"
        emptyDescription="No users match these filters."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={total}
        onPageChange={query.setPage}
      />
    </div>
  )
}

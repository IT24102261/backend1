import { useState } from 'react'

export function usePagedQuery(pageSize = 10) {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [sortBy, setSortBy] = useState('createdAt')
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc')

  function toggleSort(key: string) {
    if (sortBy === key) {
      setSortDir((current) => (current === 'asc' ? 'desc' : 'asc'))
      return
    }
    setSortBy(key)
    setSortDir('asc')
  }

  return {
    page,
    setPage,
    pageSize,
    search,
    setSearch: (value: string) => {
      setSearch(value)
      setPage(1)
    },
    status,
    setStatus: (value: string) => {
      setStatus(value)
      setPage(1)
    },
    sortBy,
    sortDir,
    toggleSort,
    params: {
      page,
      pageSize,
      search: search || undefined,
      status: status || undefined,
      sortBy,
      sortDir,
    },
  }
}

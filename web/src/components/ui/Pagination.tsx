export function Pagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
}: {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex flex-col gap-3 border-t border-black/5 bg-[#faf7f1] px-4 py-3 text-sm text-[#6d6a64] sm:flex-row sm:items-center sm:justify-between">
      <p>
        Showing {from}-{to} of {totalCount}
      </p>
      <div className="flex gap-2">
        <button
          type="button"
          className="border border-[#171717] px-3 py-1.5 text-sm font-medium disabled:opacity-40"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Previous
        </button>
        <button
          type="button"
          className="border border-[#171717] px-3 py-1.5 text-sm font-medium disabled:opacity-40"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}

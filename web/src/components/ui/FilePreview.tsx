import { FileText } from 'lucide-react'
import { formatDate } from '../../utils/format'

export function FilePreview({
  name,
  mimeType,
  uploadedAt,
}: {
  name: string
  mimeType?: string | null
  uploadedAt?: string | null
}) {
  return (
    <div className="flex items-center gap-3 border border-black/8 bg-[#f4efe6] px-3 py-2">
      <FileText size={18} className="text-[#c4a574]" />
      <div className="min-w-0">
        <p className="truncate text-sm font-medium text-slate-800">{name}</p>
        <p className="text-xs text-slate-500">
          {mimeType ?? 'file'}
          {uploadedAt ? ` · ${formatDate(uploadedAt)}` : ''}
        </p>
      </div>
    </div>
  )
}

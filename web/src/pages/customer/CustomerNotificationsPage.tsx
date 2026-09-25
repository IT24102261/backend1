import { useEffect, useState } from 'react'
import { notificationsApi } from '../../api/notifications'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { useAuth } from '../../hooks/useAuth'
import type { NotificationDto } from '../../types/api'
import { formatDate } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function CustomerNotificationsPage() {
  const role = useAuth((state) => state.user?.role)
  const [rows, setRows] = useState<NotificationDto[]>([])
  const [error, setError] = useState('')
  const isTechnician = String(role ?? '').toUpperCase() === 'TECHNICIAN'

  function load() {
    notificationsApi
      .list()
      .then(setRows)
      .catch((err) => setError(getApiError(err).error))
  }

  useEffect(load, [])

  return (
    <div className="space-y-5">
      <PageHeader
        title="Notifications"
        description={
          isTechnician
            ? 'Customer reviews, complaints, and other alerts for your jobs appear here.'
            : 'Alerts for your account, including reviews, complaints, and admin replies.'
        }
      />
      {error ? <ErrorState message={error} /> : null}
      {rows.length === 0 ? (
        <EmptyState
          title="No notifications"
          description={isTechnician ? 'When a customer reviews your work or files a complaint, the alert appears here.' : 'Submit a request or receive a quote to generate alerts.'}
        />
      ) : (
        <ul className="space-y-3">
          {rows.map((item) => (
            <li key={item.id} className="rounded-2xl border border-black/8 bg-white p-4">
              <div className="flex items-center justify-between gap-3">
                <h3 className="font-semibold">{item.title}</h3>
                {!item.isRead ? (
                  <Button
                    variant="ghost"
                    onClick={async () => {
                      await notificationsApi.markRead(item.id)
                      load()
                    }}
                  >
                    Mark read
                  </Button>
                ) : null}
              </div>
              <p className="mt-1 text-sm text-slate-600">{item.message}</p>
              <p className="mt-2 text-xs text-slate-400">{formatDate(item.createdAt)}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

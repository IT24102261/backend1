import { AuthProvider } from './context/AuthContext'
import { Toast } from './components/ui/Toast'
import { AppRoutes } from './routes/AppRoutes'

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
      <Toast />
    </AuthProvider>
  )
}

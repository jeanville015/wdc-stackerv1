import './App.css'

import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthProvider'
import ProtectedRoute from './components/ProtectedRoute'
import AppShell from './components/AppShell'
import LoginPage from './pages/LoginPage'
import HomePage from './pages/HomePage'
import ConfigPage from './pages/ConfigPage'

function App() {
    return (
        <AuthProvider>
            <BrowserRouter basename="/WDC_STACKER_FGI">
                <Routes>
                    {/* Public */}
                    <Route path="/login" element={<LoginPage />} />

                    {/* Protected */}
                    <Route
                        element={
                            <ProtectedRoute>
                                <AppShell />
                            </ProtectedRoute>
                        }
                    >
                        <Route path="/" element={<HomePage />} />
                        <Route
                            path="/config"
                            element={
                                <ProtectedRoute requireConfigurationAccess>
                                    <ConfigPage />
                                </ProtectedRoute>
                            }
                        />
                    </Route>

                    {/* Catch-all → root (ProtectedRoute redirects to /login if not authenticated) */}
                    <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
            </BrowserRouter>
        </AuthProvider>
    )
}

export default App

import { useState } from 'react'
import './App.css' 

import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import ConfigPage from './pages/ConfigPage'

function App() {

    return (
        <BrowserRouter>
            <Routes>
                {/* Temporarily redirect root to /config for testing */}
                <Route path="/" element={<Navigate to="/config" replace />} />
                <Route path="/config" element={<ConfigPage />} />
            </Routes>
        </BrowserRouter>
    )

    const [isPaneOpen, setIsPaneOpen] = useState(true)
    const [searchValue, setSearchValue] = useState('')
    const [fields, setFields] = useState({
        jobName: '',
        binName: '',
        buildCode: '',
        className: '',
        experiment: '',
        productName: '',
    })

    const handleFieldChange = (key: string, value: string) => {
        setFields((prev) => ({ ...prev, [key]: value }))
    }

    const handleAssign = () => {
        console.log('Assign clicked', fields)
    }

    const rackData = [
        {
            id: 'rack-1',
            label: 'Rack No. 1',
            rows: [
                { slot: 'A1', item: 'Unit Alpha', status: 'Active', weight: '1.2 kg' },
                { slot: 'A2', item: 'Unit Beta', status: 'Idle', weight: '0.8 kg' },
                { slot: 'B1', item: 'Unit Gamma', status: 'Active', weight: '2.1 kg' },
                { slot: 'B2', item: 'Unit Delta', status: 'Error', weight: '1.5 kg' },
                { slot: 'C1', item: 'Unit Epsilon', status: 'Idle', weight: '0.6 kg' },
            ],
        },
        {
            id: 'rack-2',
            label: 'Rack No. 2',
            rows: [
                { slot: 'D1', item: 'Unit Zeta', status: 'Active', weight: '1.9 kg' },
                { slot: 'D2', item: 'Unit Eta', status: 'Idle', weight: '1.1 kg' },
                { slot: 'E1', item: 'Unit Theta', status: 'Active', weight: '2.4 kg' },
                { slot: 'E2', item: 'Unit Iota', status: 'Error', weight: '0.7 kg' },
                { slot: 'F1', item: 'Unit Kappa', status: 'Idle', weight: '1.3 kg' },
            ],
        },
    ]

    const statusBadge: Record<string, string> = {
        Active: 'bg-success',
        Idle: 'bg-secondary',
        Error: 'bg-danger',
    }

    return (
        <div className="app-shell">

            {/* ── HEADER ── */}
            <header className="app-header navbar bg-white border-bottom px-3 gap-3 shadow-sm">
                {/* Brand */}
                <span className="navbar-brand d-flex align-items-center gap-2 mb-0 fw-bold text-dark fs-5">
                    <span className="text-primary">⬡</span>
                    RackManager
                </span>

                {/* Session ID */}
                <div className="d-flex align-items-center gap-2 flex-grow-1 justify-content-center">
                    <label htmlFor="session-id" className="text-secondary small mb-0 text-nowrap">
                        Session ID
                    </label>
                    <input
                        id="session-id"
                        type="text"
                        className="form-control form-control-sm"
                        style={{ maxWidth: 240 }}
                        placeholder="e.g. SES-20260506-001"
                    />
                </div>

                {/* Status */}
                <div className="d-flex align-items-center gap-2">
                    <span className="status-dot" />
                    <span className="text-secondary small">System Online</span>
                </div>
            </header>

            {/* ── BODY ── */}
            <div className="app-body">

                {/* LEFT PANE */}
                <aside className={`left-pane bg-light border-end ${isPaneOpen ? 'pane-open' : 'pane-collapsed'}`}>
                    {/* Toggle */}
                    <button
                        className="pane-toggle btn btn-sm"
                        onClick={() => setIsPaneOpen((v) => !v)}
                        aria-label={isPaneOpen ? 'Collapse pane' : 'Expand pane'}
                    >
                        <i className="fa-solid fa-bars" />
                    </button>

                    {isPaneOpen && (
                        <div className="p-3 overflow-auto h-100 d-flex flex-column gap-3">

                            {/* Search */}
                            <div>
                                <p className="text-uppercase text-secondary fw-semibold small mb-2 section-label">
                                    Search
                                </p>
                                <input
                                    type="text"
                                    className="form-control form-control-sm"
                                    placeholder="Search…"
                                    value={searchValue}
                                    onChange={(e) => setSearchValue(e.target.value)}
                                />
                            </div>

                            {/* Details fields */}
                            <div>
                                <p className="text-uppercase text-secondary fw-semibold small mb-2 section-label">
                                    Details
                                </p>
                                <div className="d-flex flex-column gap-2">
                                    {(
                                        [
                                            ['jobName', 'Job Name'],
                                            ['binName', 'Bin Name'],
                                            ['buildCode', 'Build Code'],
                                            ['className', 'Class Name'],
                                            ['experiment', 'Experiment'],
                                            ['productName', 'Product Name'],
                                        ] as [keyof typeof fields, string][]
                                    ).map(([key, label]) => (
                                        <div key={key}>
                                            <label htmlFor={key} className="form-label text-secondary small mb-1">
                                                {label}
                                            </label>
                                            <input
                                                id={key}
                                                type="text"
                                                className="form-control form-control-sm"
                                                placeholder={label}
                                                value={fields[key]}
                                                onChange={(e) => handleFieldChange(key, e.target.value)}
                                            />
                                        </div>
                                    ))}
                                </div>
                            </div>

                            {/* Assign */}
                            <button
                                className="btn btn-primary w-100 fw-semibold mt-auto"
                                onClick={handleAssign}
                            >
                                Assign
                            </button>

                        </div>
                    )}
                </aside>

                {/* CONTENT AREA */}
                <main className="content-area overflow-auto p-4 bg-light">
                    <div className="d-flex flex-column gap-4">
                        {rackData.map((rack) => (
                            <div key={rack.id} className="card border shadow-sm">

                                {/* Card header */}
                                <div className="card-header bg-white d-flex align-items-center gap-2 border-bottom">
                                    <span className="text-primary fs-5">▤</span>
                                    <h5 className="mb-0 fw-bold text-uppercase text-dark" style={{ letterSpacing: '0.05em' }}>
                                        {rack.label}
                                    </h5>
                                    <span className="badge bg-primary ms-auto">{rack.rows.length} slots</span>
                                </div>

                                {/* Table */}
                                <div className="card-body p-0">
                                    <div className="table-responsive">
                                        <table className="table table-striped table-hover table-bordered mb-0 align-middle">
                                            <thead className="table-light">
                                                <tr>
                                                    {['Slot', 'Item', 'Status', 'Weight'].map((h) => (
                                                        <th
                                                            key={h}
                                                            className="text-uppercase small fw-semibold text-secondary"
                                                            style={{ letterSpacing: '0.07em' }}
                                                        >
                                                            {h}
                                                        </th>
                                                    ))}
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {rack.rows.map((row) => (
                                                    <tr key={row.slot}>
                                                        <td className="fw-bold text-primary">{row.slot}</td>
                                                        <td className="text-dark">{row.item}</td>
                                                        <td>
                                                            <span className={`badge ${statusBadge[row.status] ?? 'bg-secondary'}`}>
                                                                {row.status}
                                                            </span>
                                                        </td>
                                                        <td className="text-dark">{row.weight}</td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                </div>

                            </div>
                        ))}
                    </div>
                </main>
            </div>

            {/* ── FOOTER ── */}
            <footer className="app-footer bg-white border-top d-flex align-items-center justify-content-between px-3 shadow-sm">
                <span className="text-secondary small">© 2026 Stacker</span>
                <span className="text-secondary small opacity-50">v1.0.0</span>
            </footer>

        </div>
    )
}

export default App

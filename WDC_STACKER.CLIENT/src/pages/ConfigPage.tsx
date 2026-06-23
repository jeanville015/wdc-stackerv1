import { useState, useEffect, useRef } from 'react'
import { useCapacityConfig } from '../hooks/useCapacityConfig';
import type { CapacityConfig } from '../types/models';
import {
    Container,
    Card,
    Form,
    Button,
    Spinner,
    Alert,
    Stack,
} from 'react-bootstrap';

/** Converts a camelCase or snake_case field name to a readable label.
 *  e.g. "maxCapacity" → "Max Capacity" */
const toLabel = (field: string) =>
    field
        .replace(/_/g, " ")
        .replace(/([a-z])([A-Z])/g, "$1 $2")
        .toUpperCase()
        .trim();

const ConfigPage = () => {
    const { config, loading, error, save, reset } = useCapacityConfig();
    const [form, setForm] = useState<CapacityConfig | null>(null);
    const [saved, setSaved] = useState(false);
    const initialized = useRef(false);

    // Sync form when config loads
    useEffect(() => {
        if (config && !initialized.current) {
            initialized.current = true;
            setForm(config);
        }
    }, [config]);

    const handleChange = (field: keyof CapacityConfig, value: string) => {
        setForm(prev => {
            if (!prev) return prev;

            const currentValue = prev[field];
            const nextValue =
                typeof currentValue === 'number'
                    ? (value === '' ? 0 : Number(value))
                    : value;

            return {
                ...prev,
                [field]: nextValue,
            } as CapacityConfig;
        });
    };

    const handleSave = () => {
        if (!form) return;
        save(form);
        setSaved(true);
        setTimeout(() => setSaved(false), 3000);
    };

    const handleReset = () => {
        if (confirm('Reset to defaults?')) reset();
    };

    /* ── Loading / error states ── */
    if (loading) {
        return (
            <Container className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
                <Stack direction="horizontal" gap={2} className="text-muted">
                    <Spinner animation="border" size="sm" role="status" />
                    <span>Loading configuration…</span>
                </Stack>
            </Container>
        );
    }

    if (error) {
        return (
            <Container className="py-5" style={{ maxWidth: 640 }}>
                <Alert variant="danger">
                    <Alert.Heading>Failed to load configuration</Alert.Heading>
                    <p className="mb-0">{error}</p>
                </Alert>
            </Container>
        );
    }

    if (!form) return null;

    const fields = Object.keys(form) as (keyof CapacityConfig)[];

    return (
        <Container className="py-5" style={{ maxWidth: 640 }}>

            {/* Page header */}
            <div className="mb-4">
                <h2 className="fw-semibold mb-1">Capacity Configuration</h2>
                <p className="text-muted mb-0">
                    Adjust the capacity limits below and save when ready.
                </p>
            </div>

            {/* Success banner */}
            {saved && (
                <Alert variant="success" dismissible onClose={() => setSaved(false)}>
                    Configuration saved successfully.
                </Alert>
            )}

            {/* Form card */}
            <Card className="shadow-sm border-0">
                <Card.Body className="p-4">
                    <Form>
                        {fields.map(field => {
                            const fieldValue = form[field];
                            const isNumberField = typeof fieldValue === 'number';

                            return (
                                <Form.Group
                                    key={field}
                                    controlId={`field-${field}`}
                                    className="d-flex align-items-center mb-3"
                                >
                                    <Form.Label className="fw-medium small text-secondary text-uppercase mb-0 me-3 text-end" style={{ width: '50%' }}>
                                        {toLabel(field)}
                                    </Form.Label>
                                    <Form.Control
                                        type={isNumberField ? 'number' : 'text'}
                                        value={fieldValue}
                                        min={isNumberField ? 0 : undefined}
                                        onChange={e => handleChange(field, e.target.value)}
                                        style={{ width: '50%' }}
                                    />
                                </Form.Group>
                            );
                        })}
                    </Form>
                </Card.Body>

                <Card.Footer className="bg-white border-top px-4 py-3 d-flex justify-content-end gap-2">
                    <Button
                        variant="outline-secondary"
                        onClick={handleReset}
                    >
                        Reset to Defaults
                    </Button>
                    <Button
                        variant="primary"
                        onClick={handleSave}
                    >
                        Save Changes
                    </Button>
                </Card.Footer>
            </Card>

        </Container>
    );
};

export default ConfigPage;

import { useEffect, useRef, useState, type CSSProperties } from "react";
import { Alert, Button, Form, Spinner, Stack } from "react-bootstrap";
import RackReference from "../components/config/RackReference";
import ShipBoxReference from "../components/config/ShipBoxReference";
import { useCapacityConfig } from "../hooks/useCapacityConfig";
import type { CapacityConfig } from "../types/models";

const toLabel = (field: string) =>
    field
        .replace(/_/g, " ")
        .replace(/([a-z])([A-Z])/g, "$1 $2")
        .toUpperCase()
        .trim();

const rackFields = [
    { key: "RACK_COUNT", marker: "#0b66d8" },
    { key: "LAYER_COUNT", marker: "#16a6a2" },
    { key: "BOX_COUNT", marker: "#e49a12" },
    { key: "MAX_ITEM_PER_BOX", marker: "#8d3fd1" },
] as const;

const shipBoxFields = [
    { key: "LAYER_COUNT-SHIPBOX", marker: "#16a6a2" },
    { key: "BOX_COUNT-SHIPBOX", marker: "#e49a12" },
    { key: "MAX_ITEM_PER_BOX-SHIPBOX", marker: "#0b66d8" },
] as const;

const operationFields: (keyof CapacityConfig)[] = [
    "ValidOperation",
    "FJ",
    "FD",
    "FS",
    "SJ",
    "SD",
];

const pageStyle: CSSProperties = {
    width: "100%",
    maxWidth: "1220px",
    margin: "0 auto",
    padding: "1rem 0 2rem",
};

const sectionStyle: CSSProperties = {
    background: "#ffffff",
    border: "1px solid #dde1e9",
    borderRadius: "12px",
    boxShadow: "0 4px 18px rgba(23,43,77,0.08)",
    padding: "1.15rem",
    marginBottom: "1rem",
};

const sectionHeaderStyle: CSSProperties = {
    display: "flex",
    alignItems: "baseline",
    gap: "0.75rem",
    paddingBottom: "0.75rem",
    marginBottom: "1rem",
    borderBottom: "1px solid #e3e8f0",
};

const sectionTitleStyle: CSSProperties = {
    margin: 0,
    color: "#172b4d",
    fontSize: "1.05rem",
    fontWeight: 800,
};

const contentGridStyle: CSSProperties = {
    display: "grid",
    gridTemplateColumns: "minmax(260px, 0.75fr) minmax(0, 1.25fr)",
    gap: "1rem",
    alignItems: "start",
};

const settingsPanelStyle: CSSProperties = {
    border: "1px solid #e3e8f0",
    borderRadius: "10px",
    padding: "0.85rem",
};

const settingGroupTitleStyle: CSSProperties = {
    margin: "0 0 0.75rem",
    color: "#172b4d",
    fontSize: "0.72rem",
    fontWeight: 900,
    letterSpacing: "0.1em",
    textTransform: "uppercase",
};

interface ConfigFieldProps {
    field: keyof CapacityConfig;
    form: CapacityConfig;
    onChange: (field: keyof CapacityConfig, value: string) => void;
    marker?: string;
    fullWidth?: boolean;
}

function ConfigField({ field, form, onChange, marker, fullWidth = false }: ConfigFieldProps) {
    const value = form[field];
    const isNumberField = typeof value === "number";

    return (
        <Form.Group
            controlId={`field-${String(field)}`}
            style={{
                position: "relative",
                display: "grid",
                gridTemplateColumns: "minmax(120px, 0.8fr) minmax(0, 1.2fr)",
                alignItems: "center",
                gap: "0.7rem",
                padding: "0.6rem 0.7rem",
                marginBottom: "0.55rem",
                border: `1px solid ${marker ?? "#e3e8f0"}`,
                borderLeft: marker ? `4px solid ${marker}` : undefined,
                borderRadius: "8px",
                background: marker ? "#f8fbff" : "#fbfcfe",
                ...(fullWidth ? { gridColumn: "1 / -1" } : {}),
            }}
        >
            <Form.Label
                className="mb-0"
                style={{
                    color: "#5e6c84",
                    fontSize: "0.7rem",
                    fontWeight: 800,
                    letterSpacing: "0.04em",
                }}
            >
                {toLabel(String(field))}
            </Form.Label>
            <Form.Control
                type={isNumberField ? "number" : "text"}
                value={value}
                min={isNumberField ? 0 : undefined}
                onChange={(event) => onChange(field, event.target.value)}
                style={{ minWidth: 0 }}
            />
            {marker && (
                <span
                    aria-hidden="true"
                    style={{
                        position: "absolute",
                        right: "-7px",
                        top: "50%",
                        width: "12px",
                        height: "12px",
                        borderRadius: "50%",
                        transform: "translateY(-50%)",
                        background: marker,
                        border: "2px solid #ffffff",
                        boxShadow: `0 0 0 1px ${marker}`,
                    }}
                />
            )}
        </Form.Group>
    );
}

function OperationRules({ form, onChange }: { form: CapacityConfig; onChange: ConfigFieldProps["onChange"] }) {
    return (
        <section style={sectionStyle}>
            <div style={sectionHeaderStyle}>
                <h3 style={sectionTitleStyle}>Operation rules</h3>
            </div>
            <div
                className="operation-rules-grid"
                style={{
                    ...settingsPanelStyle,
                    display: "grid",
                    gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                    gap: "0 0.7rem",
                }}
            >
                <ConfigField field="ValidOperation" form={form} onChange={onChange} fullWidth />
                {operationFields.slice(1).map((field) => (
                    <ConfigField key={field} field={field} form={form} onChange={onChange} />
                ))}
            </div>
        </section>
    );
}

export default function ConfigPage() {
    const { config, loading, error, save, reset } = useCapacityConfig();
    const [form, setForm] = useState<CapacityConfig | null>(null);
    const [saved, setSaved] = useState(false);
    const initialized = useRef(false);

    useEffect(() => {
        if (config && !initialized.current) {
            initialized.current = true;
            setForm(config);
        }
    }, [config]);

    const handleChange = (field: keyof CapacityConfig, value: string) => {
        setForm((previous) => {
            if (!previous) return previous;

            const currentValue = previous[field];
            const nextValue = typeof currentValue === "number"
                ? (value === "" ? 0 : Number(value))
                : value;

            return { ...previous, [field]: nextValue } as CapacityConfig;
        });
    };

    const handleSave = () => {
        if (!form) return;
        save(form);
        setSaved(true);
        window.setTimeout(() => setSaved(false), 3000);
    };

    const handleReset = () => {
        if (window.confirm("Reset to defaults?")) reset();
    };

    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center" style={{ minHeight: "60vh" }}>
                <Stack direction="horizontal" gap={2} className="text-muted">
                    <Spinner animation="border" size="sm" role="status" />
                    <span>Loading configuration...</span>
                </Stack>
            </div>
        );
    }

    if (error) {
        return (
            <div style={{ maxWidth: 640, margin: "3rem auto" }}>
                <Alert variant="danger">
                    <Alert.Heading>Failed to load configuration</Alert.Heading>
                    <p className="mb-0">{error}</p>
                </Alert>
            </div>
        );
    }

    if (!form) return null;

    return (
        <div style={pageStyle}>
            <div style={{ marginBottom: "1rem" }}>
                <h2 className="fw-semibold mb-1">Capacity Configuration</h2>
                <p className="text-muted mb-0">Adjust the capacity limits below and save when ready.</p>
            </div>

            {saved && (
                <Alert variant="success" dismissible onClose={() => setSaved(false)}>
                    Configuration saved successfully.
                </Alert>
            )}

            <section style={sectionStyle}>
                <div style={sectionHeaderStyle}>
                    <h3 style={sectionTitleStyle}>Rack container settings</h3>
                </div>
                <div className="config-reference-grid" style={contentGridStyle}>
                    <div style={settingsPanelStyle}>
                        <h4 style={settingGroupTitleStyle}>Rack container settings</h4>
                        <Form>
                            {rackFields.map(({ key, marker }) => (
                                <ConfigField
                                    key={key}
                                    field={key}
                                    form={form}
                                    onChange={handleChange}
                                    marker={marker}
                                />
                            ))}
                        </Form>
                    </div>
                    <RackReference
                        layerCount={form.LAYER_COUNT}
                        boxCount={form.BOX_COUNT}
                        maxItems={form.MAX_ITEM_PER_BOX}
                    />
                </div>
            </section>

            <section style={sectionStyle}>
                <div style={sectionHeaderStyle}>
                    <h3 style={sectionTitleStyle}>ShipBox modal settings</h3>
                </div>
                <div className="config-reference-grid" style={contentGridStyle}>
                    <div style={settingsPanelStyle}>
                        <h4 style={settingGroupTitleStyle}>ShipBox modal settings</h4>
                        <Form>
                            {shipBoxFields.map(({ key, marker }) => (
                                <ConfigField
                                    key={key}
                                    field={key}
                                    form={form}
                                    onChange={handleChange}
                                    marker={marker}
                                />
                            ))}
                        </Form>
                    </div>
                    <ShipBoxReference
                        layerCount={form["LAYER_COUNT-SHIPBOX"]}
                        boxCount={form["BOX_COUNT-SHIPBOX"]}
                        maxItems={form["MAX_ITEM_PER_BOX-SHIPBOX"]}
                    />
                </div>
            </section>

            <OperationRules form={form} onChange={handleChange} />

            <div
                style={{
                    display: "flex",
                    justifyContent: "flex-end",
                    gap: "0.6rem",
                    paddingTop: "0.25rem",
                }}
            >
                <Button variant="outline-secondary" onClick={handleReset}>Reset to Defaults</Button>
                <Button variant="primary" onClick={handleSave}>Save Changes</Button>
            </div>
        </div>
    );
}

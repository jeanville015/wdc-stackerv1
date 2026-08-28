import { useEffect, useRef, useState, useCallback } from "react";

import StackerOperationControls from "../components/StackerOperationControls";

import RackBoard from "../components/home/RackBoard";

import HoldCheckResultModal from "../components/home/HoldCheckResultModal";

import { useCapacityConfig } from "../hooks/useCapacityConfig";

import JobWithdrawalPanel from "../components/withdrawal/JobWithdrawalPanel";

import JobUnshipPanel from "../components/unship/JobUnshipPanel";

import type { BoxView, ShipBoxView } from "../types/stacker";

import { Tab, Tabs } from "react-bootstrap";

import { getBoxesApi, exportCsvApi } from "../api/stackerApi";

import { runHoldCheck, type HoldCheckResult, } from "../api/holdCheckApi";

import { useAuth } from "../context/useAuth";



export default function HomePage() {

    const { config, loading, error } = useCapacityConfig();

    const { user } = useAuth();
    const token = user?.token;

    const [gridViewBoxes, setGridViewBoxes] = useState<BoxView[]>([]);

    const [selectedTargetBox, setSelectedTargetBox] = useState<BoxView | null>(null);

    const [selectedTargetShipBox, setSelectedTargetShipBox] = useState<ShipBoxView | null>(null);

    const [recentlyAssignedBoxNo, setRecentlyAssignedBoxNo] = useState<string | null>(null);

    const assignedBoxTimerRef = useRef<number | null>(null);

    const [refreshLoading, setRefreshLoading] = useState(false);

    const [holdCheckLoading, setHoldCheckLoading] = useState(false);

    const [holdCheckResult, setHoldCheckResult] = useState<HoldCheckResult | null>(null);

    const closeHoldCheckResult = useCallback(() => {  setHoldCheckResult(null); }, []);

    const [csvExportLoading, setCsvExportLoading] = useState(false);



    useEffect(() => {

        return () => {

            if (assignedBoxTimerRef.current) {

                window.clearTimeout(assignedBoxTimerRef.current);

            }

        };

    }, []);



    const showAssignedBoxConfirmation = (boxNo: string) => {

        setRecentlyAssignedBoxNo(boxNo);



        if (assignedBoxTimerRef.current) {

            window.clearTimeout(assignedBoxTimerRef.current);

        }



        assignedBoxTimerRef.current = window.setTimeout(() => {

            setRecentlyAssignedBoxNo(null);

        }, 4000);

    };



    const handleRefresh = useCallback(async () => {

        if (!token) return;



        setRefreshLoading(true);

        try {

            const result = await getBoxesApi(token);

            if (result.GridViewBoxes) {

                setGridViewBoxes(result.GridViewBoxes);

                setSelectedTargetBox(null);

                setSelectedTargetShipBox(null);

            }

        } catch (err) {

            console.error("Failed to refresh boxes:", err);

        } finally {

            setRefreshLoading(false);

        }

    }, [token]);



    const handleHoldCheck = useCallback(async () => {
        setHoldCheckLoading(true);
        setHoldCheckResult(null);

        try {
            const result = await runHoldCheck();

            if (result.success) {
                await handleRefresh();
            }

            setHoldCheckResult(result);
        } catch (err) {
            console.error("Failed to run hold check:", err);

            setHoldCheckResult({
                success: false,
                message: "Failed to run hold check.",
                error:
                    err instanceof Error
                        ? err.message
                        : "Please check the service connection.",
            });
        } finally {
            setHoldCheckLoading(false);
        }
    }, [handleRefresh]);

    const handleExportCsv = async () => {
        if (!token) {
            alert("Login token is missing. Please sign in again.");
            return;
        }

        setCsvExportLoading(true);
        try {
            await exportCsvApi(token);
        } catch (err) {
            alert(err instanceof Error ? err.message : "CSV export failed.");
        } finally {
            setCsvExportLoading(false);
        }
    };



    // Auto-refresh boxes on login (when user becomes available)

    useEffect(() => {

        if (!token || loading || !config) {
            return;
        }

        let isCancelled = false;

        Promise.resolve().then(async () => {
            if (isCancelled) return;

            setRefreshLoading(true);

            try {
                const result = await getBoxesApi(token);

                if (!isCancelled && result.GridViewBoxes) {
                    setGridViewBoxes(result.GridViewBoxes);
                    setSelectedTargetBox(null);
                    setSelectedTargetShipBox(null);
                }
            } catch (err) {
                if (!isCancelled) {
                    console.error("Failed to refresh boxes:", err);
                }
            } finally {
                if (!isCancelled) {
                    setRefreshLoading(false);
                }
            }
        });

        return () => {
            isCancelled = true;
        };

    }, [token, loading, config]);



    return (

        <div className="fgi-home-tabs-container">

            <Tabs

                defaultActiveKey="job-scanning-batching"

                id="fgi-home-tabs"

                className="fgi-home-tabs mb-0"

            >

            <Tab

                eventKey="job-scanning-batching"

                title="JOB SCANNING - BATCHING"

            >

                <div className="fgi-tab-page-panel">

                    <div

                        style={{

                            display: "grid",

                        gridTemplateColumns: "minmax(220px, 320px) minmax(0, 1fr)",

                        gap: "1rem",

                        alignItems: "start",

                        width: "100%",

                    }}

                >

                    <StackerOperationControls

                        onGridViewBoxesLoaded={setGridViewBoxes}

                        selectedTargetBox={selectedTargetBox}

                        selectedTargetShipBox={selectedTargetShipBox}

                        onSelectedTargetBoxChanged={setSelectedTargetBox}

                        onSelectedTargetShipBoxChanged={setSelectedTargetShipBox}

                        onAssignedBoxConfirmed={showAssignedBoxConfirmation}

                        boxCount={config?.BOX_COUNT ?? 1}

                        shipBoxBoxCount={config?.["BOX_COUNT-SHIPBOX"] ?? 1}

                    />



                    <section style={{ minWidth: 0, width: "100%" }}>

                        <div className="rack-view-toolbar">
                            <div className="rack-view-toolbar-heading">
                                <span className="rack-view-toolbar-icon" aria-hidden="true">
                                    <i className="fa-solid fa-border-all" />
                                </span>
                                <h2 className="rack-view-toolbar-title">Rack View</h2>
                            </div>

                            <div className="rack-view-toolbar-actions">
                                <button
                                    className="btn btn-sm rack-view-toolbar-button rack-view-toolbar-button--refresh"
                                    onClick={handleRefresh}
                                    disabled={refreshLoading || loading}
                                >
                                    {refreshLoading ? (
                                        <>
                                            <span
                                                className="spinner-border spinner-border-sm"
                                                role="status"
                                                aria-hidden="true"
                                            />
                                            Refreshing...
                                        </>
                                    ) : (
                                        <>
                                            <i className="fa-solid fa-arrows-rotate" aria-hidden="true" />
                                            Refresh
                                        </>
                                    )}
                                </button>

                                <button
                                    className="btn btn-sm rack-view-toolbar-button rack-view-toolbar-button--hold"
                                    onClick={handleHoldCheck}
                                    disabled={holdCheckLoading || refreshLoading || loading}
                                >
                                    {holdCheckLoading ? (
                                        <>
                                            <span
                                                className="spinner-border spinner-border-sm"
                                                role="status"
                                                aria-hidden="true"
                                            />
                                            Checking...
                                        </>
                                    ) : (
                                        <>
                                            <i className="fa-regular fa-clock" aria-hidden="true" />
                                            Check Hold
                                        </>
                                    )}
                                </button>

                                <button
                                    className="btn btn-sm rack-view-toolbar-button rack-view-toolbar-button--csv"
                                    onClick={handleExportCsv}
                                    disabled={csvExportLoading || loading}
                                >
                                    {csvExportLoading ? (
                                        <>
                                            <span
                                                className="spinner-border spinner-border-sm"
                                                role="status"
                                                aria-hidden="true"
                                            />
                                            Exporting...
                                        </>
                                    ) : (
                                        <>
                                            <i className="fa-solid fa-file-csv" aria-hidden="true" />
                                            Download CSV
                                        </>
                                    )}
                                </button>
                            </div>
                        </div>



                        {loading && (

                            <div

                                style={{

                                    background: "#ffffff",

                                    border: "1px solid #dde1e9",

                                    borderRadius: "12px",

                                    boxShadow: "0 4px 18px rgba(23,43,77,0.08)",

                                    padding: "1rem 1.1rem",

                                    color: "#172b4d",

                                    display: "flex",

                                    alignItems: "center",

                                    gap: "0.75rem",

                                    fontSize: "0.88rem",

                                }}

                            >

                                <span

                                    className="spinner-border spinner-border-sm"

                                    role="status"

                                    aria-hidden="true"

                                />

                                Loading capacity configuration...

                            </div>

                        )}



                        {!loading && error && (

                            <div

                                role="alert"

                                style={{

                                    background: "#ffffff",

                                    border: "1px solid #dde1e9",

                                    borderLeft: "3px solid #d23232",

                                    borderRadius: "12px",

                                    boxShadow: "0 4px 18px rgba(23,43,77,0.08)",

                                    padding: "1rem 1.1rem",

                                    color: "#d23232",

                                    fontSize: "0.88rem",

                                }}

                            >

                                {error}

                            </div>

                        )}



                        {!loading && !error && config && (

                            <RackBoard

                                config={{

                                    RACK_COUNT: config.RACK_COUNT,

                                    LAYER_COUNT: config.LAYER_COUNT,

                                    BOX_COUNT: config.BOX_COUNT,

                                    MAX_ITEM_PER_BOX: config.MAX_ITEM_PER_BOX,

                                    "LAYER_COUNT-SHIPBOX": config["LAYER_COUNT-SHIPBOX"],

                                    "BOX_COUNT-SHIPBOX": config["BOX_COUNT-SHIPBOX"],

                                    "MAX_ITEM_PER_BOX-SHIPBOX": config["MAX_ITEM_PER_BOX-SHIPBOX"],

                                }}

                                boxes={gridViewBoxes}

                                boxSelectionEnabled={false}

                                selectedTargetBox={selectedTargetBox}

                                recentlyAssignedBoxNo={recentlyAssignedBoxNo}

                                selectedTargetShipBox={selectedTargetShipBox}

                                onTargetShipBoxSelected={(box, shipBox) => {

                                    setSelectedTargetBox(box);

                                    setSelectedTargetShipBox(shipBox);

                                }}

                                onDisassociateSuccess={handleRefresh}

                            />

                        )}

                    </section>

                    </div>

                </div>

            </Tab>



                <Tab

                    eventKey="job-withdrawal"

                    title="JOB WITHDRAWAL"

                >

                    <div className="fgi-tab-page-panel">

                        {config ? (

                            <JobWithdrawalPanel config={config} />

                        ) : (

                            <div className="withdrawal-placeholder">

                                Loading capacity configuration...

                            </div>

                        )}

                    </div>

                </Tab>

                <Tab

                    eventKey="job-unship"

                    title="JOB UNSHIP"

                >

                    <div className="fgi-tab-page-panel">

                        <JobUnshipPanel />

                    </div>

                </Tab>

            </Tabs>

            <HoldCheckResultModal
                result={holdCheckResult}
                onClose={closeHoldCheckResult}
            />

        </div>

);

}


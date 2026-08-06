import { useEffect, useRef, useState, useCallback } from "react";

import StackerOperationControls from "../components/StackerOperationControls";

import RackBoard from "../components/home/RackBoard";

import { useCapacityConfig } from "../hooks/useCapacityConfig";

import JobWithdrawalPanel from "../components/withdrawal/JobWithdrawalPanel";

import type { BoxView, ShipBoxView } from "../types/stacker";

import { Tab, Tabs } from "react-bootstrap";

import { getBoxesApi } from "../api/stackerApi";

import { runHoldCheck } from "../api/holdCheckApi";

import { useAuth } from "../context/AuthContext";



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

        try {

            const result = await runHoldCheck();

            if (result.success) {

                alert(`Hold check completed successfully!\n\nTotal holders: ${result.data?.totalHolders}\nSet to HOLD: ${result.data?.holdersSetToHold}\nCleared: ${result.data?.holdersCleared}\nAlready on hold: ${result.data?.holdersAlreadyOnHold}\nAlready clear: ${result.data?.holdersAlreadyClear}`);

                // Clear hold check loading state before refresh
                setHoldCheckLoading(false);

                // Refresh the rack after hold check
                await handleRefresh();

            } else {

                alert(`Hold check failed: ${result.message}${result.error ? `\n\nError: ${result.error}` : ''}`);

            }

        } catch (err) {

            console.error("Failed to run hold check:", err);

            alert("Failed to run hold check. Please check the service connection.");

        } finally {

            setHoldCheckLoading(false);

        }

    }, [handleRefresh]);



    // Auto-refresh boxes on login (when user becomes available)

    useEffect(() => {

        if (!token || loading || !config) {
            return;

        }

        let cancelled = false;

        getBoxesApi(token)
            .then((result) => {
                if (cancelled || !result.GridViewBoxes) {
                    return;
                }

                setGridViewBoxes(result.GridViewBoxes);
                setSelectedTargetBox(null);
                setSelectedTargetShipBox(null);
            })
            .catch((err) => {
                if (!cancelled) {
                    console.error("Failed to refresh boxes:", err);
                }
            });

        return () => {
            cancelled = true;
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

                        gridTemplateColumns: "minmax(220px, 280px) minmax(0, 1fr)",

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

                    />



                    <section style={{ minWidth: 0, width: "100%" }}>

                        <div

                            style={{

                                display: "flex",

                                justifyContent: "space-between",

                                alignItems: "center",

                                marginBottom: "1rem",

                            }}

                        >

                            <h2

                                style={{

                                    fontSize: "1.1rem",

                                    fontWeight: "600",

                                    color: "#172b4d",

                                    margin: 0,

                                }}

                            >

                                Rack View

                            </h2>

                            <div style={{ display: "flex", gap: "0.5rem" }}>

                                <button

                                    className="btn btn-sm"

                                    onClick={handleHoldCheck}

                                    disabled={holdCheckLoading || refreshLoading || loading}

                                    style={{

                                        background: holdCheckLoading || refreshLoading || loading

                                            ? "#a0b4d6"

                                            : "#ff8b00",

                                        color: "#ffffff",

                                        border: "none",

                                        borderRadius: "6px",

                                        padding: "0.4rem 0.8rem",

                                        fontSize: "0.8rem",

                                        fontWeight: "500",

                                        display: "flex",

                                        alignItems: "center",

                                        gap: "0.5rem",

                                        cursor: holdCheckLoading || refreshLoading || loading ? "not-allowed" : "pointer",

                                    }}

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

                                            <svg

                                                xmlns="http://www.w3.org/2000/svg"

                                                width="14"

                                                height="14"

                                                viewBox="0 0 24 24"

                                                fill="none"

                                                stroke="currentColor"

                                                strokeWidth="2"

                                                strokeLinecap="round"

                                                strokeLinejoin="round"

                                            >

                                                <circle cx="12" cy="12" r="10" />

                                                <path d="M12 6v6l4 2" />

                                            </svg>

                                            Check Hold

                                        </>

                                    )}

                                </button>

                                <button

                                className="btn btn-sm"

                                onClick={handleRefresh}

                                disabled={refreshLoading || loading}

                                style={{

                                    background: refreshLoading || loading

                                        ? "#a0b4d6"

                                        : "#0052cc",

                                    color: "#ffffff",

                                    border: "none",

                                    borderRadius: "6px",

                                    padding: "0.4rem 0.8rem",

                                    fontSize: "0.8rem",

                                    fontWeight: "500",

                                    display: "flex",

                                    alignItems: "center",

                                    gap: "0.5rem",

                                    cursor: refreshLoading || loading ? "not-allowed" : "pointer",

                                }}

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

                                        <svg

                                            xmlns="http://www.w3.org/2000/svg"

                                            width="14"

                                            height="14"

                                            viewBox="0 0 24 24"

                                            fill="none"

                                            stroke="currentColor"

                                            strokeWidth="2"

                                            strokeLinecap="round"

                                            strokeLinejoin="round"

                                        >

                                            <path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />

                                            <path d="M3 3v5h5" />

                                            <path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16" />

                                            <path d="M16 21h5v-5" />

                                        </svg>

                                        Refresh

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

            </Tabs>

        </div>

);

}


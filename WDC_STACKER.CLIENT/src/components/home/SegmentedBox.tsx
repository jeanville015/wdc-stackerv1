import type { BoxView } from "../../types/stacker";
import { formatBoxName } from "../../utils/nameTransformers";

const HOLDER_MATRIX_CAP = 100;
const PROPORTIONAL_SEGMENT_COUNT = 10; 

interface SegmentedBoxProps {
    box: BoxView;
    maxItemPerBox: number;
}

export default function SegmentedBox({
    box,
    maxItemPerBox,
}: SegmentedBoxProps) {
    const capacity = Math.max(1, Number(maxItemPerBox) || 1);
    const usesHolderMatrix = capacity <= HOLDER_MATRIX_CAP;
    const segmentCount = usesHolderMatrix
        ? capacity
        : PROPORTIONAL_SEGMENT_COUNT;

    const itemCount = Math.max(0, Number(box.BoxListCount) || 0);

    const occupiedSegments = usesHolderMatrix
        ? Math.min(itemCount, segmentCount)
        : itemCount === 0
            ? 0
            : Math.min(
                segmentCount,
                Math.max(
                    1,
                    Math.round((itemCount / capacity) * segmentCount)
                )
            );

    const toSegmentIndex = (position: number) =>
        usesHolderMatrix
            ? position
            : Math.min(
                segmentCount - 1,
                Math.floor((position * segmentCount) / capacity)
            );

    const releaseSegmentIndexes = new Set(
        (box.ReleaseHolderPositions ?? [])
            .filter(
                (position) =>
                    Number.isInteger(position) &&
                    position >= 0 &&
                    position < itemCount
            )
            .map(toSegmentIndex)
    );

    const heldSegmentIndexes = new Set(
        (box.HeldHolderPositions ?? [])
            .filter(
                (position) =>
                    Number.isInteger(position) &&
                    position >= 0 &&
                    position < itemCount
            )
            .map(toSegmentIndex)
    );

    return (
        <span className="pwd-box-cell-content" aria-hidden="true">
            <span className="pwd-box-cell-identity">
                <strong className="pwd-box-cell-name-pill">
                    {formatBoxName(box.BoxNo, box.RackNum)}
                </strong>

                <small className="pwd-box-cell-count">
                    {itemCount}/{capacity}
                </small>
            </span>

            <span
                className="pwd-box-capacity-track"
                style={{
                    gridTemplateColumns:
                        `repeat(${segmentCount}, minmax(0, 1fr))`,
                }}
            >
                {Array.from({ length: segmentCount }, (_, index) => {
                    const isHeld = heldSegmentIndexes.has(index);
                    const isRelease =
                        releaseSegmentIndexes.has(index);
                    const isFilled = index < occupiedSegments;

                    const stateClass = isHeld
                        ? "is-held"
                        : isRelease
                            ? "is-release"
                            : isFilled
                                ? "is-filled"
                                : "is-available";

                    return (
                        <span
                            key={index}
                            className={
                                `pwd-box-capacity-segment ${stateClass}`
                            }
                        />
                    );
                })}
            </span>

            <span className="pwd-box-cell-action">
                <span>View holders</span>
                <i
                    className="fa-solid fa-chevron-right"
                    aria-hidden="true"
                />
            </span>
        </span>
    );
}

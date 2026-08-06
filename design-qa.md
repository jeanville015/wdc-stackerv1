# Withdrawal Holder Not-Found State — Design QA

- Source visual truth: `C:\Users\7362647\.codex\generated_images\019fab9f-877d-7521-9d1a-9cd135df808a\call_yBvMmOBXjrBfCcOYi42p7fTq.png`
- Implementation screenshot: `C:\Users\7362647\.codex\visualizations\2026\07\29\019fab9f-877d-7521-9d1a-9cd135df808a\holder-not-found-implemented.png`
- Full-view comparison: `C:\Users\7362647\.codex\visualizations\2026\07\29\019fab9f-877d-7521-9d1a-9cd135df808a\holder-not-found-comparison.png`
- Focused notification comparison: `C:\Users\7362647\.codex\visualizations\2026\07\29\019fab9f-877d-7521-9d1a-9cd135df808a\holder-not-found-focused-comparison.png`
- Source pixels: 1488 × 1057
- Implementation pixels: 1488 × 1058
- CSS viewport: 1488 × 1058
- Density normalization: 1× browser capture; focused notification crops were normalized to a common 60 px height for close inspection.
- State: large Withdrawal Disassociation Details modal with an unmatched Holder submitted against five included records.

## Full-view comparison evidence

The implementation preserves the existing large modal, two-column Verify Holders layout, FIFO review tables, and disabled Disassociate action. The selected Option 1 failure state appears directly below the Holder controls in the expected position, with a light-red background, red border, red error icon, and the exact copy `Holder not found in Included.`

The implementation capture uses an isolated local fixture so the surrounding application background and Shipping ID state differ from the source mockup. Those differences are verification-fixture constraints and are outside this scoped Holder notification change.

## Focused region comparison evidence

The focused comparison makes the requested notification readable at equal height. Copy, semantic color, bordered-container treatment, left alignment, and icon-before-text hierarchy match the selected direction. The production modal is wider than the generated concept, so the implementation banner expands responsively with its existing Holder card.

## Findings

- No actionable P0, P1, or P2 mismatch was found for the requested Holder notification state.
- P3: the implementation uses the existing Font Awesome circle-X error glyph while the generated concept resembles a circle-exclamation glyph. Both communicate an error clearly, and the difference does not affect the selected layout or behavior.

## Required fidelity surfaces

- Fonts and typography: Existing FGI Client family, weight hierarchy, uppercase labels, and readable error copy are preserved.
- Spacing and layout rhythm: Notification spacing, padding, border radius, and placement follow the selected paired-inline layout; width responds to the existing large modal.
- Colors and visual tokens: Existing FGI semantic error border, pale-red fill, and red foreground are reused consistently.
- Image quality and asset fidelity: No raster assets were introduced. The error icon comes from the project's existing Font Awesome library.
- Copy and content: Exact failure text is `Holder not found in Included.` The normal state uses the live `0 out of 5 included holders verified.` pattern.

## Interaction evidence

- Initial state displayed `0 out of 5 included holders verified.`
- Submitting an unmatched Holder replaced the count with `Holder not found in Included.`
- Replacing that value with an exact included Holder immediately removed the error and restored `0 out of 5 included holders verified.`
- Submitting the included Holder through Verify changed the live count to `1 out of 5 included holders verified.` and changed the matching row status to `VERIFIED`.
- The Holder input has an explicit Enter-key submission path through its containing form.

## Comparison history

- Pass 1: no P0/P1/P2 visual issue was found, so no blocking visual iteration was required.

## Implementation checklist

- [x] Replace the live count only while the Holder value is not in Included.
- [x] Restore the live count when the input is corrected to an included Holder.
- [x] Keep included-membership detection separate from unverified-row selection.
- [x] Preserve Verify-button and Enter-key submission behavior.
- [x] Keep the Disassociate action disabled until all required verification conditions pass.

final result: passed

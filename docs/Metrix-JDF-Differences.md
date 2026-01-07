# Metrix vs Signa JDF Differences (Working Draft)

## Purpose and scope
- Document structural and semantic differences between JDFs produced by EPS Metrix and Heidelberg Signa 21.x.
- Focus on imposition-related resources and Prinect Cockpit importability.
- This is a living comparison; details will be added as Metrix samples arrive.
- Signa is the reference dialect; see `docs/Signa-JDF-Dialect.md` for baseline behavior.
- Metrix exports imposition as a dedicated JDF file; other Metrix JDF types are separate files and are out of scope for this document.
- Current Metrix samples use prepress workflow "Heidelberg Printready" with JDF export set to "Impose To: Plate".

## Out of scope
- Full CIP4 JDF coverage beyond imposition and printing handoff.
- JMF workflows, device integration, or MIS/ERP automation.
- Formal vendor documentation or reverse-engineering of proprietary algorithms.

## Evidence model
- Metrix samples are the evidence corpus and will be cited explicitly as anchors.
- Absence of a Metrix sample does not imply absence in Metrix; it indicates unknown status.
- Claims will be marked as Observed / Inferred / Working theory, with stability tags.
- Avoid inline XML unless strictly needed for disambiguation.
- Sample set currently uses Metrix 24.1 and mxml schema 2.1.

## Stability levels (interpretation guidance)
- **Stable** — consistent across samples and required for interpretation.
- **Advisory** — useful when present but safe to ignore.
- **Transport-only** — required for handoff; not part of logical model.
- **Decorative** — preserve verbatim; do not interpret.

## Evidence set (Metrix samples)
Placeholder for the Metrix sample index.
- `Metrix_Samples/jdf/S2328.jdf` (imposition)
- `Metrix_Samples/mxml/S2328.mxml` (companion mxml)
- `Metrix_Samples/jdf/S2326.jdf` (imposition)
- `Metrix_Samples/mxml/S2326.mxml` (companion mxml)
- `Metrix_Samples/jdf/S2309.jdf` (imposition)
- `Metrix_Samples/mxml/S2309.mxml` (companion mxml)
- `Metrix_Samples/jdf/S2271.jdf` (imposition)
- `Metrix_Samples/mxml/S2271.mxml` (companion mxml)
- `Metrix_Samples/jdf/S2313.jdf` (imposition)
- `Metrix_Samples/mxml/S2313.mxml` (companion mxml)
- `Metrix_Samples/MetrixMXML.xsd` (mxml schema)

Sample metadata table (to be filled as samples arrive):
Sample set assumptions (current samples):
- Metrix 24.1
- mxml 2.1
- Prepress workflow: Heidelberg Printready
- JDF export: Impose To: Plate

| Sample ID | JDF file | MXML file | Metrix version/build | Export settings | Job type | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| S2328 | `Metrix_Samples/jdf/S2328.jdf` | `Metrix_Samples/mxml/S2328.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=OneSided + Perfected (mxml LayoutPool), Units=Inches (mxml) | Bound book, 8.5 x 11 trim, qty 3000, perfect bound (cover SS, text PE) | Mixed work styles (SS + PE), JDF Type=Imposition, Bound type meaning TBD |
| S2326 | `Metrix_Samples/jdf/S2326.jdf` | `Metrix_Samples/mxml/S2326.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=Perfected (mxml LayoutPool), Units=Inches (mxml) | Folded product, 8.5 x 11 trim, qty 25000, 3-panel folder | Unbound, JDF Type=Imposition, WorkStyle=PE |
| S2309 | `Metrix_Samples/jdf/S2309.jdf` | `Metrix_Samples/mxml/S2309.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=WorkAndTurn + Perfected (mxml LayoutPool), Units=Inches (mxml) | Bound calendar, 8.5 x 11 trim, qty 3500, saddle stitch | JDF Type=Imposition, WorkStyle=TN + PE, Bound type meaning TBD |
| S2271 | `Metrix_Samples/jdf/S2271.jdf` | `Metrix_Samples/mxml/S2271.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=Perfected (mxml LayoutPool), Units=Inches (mxml) | Bound magazine, 8.5 x 11 trim, qty 1000, saddle stitch | JDF Type=Imposition, WorkStyle=PE, Bound type meaning TBD |
| S2313 | `Metrix_Samples/jdf/S2313.jdf` | `Metrix_Samples/mxml/S2313.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=Perfected (mxml LayoutPool), Units=Inches (mxml) | Ganged postcards, 5x7 and 6x9, mixed quantities | Multiple flat products, JDF Type=Imposition, WorkStyle=PE |

## Metrix sample intake checklist
Use this when adding a new Metrix sample set.

- Add the imposition JDF to `Metrix_Samples/jdf/` and note its filename here.
- Add the companion mxml to `Metrix_Samples/mxml/` and note its filename here.
- Prefer Signa-style bundles: create a folder like `Metrix_Samples/S2328.jdf/` containing the JDF file and a `Content/` folder with the marks PDF; set `FileSpec/@URL` to `./Content/<marks>.pdf`.
- If using centralized marks storage (`Metrix_Samples/marks/`), ensure the JDF `FileSpec/@URL` points to the correct path.
- Record the Metrix version/build and export settings used.
- Note any external assets referenced (PDFs, marks, etc.) and whether they are included.
- Summarize the job type (simplex/perfecting, work style, ganged, finishing).

## Known references and artifacts
- Prior converter notes: `Old_Code/metrix_to_signa.py` (behavioral hints, not vendor documentation).
- Signa baseline: `docs/Signa-JDF-Dialect.md`.
- General element primer: `docs/jdf-elements-attributes.md`.
- Metrix mxml is used for Pace integration and often carries fields that can fill gaps in the JDF.
- `SSi` namespace is ignored by Heidelberg Cockpit but contains useful data (e.g., WorkStyle).

## High-level structural comparison (placeholder)
Observed in `Metrix_Samples/jdf/S2328.jdf`:

- Root `JDF` is `Type="Imposition"` with `xsi:type="Imposition"` and no `Types` process chain (Signa uses `ProcessGroup` + `Types` list).
- `Version="1.2"` and `MaxVersion="1.2"` (Signa samples are typically 1.3 with MaxVersion 1.6/1.7).
- `ResourcePool` only includes `Layout` and `RunList` resources (no Media, Component, ConventionalPrintingParams, etc.).
- `Layout` structure uses `Signature` -> `Sheet` -> `Surface` elements (not nested `Layout` resources).
- `ResourceLinkPool` is minimal: `LayoutLink` + two `RunListLink` entries, no `CombinedProcessIndex`.
- Namespaces include `SSi` (Creo/SSI extensions) and `HDM`; in this sample `HDM` appears only on `SeparationSpec`.

Stability: **Advisory** (single sample).

## Resource-by-resource differences (placeholder)
Each subsection will capture observed Metrix behavior and the corresponding Signa baseline.

### MXML companion (metrix-specific context)
Observed in `Metrix_Samples/mxml/S2328.mxml`, `Metrix_Samples/mxml/S2326.mxml`, `Metrix_Samples/mxml/S2309.mxml`, `Metrix_Samples/mxml/S2271.mxml`, and `Metrix_Samples/mxml/S2313.mxml`:

- MXML carries job-level identity (`ProjectID`, MIS IDs) and product metadata (trim size, quantity, binding intent).
- PagePool data includes per-page inks and bleed defaults, which are not present in the JDF.
- LayoutPool encodes printing method (`OneSided`, `Perfected`) and sheet-use metrics.
- Mark definitions (file-based and text marks) are defined here, with placement metadata and mark file references.
- Folding schemes appear in MXML (`FoldingScheme` with `JDFFoldCatalog`) and are referenced by components in the product.
- Binding machine references appear in MXML (e.g., saddle stitch hardware), not in JDF.
- Ganged work can appear as multiple `Product` entries in MXML, each with its own PagePool and component sizing.

Stability: **Advisory** (five samples).

### Layout and imposition geometry
Observed in `Metrix_Samples/jdf/S2328.jdf`, `Metrix_Samples/jdf/S2326.jdf`, `Metrix_Samples/jdf/S2309.jdf`, `Metrix_Samples/jdf/S2271.jdf`, and `Metrix_Samples/jdf/S2313.jdf`:

- Layout hierarchy uses `Signature` elements with `Name`, each containing `Sheet` with `Name` and `SSi:WorkStyle` (e.g., `SS`, `PE`).
- `Surface` elements are explicitly labeled with `Side="Front"` / `Side="Back"`, and carry `SSi:Dimension`, `SSi:MediaOrigin`, and `SurfaceContentsBox`.
- `ContentObject` instances carry `CTM`, `TrimCTM`, `TrimSize`, `ClipBox`, and `SSi:TrimBox1`; no `DescriptiveName` or `HDM:FinalPageBox`.
- `MarkObject` instances exist but are minimal (CTM + ClipBox + Ord), without per-mark metadata.
- `S2326` shows repeated `ContentObject` placements on each side with a 270-degree rotation (`CTM` with `0 -1 1 0`), indicating folded panel orientation.
- `S2309` mixes work styles across signatures (`SSi:WorkStyle="TN"` and `SSi:WorkStyle="PE"`) with 180-degree flips and 90/270-degree rotations, matching a saddle-stitch calendar layout.
- `S2271` uses consistent 270-degree rotations on the first signature and standard perfecting-style rotations on later signatures, matching a conventional saddle-stitch magazine layout.
- `S2313` mixes multiple `TrimSize` values (5x7 and 6x9) in one layout, indicating ganged products on the same sheet.
- In `S2326`, MXML `ComponentPool` describes a 3-up layout with rotated component instances, matching the repeated three-panel `ContentObject` geometry in the JDF.

Stability: **Advisory** (five samples).

### RunList / FileSpec
Observed in `Metrix_Samples/jdf/S2328.jdf`, `Metrix_Samples/jdf/S2326.jdf`, `Metrix_Samples/jdf/S2309.jdf`, `Metrix_Samples/jdf/S2271.jdf`, and `Metrix_Samples/jdf/S2313.jdf`:

- Document RunList includes `NPage="520"` with a `PageList` of `PageData` entries; `LayoutElement` is present with `IsBlank="true"` and no `FileSpec`.
- Marks RunList includes a `LayoutElement` with `FileSpec` pointing to `./Content/S2328_Marks.pdf`.
- Marks RunList contains a `SeparationSpec` with `HDM:Type="DieLine"` and `Name="ProofColor"`.

Stability: **Advisory** (single sample).

### Media (paper/plate)
Observed in `Metrix_Samples/jdf/S2328.jdf`:

- No `Media` resources present; sheet sizing appears embedded on `Surface` via `SSi:Dimension` and `SSi:MediaOrigin`.

Stability: **Advisory** (single sample).

### ConventionalPrintingParams
Observed in `Metrix_Samples/jdf/S2328.jdf`:

- No `ConventionalPrintingParams` resource; work style appears encoded on `Sheet` via `SSi:WorkStyle`.

Stability: **Advisory** (single sample).

### StrippingParams / BinderySignature / FoldingParams
Observed in `Metrix_Samples/jdf/S2328.jdf`:

- No `StrippingParams`, `BinderySignature`, or `FoldingParams` resources (folding intent appears in MXML instead).

Stability: **Advisory** (five samples).

### Component / Assembly / CuttingParams
Observed in `Metrix_Samples/jdf/S2328.jdf`:

- No `Component`, `Assembly`, or `CuttingParams` resources.

Stability: **Advisory** (single sample).

## Missing or reduced data in Metrix (candidate list)
These are hypotheses to verify against samples.
Observed gaps in `Metrix_Samples/jdf/S2328.jdf`, `Metrix_Samples/jdf/S2326.jdf`, `Metrix_Samples/jdf/S2309.jdf`, `Metrix_Samples/jdf/S2271.jdf`, and `Metrix_Samples/jdf/S2313.jdf` (relative to Signa exports):

- Signa-specific HDM metadata (SignaBLOB, SignaJob, SignaGenContext) is absent.
- No Media resources; sheet sizes appear only on `Surface`.
- No ConventionalPrintingParams, StrippingParams, BinderySignature, Component, or Assembly resources.
- Marks are present as `MarkObject` and a marks RunList, but without per-mark parameter metadata.

Stability: **Advisory** (five samples).

## Prinect importability notes (placeholder)
Observed in Cockpit with vanilla Metrix JDFs:

- Imports succeed without complaints.
- Proof generation works.
- Plate generation works.
- WorkStyle is unknown in Cockpit (defaults to sheetwise and is not editable), which can break press preview routing.
- Sheet previews are often incorrect (sheet drawn in a corner rather than centered).
- Cockpit option "Allow spot colors for BCMY" is disabled/greyed out, preventing spot-color mapping for color bars.
- Additional issues may exist (to be validated).

Stability: **Advisory** (operator observation, not yet tied to specific samples).

Observed in Cockpit with normalized S2328 bundle (`Metrix_Samples/S2328.jdf/data.jdf`):

- Import errors: “The layout to be imported is empty.” and “Can’t copy the Signa Station data file.”
- Previews render fronts at bottom-left and backs at bottom-right of the plate.
- No page list created.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 2 with content PDF, no SignaBLOB URL):

- Import warning: “The layout to be imported is empty.”
- WorkStyle shows as Sheetwise for all sheets.
- Paper size shows as 26x20 on all sheets; no other paper details shown.
- Plate size shows as 29.33x25.98 on all sheets.
- “Allow spot colors for BCMY” is enabled.
- Previews still show off-center sheets (fronts bottom-left, backs bottom-right).
- No page list created.
- No error about missing Signa Station file.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 3 with Document page mapping enabled):

- No observable changes from iteration 2 (same errors, previews, and missing page list).

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 4 with PagePool/Output RunLists enabled):

- No observable changes from iteration 3.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 5 with content placement grid):

- “Layout is valid.”
- Page list created, but only one position (label “1”) repeated 612 times.
- Previews still incorrect (off-center).
- Sheet sizes, plate sizes, and WorkStyle remain unchanged.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 6 with Metrix Ord mapping):

- Page list now contains positions 0, 2, 4, 6, 8, 10, 12, 14, 16 (each repeated 68 times).
- No other observable changes.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 7 with Metrix CTM/TrimBox placement):

- Page list expands to 518 positions with uneven duplication counts (e.g., 0×3, 2×2, 4×2, 98–101×4).
- Cover front placements look closer to correct; cover back appears but should not, and runs off sheet.
- Previews still anchored bottom-left/bottom-right.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 8 with simplex back suppression + WorkStyle mapping):

- No observable changes from iteration 7.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 9 with MediaOrigin offset applied to content placement):

- Cover layout may have shifted slightly.
- No other observable changes.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 10 with PaperRect centering + TransferCurvePool CTM):

- Sheet previews appear correctly centered in thumbnails and Layout Preview.
- No other problems resolved.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 11 with uniform WorkStyle rule):

- WorkStyle shows Perfecting on all sheets except the first, which shows Single-Sided.
- The first sheet still contains two sides despite Single-Sided WorkStyle.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 12 with per-sheet simplex back suppression):

- Sheet 1 is truly single-sided; sheets 2+ are perfecting.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 13 with label mapping from MXML/PageList):

- Page list appears correct.
- Sheet and plate sizes still incorrect.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 14 with per-sheet Media + PaperRect + TransferCurvePool partitions):

- Sheet and plate sizes now appear correct.
- Page blocks are offset to the right and high, running off the top edge of the sheet.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 15 with ContentObject placement aligned to Metrix CTM, no MediaOrigin shift):

- Layout previews appear correct (subject to visual confirmation against Metrix).
- Page blocks no longer appear offset in the preview.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 16 with paper metadata injected from MXML):

- Imposed PDF generation fails with “No PDF file created” and “Error during imposition”.
- Failure occurs for both bundled `content.pdf` and external PDFs.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 17 with MarkObject geometry from Metrix):

- Imposed PDF generation succeeds (no imposition error).
- Marks PDF pages are only merged on Sheet 1 (page 1) and Sheet 2 front (page 3); remaining sheets have no marks.
- Expected marks pairing (pages 1&2 on Sheet 1, pages 3&4 on Sheet 2 front) is not honored.
- Trim box appears only on Sheet 1 and Sheet 2 front; missing on other sheets.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 18 with non-blank content assignment):

- Marks that rely on process/BCMY separations appear correctly when non-blank PDFs are assigned.
- Prior missing marks were caused by assigning blanks (no separations to map).

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2328 bundle (iteration 19 with CuttingParams/StrippingParams preview helpers):

- Layout Preview still crashes Acrobat.
- Imposed PDF fails again with "Error during imposition".

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2313 bundle (ganged postcards):

- Page list labels are correct and count matches expectation.
- Thumbnail Preview and Layout Preview do not crash Acrobat and look correct.
- Imposed PDF appears correct.
- Artwork assignment only works for the 9x6 position (TAR52209_65362).
- Other positions reject artwork due to TrimBox size mismatch; Cockpit reports:
  - TrimBox is 7.0 x 5.0 in, but layout positions expect 5.0 x 7.0 in for position 5 (repeated).

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized S2313 bundle (no `HDM:PageOrientation`/`HDM:FinalPageBox` on ContentObjects):

- All pages assign correctly.
- Previews look correct.
- Imposed PDF looks correct.

Stability: **Advisory** (single sample run).

## What now works (current normalization)
Observed in Cockpit with normalized S2328 bundle (current state):

- Import succeeds without errors.
- Sheet and plate sizes are correct.
- Layout previews appear centered and reasonable across cover + text sheets.
- Layout Preview does not crash Acrobat.
- Page list appears correct.
- WorkStyle is correct per sheet (SS for cover, PE for text), with simplex back suppression.
- "Allow spot colors for BCMY" is enabled.
- Paper attributes (Description/Brand/ProductID/Weight/Thickness/Grade/Manufacturer/GrainDirection) are populated.

Stability: **Advisory** (single sample run; needs cross-check against Metrix preview).

### Layout Preview crash notes
Observed behaviors so far:

- Crashes when CuttingParams/StrippingParams preview helpers are injected (iteration 19).
- Does not crash with current normalization that includes MXML-derived paper metadata.

Crash/No-crash checklist (layout preview):

- Verify `Layout`/`StrippingParams`/`CuttingParams` helper nodes are absent unless needed (crash observed when added).
- Confirm `HDM:PaperRect` exists per side and aligns with `TransferCurvePool` Paper CTM (preview placement).
- Confirm `TransferCurvePoolRef` is present at signature-level `Layout` for mixed sheet sizes (preview anchoring).
- Confirm `Media` (Paper/Plate) partitions exist with valid `Dimension` values per sheet.
- Confirm `ContentObject` CTM/TrimCTM are not double-shifted by `MediaOrigin`.
- If crash persists, diff against the Python output and check for unexpected attributes or malformed numbers.

### Normalization changes that enabled the current wins
These map specific normalization steps to the observed Cockpit outcomes for S2328.

| Change | Result | Evidence |
| --- | --- | --- |
| Add `Media` (Paper/Plate) with per-sheet `Dimension` + `SignatureName/SheetName` partitions; add signature-level `MediaRef`; clear top-level `Dimension` when mixed | Sheet and plate sizes now correct. | Iteration 14 (sizes correct). |
| Add per-sheet `HDM:PaperRect` using `SSi:MediaOrigin` (or centered fallback) + per-sheet `TransferCurveSet` Paper CTM | Layout previews centered and reasonable. | Iteration 10 and iteration 15 (previews correct). |
| Normalize `TransferCurvePool` per signature + `TransferCurvePoolRef` at signature-level `Layout` | Mixed sheet sizes (cover vs text) center correctly; avoids text sheets anchoring to cover offset. | Iteration 22 (signature-level transfer curves). |
| Use Metrix `ContentObject` geometry (CTM/TrimCTM/ClipBox/TrimBox1) without applying `MediaOrigin` shift | Page blocks align on sheets; no offset to the right/top. | Iteration 15 (page blocks no longer offset). |
| Map `SSi:WorkStyle` to Signa WorkStyle + suppress back side for simplex sheets | WorkStyle correct per sheet; cover is single-sided. | Iteration 12 (simplex back suppression works). |
| Remove `HDM:SignaBLOB` URL; keep Signa metadata otherwise | Avoid “Can’t copy the Signa Station data file.” | Iteration 2 (missing Signa Station error resolved). |
| Normalize marks RunList structure + `SeparationSpec` placeholders | "Allow spot colors for BCMY" enabled. | Iteration 2 onward (BCMY enabled). |
| Page labels from Metrix `Ord` + MXML `PagePool` | Page list appears correct. | Iteration 13 (page list correct). |
| Apply MXML paper metadata to Paper `Media` | Paper attributes show in Cockpit; Layout Preview remains stable. | Current state (paper attributes populated). |

Suspected causes (to validate):

- WorkStyle is not surfaced because `ConventionalPrintingParams` is missing; Cockpit defaults to sheetwise without editable override.
- Previews render at a corner due to missing `Media`/`PaperRect` or mismatched `SurfaceContentsBox` vs sheet origin.
- BCMY spot-color mapping likely depends on both marks RunList structure and `SeparationSpec` content (see `docs/Signa-JDF-Dialect.md` notes on marks RunList normalization and BCMY mapping).
- “Can’t copy the Signa Station data file” likely stems from `HDM:SignaBLOB`/`HDM:SignaJDF` references without matching files in the bundle.
- “Layout to be imported is empty” and missing page list may be caused by missing/invalid Document RunList and/or missing content PDF.

Validation checklist (testable items):

| Item | Status | Validation | Test setup |
| --- | --- | --- | --- |
| Add `ConventionalPrintingParams` with `WorkStyle` partitions | Failed | WorkStyle appears and is editable in Cockpit. | Use `Metrix_Samples/jdf/S2328.jdf` (mixed SS/PE) for coverage. |
| Add `Media` resources + `HDM:PaperRect` alignment | Partial | Previews are centered and match sheet dimensions. | Use `Metrix_Samples/jdf/S2271.jdf` (standard saddle stitch) for baseline. |
| Normalize `TransferCurvePool` per signature | Pass | Mixed sheet sizes center correctly (cover vs text). | Use `Metrix_Samples/jdf/S2328.jdf` (cover + text sizes). |
| Normalize marks RunList structure + BCMY `SeparationSpec` placeholders | Partial | "Allow spot colors for BCMY" is enabled and spot mapping works. | Use `Metrix_Samples/jdf/S2313.jdf` (ganged) to verify BCMY behavior. |

## Conversion strategies (Metrix -> Prinect-ready JDF)
This section will formalize a practical normalization plan.

1) Identify missing Signa-required fields and resources.
2) Inject or normalize required structures based on Signa baseline.
3) Preserve vendor-specific data without interpretation.
4) Validate against schema and Cockpit import behavior.

## Metrix -> Signa normalization deltas (checklist)
Based on observed gaps in current Metrix samples.

- Add `JDF/@Type="ProcessGroup"` and populate `Types` chain if required by Cockpit import (Metrix uses `Imposition` only).
- Create `ResourceLinkPool` entries with `CombinedProcessIndex` when a process chain is introduced.
- Inject `ConventionalPrintingParams` partitions by Signature/Sheet/Side using `SSi:WorkStyle` and/or MXML `PrintingMethod`.
- Add `Media` resources for paper/plate; derive `Media/@Dimension` and origins from `Surface` `SSi:Dimension` and `SSi:MediaOrigin`.
- Populate `HDM:PaperRect` and/or `Layout/@SurfaceContentsBox` equivalents to align with marks PDF TrimBox expectations.
- Normalize `TransferCurvePool` per signature and attach `TransferCurvePoolRef` at signature-level `Layout` when mixed sheet sizes are present.
- Translate MXML folding intent (`FoldingScheme`, component hierarchy) into `BinderySignature`/`FoldingParams` when needed.
- Emit `Component` hierarchy for sheet/block/final products when downstream workflows expect it.
- Preserve `MarkObject` geometry and attach marks RunList `FileSpec` (already present) to align with Signa-style marks handling.
- Map MXML `PagePool` inks/bleeds to JDF `SeparationSpec` and page metadata when required.
- Preserve vendor-specific namespaces (`SSi`, `HDM`) while avoiding interpretation of opaque values.

## MXML -> Signa JDF field mapping (draft)
This appendix lists candidate mappings from Metrix MXML to Signa-style JDF constructs. Treat as a working guide.

- Job identity: MXML `Project/@ProjectID` + `Project/@Name` -> JDF `JobID` + `DescriptiveName` (already aligned in samples).
- Product trim: MXML `Product/@FinishedTrimWidth` + `FinishedTrimHeight` -> Signa `HDM:PaperRect` and/or page TrimBox assumptions.
- Quantity: MXML `Product/@RequiredQuantity` -> `Component/AmountPool` (if component hierarchy is emitted).
- Work style: MXML `Layout/@PrintingMethod` and/or JDF `SSi:WorkStyle` -> `ConventionalPrintingParams/@WorkStyle`.
- Page bleeds: MXML `PageDefaults/@Bleed*` + per-page overrides -> `ContentObject/@TrimSize` + derived `FinalPageBox`.
- Page inks: MXML `Ink` + `Page/InkRef` -> `SeparationSpec` and/or `RunList` color handling.
- Folding intent: MXML `FoldingScheme/@JDFFoldCatalog` + `Component/FoldingSchemeRef` -> `BinderySignature` + `FoldingParams`.
- Component layout: MXML `ComponentCalculated` + `ComponentInstance` -> `Component` + `Assembly` + `CutBlock` (if downstream expects blocks).
- Stock sheet: MXML `StockSheet/@Width` + `@Height` -> `Media/@Dimension` (paper); `Stock/@Thickness` -> optional media detail.
- Marks: MXML `MarkPool` + `MarkFile` -> preserve marks PDF `RunList` and keep mark geometry as advisory.
- BCMY mapping: ensure marks RunList structure and `SeparationSpec` placeholders follow Signa conventions (see `docs/Signa-JDF-Dialect.md` BCMY normalization notes).

Evidence anchors: `Metrix_Samples/mxml/S2328.mxml`, `Metrix_Samples/mxml/S2326.mxml`, `Metrix_Samples/mxml/S2309.mxml`, `Metrix_Samples/mxml/S2271.mxml`, `Metrix_Samples/mxml/S2313.mxml`.

## Open questions
- Which Metrix versions/builds beyond 24.1 are in scope?
- Which JDF export settings beyond "Impose To: Plate" affect structure?
- Are there Metrix-specific vendor namespaces that need preserving?

## Resolved questions (current samples)
- MXML `Product/@Type="Bound"` appears on saddle-stitched products (e.g., S2309, S2271), so it is not exclusive to perfect binding.

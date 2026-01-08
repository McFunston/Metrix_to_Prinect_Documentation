# Metrix vs Signa JDF Differences

## Status
- Audience: Metrix→Prinect normalization and Prinect Cockpit import work.
- Maturity: Living reference; sections use Stability tags and sample anchors.
- Sample policy: filenames are sanitized placeholders; private samples live outside this repo.
- Change log: see Appendix.

## AI quick summary
- Metrix exports imposition as `Type="Imposition"` with minimal resources; Signa expects `ProcessGroup`.
- MXML fills critical gaps (paper, workstyle, folding, marks metadata).
- Converter prioritizes geometry, press behavior, and page assignment over full semantics.
- Marks PDF must be preserved; document PDF should not be injected via layout import.
- Cockpit issues correlate with missing printing params, PaperRect, or inconsistent partitions.

## Anchor index
- Scope: Purpose and scope (id: Metrix-Scope)
- Samples: Evidence set + Sample metadata (id: Metrix-Samples)
- Geometry: Layout and imposition geometry (id: Metrix-Layout)
- Gaps: Missing or reduced data in Metrix (id: Metrix-Gaps)
- Normalization: Metrix -> Signa normalization deltas (id: Metrix-Normalization)
- Testing: Prinect importability notes + observed iterations (id: Metrix-Testing)
- Wins: What now works (id: Metrix-WhatWorks)
- Recipe: Current normalization recipe (id: Metrix-Recipe)
- Defaults: Defaults (id: Metrix-Defaults)
- Preview crash: Layout Preview crash notes (id: Metrix-PreviewCrash)
- Strategy: Conversion strategies (id: Metrix-Strategies)
- Mapping: MXML -> Signa JDF field mapping (id: Metrix-Mapping)

## Key invariants (normalized output)
- `JDF/@Type="ProcessGroup"` with `Types` chain and `CombinedProcessIndex`.
- `ConventionalPrintingParams` partitions per Signature/Sheet/Side with correct WorkStyle.
- `Media` includes paper/plate dimensions and `HDM:PaperRect` for preview alignment.
- Marks RunList `FileSpec` points to marks PDF; BCMY placeholders present.
- Page list labels and `HDM:JobPart` remain consistent in multi-product jobs.

<a id="Metrix-Scope"></a>
## Purpose and scope
- Document structural and semantic differences between JDFs produced by EPS Metrix and Heidelberg Signa 21.x.
- Focus on imposition-related resources and Prinect Cockpit importability.
- This is a living comparison; details evolve as Metrix samples arrive.
- Signa is the reference dialect; see `docs/Signa-JDF-Dialect.md` for baseline behavior.
- Metrix exports imposition as a dedicated JDF file; other Metrix JDF types are separate files and are out of scope for this document.
- Current Metrix samples use prepress workflow "Heidelberg Printready" with JDF export set to "Impose To: Plate".

## Out of scope
- Full CIP4 JDF coverage beyond imposition and printing handoff.
- JMF workflows, device integration, or MIS/ERP automation.
- Formal vendor documentation or reverse-engineering of proprietary algorithms.

## Data policy
- Private sample data, marks PDFs, and customer metadata are kept outside this repo.
- Sample names in this document are placeholders; real identifiers are mapped in private notes.

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

<a id="Metrix-Samples"></a>
## Evidence set (Metrix samples)
Sample index for the Metrix evidence set.
- `Sample_A.jdf` (imposition)
- `Sample_A.mxml` (companion mxml)
- `Sample_B.jdf` (imposition)
- `Sample_B.mxml` (companion mxml)
- `Sample_C.jdf` (imposition)
- `Sample_C.mxml` (companion mxml)
- `Sample_D.jdf` (imposition)
- `Sample_D.mxml` (companion mxml)
- `Ganged_Postcards.jdf` (imposition)
- `Ganged_Postcards.mxml` (companion mxml)
- `MetrixMXML.xsd` (mxml schema)

## Sample metadata
### Sample set assumptions (current samples)
- Metrix 24.1
- mxml 2.1
- Prepress workflow: Heidelberg Printready
- JDF export: Impose To: Plate

### Sample metadata table
| Sample ID | JDF file | MXML file | Metrix version/build | Export settings | Job type | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Sample_A | `Sample_A.jdf` | `Sample_A.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=OneSided + Perfected (mxml LayoutPool), Units=Inches (mxml) | Bound book, 8.5 x 11 trim, qty 3000, perfect bound (cover SS, text PE) | Mixed work styles (SS + PE), JDF Type=Imposition, Bound type covers perfect bound and saddle stitch |
| Sample_B | `Sample_B.jdf` | `Sample_B.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=Perfected (mxml LayoutPool), Units=Inches (mxml) | Folded product, 8.5 x 11 trim, qty 25000, 3-panel folder | Unbound, JDF Type=Imposition, WorkStyle=PE |
| Sample_C | `Sample_C.jdf` | `Sample_C.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=WorkAndTurn + Perfected (mxml LayoutPool), Units=Inches (mxml) | Bound calendar, 8.5 x 11 trim, qty 3500, saddle stitch | JDF Type=Imposition, WorkStyle=TN + PE, Bound type covers perfect bound and saddle stitch |
| Sample_D | `Sample_D.jdf` | `Sample_D.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=Perfected (mxml LayoutPool), Units=Inches (mxml) | Bound magazine, 8.5 x 11 trim, qty 1000, saddle stitch | JDF Type=Imposition, WorkStyle=PE, Bound type covers perfect bound and saddle stitch |
| Ganged_Postcards | `Ganged_Postcards.jdf` | `Ganged_Postcards.mxml` | 2024.1 (202410884) [Heidelberg Printready] | PrintingMethod=Perfected (mxml LayoutPool), Units=Inches (mxml) | Ganged postcards, 5x7 and 6x9, mixed quantities | Multiple flat products, JDF Type=Imposition, WorkStyle=PE |

<a id="Metrix-Intake"></a>
## Metrix sample intake checklist
Use this when adding a new Metrix sample set.

- Add the imposition JDF to the private samples folder (see `docs/samples/README.md`) and note its filename here.
- Add the companion mxml to the private samples folder (see `docs/samples/README.md`) and note its filename here.
- Prefer Signa-style bundles: create a folder like `Sample_A.jdf/` containing the JDF file and a `Content/` folder with the marks PDF; set `FileSpec/@URL` to `./Content/<marks>.pdf`.
- If using centralized marks storage (`marks/`), ensure the JDF `FileSpec/@URL` points to the correct path.
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
Observed in `Sample_A.jdf`:

- Root `JDF` is `Type="Imposition"` with `xsi:type="Imposition"` and no `Types` process chain (Signa uses `ProcessGroup` + `Types` list).
- `Version="1.2"` and `MaxVersion="1.2"` (Signa samples are typically 1.3 with MaxVersion 1.6/1.7).
- `ResourcePool` only includes `Layout` and `RunList` resources (no Media, Component, ConventionalPrintingParams, etc.).
- `Layout` structure uses `Signature` -> `Sheet` -> `Surface` elements (not nested `Layout` resources).
- `ResourceLinkPool` is minimal: `LayoutLink` + two `RunListLink` entries, no `CombinedProcessIndex`.
- Namespaces include `SSi` (Creo/SSI extensions) and `HDM`; in this sample `HDM` appears only on `SeparationSpec`.

Stability: **Advisory** (single sample).

## Resource-by-resource differences (placeholder)
Each subsection will capture observed Metrix behavior and the corresponding Signa baseline.

<a id="Metrix-MXML"></a>
### MXML companion (metrix-specific context)
Observed in `Sample_A.mxml`, `Sample_B.mxml`, `Sample_C.mxml`, `Sample_D.mxml`, and `Ganged_Postcards.mxml`:

- MXML carries job-level identity (`ProjectID`, MIS IDs) and product metadata (trim size, quantity, binding intent).
- PagePool data includes per-page inks and bleed defaults, which are not present in the JDF.
- LayoutPool encodes printing method (`OneSided`, `Perfected`) and sheet-use metrics.
- Mark definitions (file-based and text marks) are defined here, with placement metadata and mark file references.
- Folding schemes appear in MXML (`FoldingScheme` with `JDFFoldCatalog`) and are referenced by components in the product.
- Binding machine references appear in MXML (e.g., saddle stitch hardware), not in JDF.
- Ganged work can appear as multiple `Product` entries in MXML, each with its own PagePool and component sizing.

Stability: **Advisory** (five samples).

<a id="Metrix-Layout"></a>
### Layout and imposition geometry
Observed in `Sample_A.jdf`, `Sample_B.jdf`, `Sample_C.jdf`, `Sample_D.jdf`, and `Ganged_Postcards.jdf`:

- Layout hierarchy uses `Signature` elements with `Name`, each containing `Sheet` with `Name` and `SSi:WorkStyle` (e.g., `SS`, `PE`).
- `Surface` elements are explicitly labeled with `Side="Front"` / `Side="Back"`, and carry `SSi:Dimension`, `SSi:MediaOrigin`, and `SurfaceContentsBox`.
- `ContentObject` instances carry `CTM`, `TrimCTM`, `TrimSize`, `ClipBox`, and `SSi:TrimBox1`; no `DescriptiveName` or `HDM:FinalPageBox`.
- `MarkObject` instances exist but are minimal (CTM + ClipBox + Ord), without per-mark metadata.
- `Sample_B` shows repeated `ContentObject` placements on each side with a 270-degree rotation (`CTM` with `0 -1 1 0`), indicating folded panel orientation.
- `Sample_C` mixes work styles across signatures (`SSi:WorkStyle="TN"` and `SSi:WorkStyle="PE"`) with 180-degree flips and 90/270-degree rotations, matching a saddle-stitch calendar layout.
- `Sample_D` uses consistent 270-degree rotations on the first signature and standard perfecting-style rotations on later signatures, matching a conventional saddle-stitch magazine layout.
- `Ganged_Postcards` mixes multiple `TrimSize` values (5x7 and 6x9) in one layout, indicating ganged products on the same sheet.
- In `Sample_B`, MXML `ComponentPool` describes a 3-up layout with rotated component instances, matching the repeated three-panel `ContentObject` geometry in the JDF.

Stability: **Advisory** (five samples).

### RunList / FileSpec
Observed in `Sample_A.jdf`, `Sample_B.jdf`, `Sample_C.jdf`, `Sample_D.jdf`, and `Ganged_Postcards.jdf`:

- Document RunList includes `NPage="520"` with a `PageList` of `PageData` entries; `LayoutElement` is present with `IsBlank="true"` and no `FileSpec`.
- Marks RunList includes a `LayoutElement` with `FileSpec` pointing to `./Content/Sample_A_Marks.pdf`.
- Marks RunList contains a `SeparationSpec` with `HDM:Type="DieLine"` and `Name="ProofColor"`.

Stability: **Advisory** (single sample).

### Media (paper/plate)
Observed in `Sample_A.jdf`:

- No `Media` resources present; sheet sizing appears embedded on `Surface` via `SSi:Dimension` and `SSi:MediaOrigin`.

Stability: **Advisory** (single sample).

### ConventionalPrintingParams
Observed in `Sample_A.jdf`:

- No `ConventionalPrintingParams` resource; work style appears encoded on `Sheet` via `SSi:WorkStyle`.

Stability: **Advisory** (single sample).

### StrippingParams / BinderySignature / FoldingParams
Observed in `Sample_A.jdf`:

- No `StrippingParams`, `BinderySignature`, or `FoldingParams` resources (folding intent appears in MXML instead).

Stability: **Advisory** (five samples).

### Component / Assembly / CuttingParams
Observed in `Sample_A.jdf`:

- No `Component`, `Assembly`, or `CuttingParams` resources.

Stability: **Advisory** (single sample).

<a id="Metrix-Gaps"></a>
## Missing or reduced data in Metrix (candidate list)
These are hypotheses to verify against samples.
Observed gaps in `Sample_A.jdf`, `Sample_B.jdf`, `Sample_C.jdf`, `Sample_D.jdf`, and `Ganged_Postcards.jdf` (relative to Signa exports):

- Signa-specific HDM metadata (SignaBLOB, SignaJob, SignaGenContext) is absent.
- No Media resources; sheet sizes appear only on `Surface`.
- No ConventionalPrintingParams, StrippingParams, BinderySignature, Component, or Assembly resources.
- Marks are present as `MarkObject` and a marks RunList, but without per-mark parameter metadata.

Stability: **Advisory** (five samples).

<a id="Metrix-Testing"></a>
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

Observed in Cockpit with normalized Sample_A bundle (`Sample_A.jdf/data.jdf`):

- Import errors: “The layout to be imported is empty.” and “Can’t copy the Signa Station data file.”
- Previews render fronts at bottom-left and backs at bottom-right of the plate.
- No page list created.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 2 with content PDF, no SignaBLOB URL):

- Import warning: “The layout to be imported is empty.”
- WorkStyle shows as Sheetwise for all sheets.
- Paper size shows as 26x20 on all sheets; no other paper details shown.
- Plate size shows as 29.33x25.98 on all sheets.
- “Allow spot colors for BCMY” is enabled.
- Previews still show off-center sheets (fronts bottom-left, backs bottom-right).
- No page list created.
- No error about missing Signa Station file.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 3 with Document page mapping enabled):

- No observable changes from iteration 2 (same errors, previews, and missing page list).

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 4 with PagePool/Output RunLists enabled):

- No observable changes from iteration 3.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 5 with content placement grid):

- “Layout is valid.”
- Page list created, but only one position (label “1”) repeated 612 times.
- Previews still incorrect (off-center).
- Sheet sizes, plate sizes, and WorkStyle remain unchanged.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 6 with Metrix Ord mapping):

- Page list now contains positions 0, 2, 4, 6, 8, 10, 12, 14, 16 (each repeated 68 times).
- No other observable changes.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 7 with Metrix CTM/TrimBox placement):

- Page list expands to 518 positions with uneven duplication counts (e.g., 0×3, 2×2, 4×2, 98–101×4).
- Cover front placements look closer to correct; cover back appears but should not, and runs off sheet.
- Previews still anchored bottom-left/bottom-right.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 8 with simplex back suppression + WorkStyle mapping):

- No observable changes from iteration 7.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 9 with MediaOrigin offset applied to content placement):

- Cover layout may have shifted slightly.
- No other observable changes.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 10 with PaperRect centering + TransferCurvePool CTM):

- Sheet previews appear correctly centered in thumbnails and Layout Preview.
- No other problems resolved.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 11 with uniform WorkStyle rule):

- WorkStyle shows Perfecting on all sheets except the first, which shows Single-Sided.
- The first sheet still contains two sides despite Single-Sided WorkStyle.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 12 with per-sheet simplex back suppression):

- Sheet 1 is truly single-sided; sheets 2+ are perfecting.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 13 with label mapping from MXML/PageList):

- Page list appears correct.
- Sheet and plate sizes still incorrect.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 14 with per-sheet Media + PaperRect + TransferCurvePool partitions):

- Sheet and plate sizes now appear correct.
- Page blocks are offset to the right and high, running off the top edge of the sheet.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 15 with ContentObject placement aligned to Metrix CTM, no MediaOrigin shift):

- Layout previews appear correct (subject to visual confirmation against Metrix).
- Page blocks no longer appear offset in the preview.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 16 with paper metadata injected from MXML):

- Imposed PDF generation fails with “No PDF file created” and “Error during imposition”.
- Failure occurs for both bundled `content.pdf` and external PDFs.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 17 with MarkObject geometry from Metrix):

- Imposed PDF generation succeeds (no imposition error).
- Marks PDF pages are only merged on Sheet 1 (page 1) and Sheet 2 front (page 3); remaining sheets have no marks.
- Expected marks pairing (pages 1&2 on Sheet 1, pages 3&4 on Sheet 2 front) is not honored.
- Trim box appears only on Sheet 1 and Sheet 2 front; missing on other sheets.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 18 with non-blank content assignment):

- Marks that rely on process/BCMY separations appear correctly when non-blank PDFs are assigned.
- Prior missing marks were caused by assigning blanks (no separations to map).

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_A bundle (iteration 19 with CuttingParams/StrippingParams preview helpers):

- Layout Preview still crashes Acrobat.
- Imposed PDF fails again with "Error during imposition".

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Ganged_Postcards bundle (ganged postcards):

- Page list labels are correct and count matches expectation.
- Thumbnail Preview and Layout Preview do not crash Acrobat and look correct.
- Imposed PDF appears correct.
- Artwork assignment only works for the 9x6 position (TAR52209_65362).
- Other positions reject artwork due to TrimBox size mismatch; Cockpit reports:
  - TrimBox is 7.0 x 5.0 in, but layout positions expect 5.0 x 7.0 in for position 5 (repeated).

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Ganged_Postcards bundle (no `HDM:PageOrientation`/`HDM:FinalPageBox` on ContentObjects):

- All pages assign correctly.
- Previews look correct.
- Imposed PDF looks correct.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Ganged_Postcards bundle (`HDM:FinalPageBox` present, `HDM:PageOrientation` forced to 0 for rotated CTMs):

- No complaints on page assignment.
- Previews look normal.
- Imposed PDF looks normal.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_D bundle (saddle stitch):

- No complaints on page assignment.
- Layout previews and imposed PDF look correct.
- WorkStyle looks correct.
- Paper attributes appear correct.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_C bundle:

- Pages assign without complaint.
- Sheet 1 (cover) should be Work-and-Turn but shows a back; both sides are set to Work-and-Turn.
- Cover back contains extra pages (about 9 assigned).
- Sheet 2 and 3 backs have incorrect TrimBox/paper in the imposed PDF.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_C bundle (Work-and-Turn treated as single-side layout):

- All now appears normal.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized Sample_B bundle:

- All looks normal.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized MultiProduct_2Books bundle (multi-product):

- Two products created (Book 1, Book 2).
- Two page lists created.
- Cover labels for Book 2 continue the overall numbering (Cover-593, Cover-594, Cover-1099, Cover-1100) instead of resetting to 1.
- Previews and imposed PDF look normal.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized MultiProduct_2Books bundle (multi-product, per-product label reset):

- Page list looks as expected; labels reset per product with product name prefix.
- Products are correctly separated.
- Previews and imposed PDF look as expected.

Stability: **Advisory** (single sample run).

Observed in Cockpit with normalized MultiProduct_2Books bundle (deduped label prefixes):

- Labels remain correct after de-duplicating product prefixes.

Stability: **Advisory** (single sample run).

<a id="Metrix-WhatWorks"></a>
## What now works (current normalization)
Observed in Cockpit across normalized samples (Sample_A, Ganged_Postcards, Sample_D, Sample_C, Sample_B):

- Import succeeds without errors.
- Page assignment works without complaints (including ganged postcards).
- Layout previews and imposed PDFs look normal.
- WorkStyle aligns with Metrix intent (SS/PE/WAT), with simplex back suppression.
- Sheet/plate sizes and TrimBox alignment look correct.
- Paper attributes (Description/Brand/ProductID/Weight/Thickness/Grade/Manufacturer/GrainDirection) are populated.
- "Allow spot colors for BCMY" remains enabled.
- Sample_A shows no significant differences when compared to a Signa layout.

Stability: **Advisory** (single runs per sample; cross-check against Metrix previews pending).

<a id="Metrix-Recipe"></a>
### Current normalization recipe (summary)
- Build per-sheet Media (Paper/Plate) partitions with `Dimension`, plus signature-level `MediaRef`.
- Populate `HDM:PaperRect` per side from `SSi:MediaOrigin` (or centered fallback) and align TransferCurvePool CTMs.
- Normalize TransferCurvePool per signature and attach `TransferCurvePoolRef` at signature-level `Layout`.
- Use Metrix ContentObject geometry as-is (CTM/TrimCTM/ClipBox/TrimBox1) without applying MediaOrigin shift.
- Map `SSi:WorkStyle` to Signa WorkStyle; treat Simplex, Work-and-Turn, and Work-and-Tumble as single-side layouts.
- Set `HDM:PageOrientation=0` for 90/270 CTM rotations to avoid TrimBox mismatches in Cockpit.
- For multi-product jobs, emit `HDM:SignaJobPart` entries and tag each `ContentObject` with `HDM:JobPart` based on MXML product page ranges.
- Reset per-product labels for multi-product jobs (e.g., `Book 2 Cover-1`).
- Normalize marks RunList structure + SeparationSpec placeholders.
- Include X/Z as map-rel placeholders in marks RunList for 6-slot color bars (BCMYXZ).
- Apply MXML paper metadata to Paper Media attributes.
- Remove `HDM:SignaBLOB` URL; preserve other Signa/HDM/SSi metadata.

<a id="Metrix-Defaults"></a>
### Defaults
- The transformer emits Cockpit‑ready JDFs without requiring CLI flags.
- Work-and-Turn/Work-and-Tumble are treated as single-side layouts by default.
- `HDM:PageOrientation` is normalized for rotated CTMs (90/270 → 0).
- Multi-product jobs emit `HDM:SignaJobPart` + `HDM:JobPart` with per-product labels.
- `content.pdf` FileSpec is omitted by default (marks remain included).

<a id="Metrix-PreviewCrash"></a>
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

Working theory (PageOrientation vs TrimBox):

- `HDM:PageOrientation` communicates the intended logical page rotation (0/90/180/270) so downstream tools can interpret trim/clip geometry consistently.
- In Metrix ganged layouts, CTM already rotates the page while TrimBox remains in the original orientation; Cockpit appears to apply `PageOrientation` again, effectively swapping expected trim dimensions (e.g., 7x5 → 5x7).
- Forcing `PageOrientation="0"` for 90/270 CTMs avoids the double-rotation signal and restores page assignment without harming previews/imposed PDFs (Ganged_Postcards).

### Normalization changes that enabled the current wins
These map specific normalization steps to the observed Cockpit outcomes across samples.

| Change | Result | Evidence |
| --- | --- | --- |
| Add `Media` (Paper/Plate) with per-sheet `Dimension` + `SignatureName/SheetName` partitions; add signature-level `MediaRef`; clear top-level `Dimension` when mixed | Sheet and plate sizes now correct. | Iteration 14 (sizes correct). |
| Add per-sheet `HDM:PaperRect` using `SSi:MediaOrigin` (or centered fallback) + per-sheet `TransferCurveSet` Paper CTM | Layout previews centered and reasonable. | Iteration 10 and iteration 15 (previews correct). |
| Normalize `TransferCurvePool` per signature + `TransferCurvePoolRef` at signature-level `Layout` | Mixed sheet sizes (cover vs text) center correctly; avoids text sheets anchoring to cover offset. | Iteration 22 (signature-level transfer curves). |
| Use Metrix `ContentObject` geometry (CTM/TrimCTM/ClipBox/TrimBox1) without applying `MediaOrigin` shift | Page blocks align on sheets; no offset to the right/top. | Iteration 15 (page blocks no longer offset). |
| Map `SSi:WorkStyle` to Signa WorkStyle + suppress back side for simplex sheets | WorkStyle correct per sheet; cover is single-sided. | Iteration 12 (simplex back suppression works). |
| Treat Work-and-Turn/Work-and-Tumble as single-side layouts | Removes erroneous back side and extra page placements on WAT/WTT sheets. | Sample_C (cover fixed). |
| Normalize `HDM:PageOrientation` to `0` for 90/270 CTMs | Avoids TrimBox size mismatch during page assignment in ganged layouts. | Ganged_Postcards (page assignment fixed). |
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
| Add `ConventionalPrintingParams` with `WorkStyle` partitions | Failed | WorkStyle appears and is editable in Cockpit. | Use `Sample_A.jdf` (mixed SS/PE) for coverage. |
| Add `Media` resources + `HDM:PaperRect` alignment | Pass | Previews are centered and match sheet dimensions. | Verified on Sample_A/Sample_D/Sample_B. |
| Normalize `TransferCurvePool` per signature | Pass | Mixed sheet sizes center correctly (cover vs text). | Use `Sample_A.jdf` (cover + text sizes). |
| Normalize marks RunList structure + BCMY `SeparationSpec` placeholders | Pass | "Allow spot colors for BCMY" is enabled and spot mapping works. | Verified with Sample_A (non-blank content). |

<a id="Metrix-Strategies"></a>
## Conversion strategies (Metrix -> Prinect-ready JDF)
This section will formalize a practical normalization plan.

### Design philosophy (Metrix → Prinect)
This converter prioritizes:
- correct geometry
- correct press behavior
- correct page assignment
over completeness of JDF semantics.

Note: This project does not attempt to generate bindery-executable JDF (folding machines, cutters, stitchers). Bindery components require licensed Prinect modules and physical device integration, which are not available in this environment.

1) Intake and classification
- Parse Metrix JDF + MXML; determine product count, sheet sizes, and workstyles.
- Classify job as single-product vs multi-product; note any ganged layouts or mixed sheet sizes.

2) Core structure normalization (always-on)
- Ensure per-sheet `Media` (Paper/Plate) partitions with `Dimension`; add signature-level `MediaRef`.
- Emit per-side `HDM:PaperRect` and align `TransferCurvePool` CTMs.
- Normalize `TransferCurvePool` per signature and attach `TransferCurvePoolRef` at signature-level `Layout`.
- Use Metrix `ContentObject` geometry as-is (CTM/TrimCTM/ClipBox/TrimBox1) without additional MediaOrigin shifting.
- Map `SSi:WorkStyle` to Signa WorkStyle; treat Simplex, Work-and-Turn, and Work-and-Tumble as single-side layouts.
- Normalize `HDM:PageOrientation` to `0` for 90/270 CTMs to avoid TrimBox size mismatches in Cockpit.

3) Marks and runlist normalization
- Normalize marks RunList structure and `SeparationSpec` placeholders for BCMY behavior.
- Keep marks page counts consistent per side; reset logical pages per sheet if trim boxes disappear in multi-signature jobs.

4) Paper metadata enrichment
- Pull paper attributes from MXML stock sheets (Brand, Description, Grade, ProductID, Weight, Thickness, GrainDirection).
- Apply uniformly to `Media` leaves; keep top-level `Media` attributes minimal.

5) Multi-product handling (when >1 MXML product)
- Emit `HDM:SignaJobPart` entries for each product.
- Tag `ContentObject` with `HDM:JobPart` based on `Ord` ranges derived from product page counts.
- Reset labels per product and avoid redundant prefixes (e.g., `Book 2 Cover-1`).

6) Preserve Signa/HDM/SSi transport data
- Keep Signa metadata unless it blocks import; remove only `HDM:SignaBLOB` URLs since we cannot produce the SDF.

7) Validation and Cockpit checks
- Validate structure (Media, PaperRect, TransferCurvePool, RunLists) and import into Cockpit.
- Confirm page assignment, page lists, workstyles, previews, and imposed PDFs across representative samples.

### Known pitfalls (and mitigations)
- **Work-and-Turn backs appear unexpectedly.** Treat WAT/WTT as single-side layouts to avoid extra back-side placements (fixed in Sample_C).
- **Page assignment fails on rotated ganged layouts.** Force `HDM:PageOrientation=0` for 90/270 CTMs to avoid TrimBox size swaps (fixed in Ganged_Postcards).
- **Layout Preview crashes Acrobat.** Avoid injecting CuttingParams/StrippingParams preview helpers; ensure PaperRect + TransferCurvePool alignment.
- **TrimBox missing on some sheets in multi-signature jobs.** Reset marks logical pages per sheet when trim boxes disappear.
- **Missing paper metadata.** Populate Paper Media attributes from MXML stock sheets (Brand/ProductID/Weight/Thickness/etc).
- **X/Z marks mapped to 20% black.** On a 6-slot bar (BCMYXZ), mapping X/Z as map-rel placeholders matches Python/Signa behavior and prevents unintended 20% black fills on 4‑color presses.
- **Implicit mapping can work, but is inconsistent.** Some Signa/Python tickets omit explicit X/Z separations yet Cockpit still maps correctly, likely from the marks PDF/press defaults. We still emit X/Z as map-rel placeholders to avoid fallback remapping.

<a id="Metrix-Normalization"></a>
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

<a id="Metrix-Mapping"></a>
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

Evidence anchors: `Sample_A.mxml`, `Sample_B.mxml`, `Sample_C.mxml`, `Sample_D.mxml`, `Ganged_Postcards.mxml`.

## Open questions
- Which Metrix versions/builds beyond 24.1 are in scope?
- Which JDF export settings beyond "Impose To: Plate" affect structure?
- Are there Metrix-specific vendor namespaces that need preserving?

## Resolved questions (current samples)
- MXML `Product/@Type="Bound"` appears on saddle-stitched products (e.g., Sample_C, Sample_D), so it is not exclusive to perfect binding.

## Appendix

## Glossary (AI aliases)
- WorkAndBack = Sheetwise
- WorkAndTurn = WAT
- WorkAndTumble = WTT
- Simplex = Single-Sided

## Glossary (Metrix/Prinect terms)
- WorkStyle: Print-side model (Perfecting, WorkAndBack, WorkAndTurn, WorkAndTumble, Simplex).
- SurfaceContentsBox: Sheet coordinate box used by Metrix for layout geometry.
- PaperRect: Signa/HDM sheet rectangle used for TrimBox alignment in Cockpit previews.
- RunList: JDF resource linking marks/document PDFs and separation specs.
- JobPart: Product-part grouping used for multi-product/ganged layouts.

## Change log (summary)
- Added data policy and sample intake guidance aligned to private sample storage.
- Consolidated normalization notes (defaults, design philosophy, deltas, mappings).
- Sanitized sample references for public sharing.

# Signa 21 JDF Dialect for Imposition

## Purpose and scope
- Capture the Signa 21 JDF dialect used for imposition so it is reusable across projects.
- Human readable, explicit, and code agnostic (documentation only, no XSD or schema).
- Focus on elements and structure that describe imposition, with process types as context.
- Based on Signa 21 samples in `Signa_Samples/`; earlier builds may appear but the target is 21.x.
- Applies to Signa 21.x; validated against builds 21.00–21.10 (sample corpus range).
- This document describes JDFs exported by Signa; when Signa imports JDFs it expects product intent JDFs rather than these output-style tickets.
- Companion primers:
  - `docs/general_print_knowledge_for_jdf.md` covers print/imposition concepts that JDF does not fully express.
  - `docs/jdf-elements-attributes.md` lists observed JDF elements/attributes with brief descriptions.
- Samples are evidence, not dependencies: they are optional, replaceable over time, and used like unit-test fixtures or academic citations to support claims.

## Out of scope
- Full CIP4 JDF coverage beyond the imposition-related resources listed here.
- JMF messaging, device integration, or workflow automation topics.
- Validation schemas or code generation.

## This document does not attempt to
- Infer Heidelberg internal algorithms.
- Predict future Signa releases.
- Guarantee bindery correctness without geometry validation.
- Replace physical print knowledge.

## Evidence model
- This document is based on a large internal corpus of Signa-exported JDFs across multiple Signa 21.x builds.
- Individual sample filenames are cited as anchors, not as exhaustive proof.
- Absence of a cited sample does not invalidate the described behavior.
- When available, counts or notes like “Observed across all work‑and‑turn samples” indicate the weight of evidence.
- Inline XML excerpts are avoided unless strictly necessary, to keep focus on behavior rather than syntax.

## Conventions used in this document
- MUST/SHOULD/MAY indicate observed expectations across the sample set.
- Evidence lists sample files that exhibit the behavior.
- Working theory is used when behavior is observed but the reason is inferred.
- When a value is optional or inconsistent across samples, it is called out explicitly.
- Spec references point to JDF 1.7 section numbers (see `JDF Specification 1.7.pdf`).

## Stability levels (interpretation guidance)
- **Stable** — relied on for interpretation (high confidence and consistent across samples).
- **Advisory** — useful when present but safe to ignore (low impact if missing).
- **Transport-only** — required for Signa/Cockpit handoff; not part of the logical model.
- **Decorative** — preserve verbatim, never interpret (opaque or vendor-specific payloads).

## Sample index (evidence set)
- Note: Sample JDF filenames in this repo are renamed for clarity and to avoid collisions. Signa exports a `<jobnumber>.jdf` bundle that contains `data.jdf` (the ticket) and related assets (e.g., marks PDF).
- These samples are references only; the dialect rules stand on their own and should remain valid even if the sample set changes.
- `Signa_Samples/SingleSheet-SingleSide.jdf` - Simplex single sheet.
- `Signa_Samples/SingleSheet-WorkandTurn.jdf` - Work-and-turn single sheet.
- `Signa_Samples/SingleSheet-Sheetwise.jdf` - Work-and-back (sheetwise) single sheet.
- `Signa_Samples/SS-SingleSheet-Perfecting.jdf` - Perfecting single sheet.
- `Signa_Samples/GangedPostcards-Perfecting.jdf` - Perfecting with multiple bindery signatures.
- `Signa_Samples/GangedPostcards-WorkandTurn.jdf` - Work-and-turn with ganging.
- `Signa_Samples/SS-BookText-Perfecting.jdf` - Book text with finishing chain.
- `Signa_Samples/SS-BookCover-WorkandTurn.jdf` - Cover with work-and-turn.
- `Signa_Samples/SS-Book-SelfCover-Perfecting.jdf` - Self cover, perfecting.
- `Signa_Samples/PB-BookText2Versions-Perfecting.jdf` - Two job parts/products, perfecting.
- `Signa_Samples/PB-BookCover2Versions-Ganged-Sheetwise.jdf` - Two job parts/products, ganged, sheetwise.
- `Signa_Samples/MultiSig-Perfecting.jdf` - Many signatures, perfecting.
- `Signa_Samples/MixedWorkStyles.jdf` - Mixed work styles in one job.
- `Signa_Samples/Saddle28P.jdf` - Saddle‑stitched self‑cover, 4+16+8 signatures, perfecting.
- `Signa_Samples/Saddle28PWATCover.jdf` - Saddle‑stitched with cover set to work‑and‑turn.
- `Signa_Samples/Saddle28PWATCoverSW8.jdf` - Saddle‑stitched with work‑and‑turn cover and sheetwise 8‑page signature.
- `Signa_Samples/Saddle28PWATCoverSW8NoCreep.jdf` - Same as above with gapped imposition, creep still off.
- `Signa_Samples/Saddle28PWATCoverSW8CreepOffset.jdf` - Same layout with creep enabled (offset method).
- `Signa_Samples/Saddle28PWATCoverSW8CreepScaling.jdf` - Same layout with creep enabled (scaling method).
- `Signa_Samples/Saddle28PWATCoverSW8AndMontage.jdf` - Saddle‑stitched with a montage as a separate product part.

## Signa export bundle (observed)
Example bundle: `Signa_Samples/P2452 Cdn Paediatric_P2452 CdnPaed.jdf`.
- `data.jdf` is the main ticket and contains the same structure discussed in this document.
- `SignaData.sdf` is referenced by `HDM:SignaBLOB` and is used by Signa/Cockpit for "edit layout". (Stability: Transport-only)
- `data.pdf` is referenced by RunList `FileSpec` entries as the source PDF in this sample.
- `report.pdf` is present in the bundle but not referenced in `data.jdf` in this sample.
- Marks PDFs are referenced by the RunList with `ProcessUsage="Marks"` and contain printer marks (color bars, side guides, crop marks, etc.). Filenames vary by export.

## PDF observations (samples)
- Page size matches `Layout/@SurfaceContentsBox` and plate `Media/@Dimension` (points), supporting JDF default length units.
- `MediaBox`, `CropBox`, and `BleedBox` match sheet size; `TrimBox` matches `HDM:PaperRect` in the JDF (e.g., `Signa_Samples/SingleSheet-SingleSide.pdf`, `Signa_Samples/SingleSheet-Sheetwise.pdf`, `Signa_Samples/SS-SingleSheet-Perfecting.pdf`).
- Marks PDFs can have more pages than printed sides; for example, `Signa_Samples/SingleSheet-SingleSide.pdf` has 2 pages despite simplex, reflecting different mark sets.
- Page rotation is 0 in sampled PDFs; orientation is carried by JDF transforms.
- PDF page count matches the RunList linked with `ProcessUsage="Marks"` across all sampled pairs.
- Document and PagePool RunList counts are not expected to match marks PDFs; they describe source document/page pool pages, not sheet/plate marks.
- Marks PDFs may contain textual labels or placeholders (e.g., `Plate Control Strip`, `$[Sheet]`, `$[jobid]`, `$[jobname]`) but do not expose JDF mark type names like `SideGuide` or `FoldingMark`.
- Marks PDFs inspected do not expose layer/OCG names or semantic object names; XObjects are generically named (e.g., `/pssMO1_1`) and content streams contain no mark-type tokens. Evidence: `Signa_Samples/SingleSheet-SingleSide.pdf`, `Signa_Samples/SingleSheet-Sheetwise.pdf`.
- Marks PDFs expose spot color placeholders via `/Separation` and `/DeviceN`: `All`, `B`, `C`, `M`, `Y`, `X`, `Z`, `U`, `V`, `S1...S8`, with `HDM_DarkColor` present in some files; `/DeviceN` names are typically `B`, `C`, `M`, `Y`. Evidence: `Signa_Samples/SingleSheet-SingleSide.pdf`, `Signa_Samples/SS-SingleSheet-Perfecting.pdf`.

| PDF | Pages | Page size (pts) | JDF | SurfaceContentsBox match | Plate Dimension match |
| --- | ---: | --- | --- | --- | --- |
| GangedPostcards-Perfecting.pdf | 4 | 2990.55 x 2298.90 | GangedPostcards-Perfecting.jdf | yes | yes |
| GangedPostcards-WorkandTurn.pdf | 2 | 2111.81 x 1870.87 | GangedPostcards-WorkandTurn.jdf | yes | yes |
| MixedWorkStyles.pdf | 8 | 2990.55 x 2298.90 | MixedWorkStyles.jdf | yes | yes |
| MultiSig-Perfecting.pdf | 276 | 2919.69 x 2239.37 | MultiSig-Perfecting.jdf | yes | yes |
| PB-BookCover2Versions-Ganged-Sheetwise.pdf | 8 | 2111.81 x 1870.87 | PB-BookCover2Versions-Ganged-Sheetwise.jdf | yes | yes |
| PB-BookText2Versions-Perfecting.pdf | 8 | 2990.55 x 2298.90 | PB-BookText2Versions-Perfecting.jdf | yes | yes |
| SS-Book-SelfCover-Perfecting.pdf | 16 | 2990.55 x 2298.90 | SS-Book-SelfCover-Perfecting.jdf | yes | yes |
| SS-BookCover-WorkandTurn.pdf | 2 | 2111.81 x 1870.87 | SS-BookCover-WorkandTurn.jdf | yes | yes |
| SS-BookText-Perfecting.pdf | 4 | 2919.69 x 2239.37 | SS-BookText-Perfecting.jdf | yes | yes |
| SS-SingleSheet-Perfecting.pdf | 4 | 2919.69 x 2239.37 | SS-SingleSheet-Perfecting.jdf | yes | yes |
| SingleSheet-Sheetwise.pdf | 4 | 2111.81 x 1870.87 | SingleSheet-Sheetwise.jdf | yes | yes |
| SingleSheet-SingleSide.pdf | 2 | 2990.55 x 2298.90 | SingleSheet-SingleSide.jdf | yes | yes |
| SingleSheet-WorkandTurn.pdf | 2 | 2111.81 x 1870.87 | SingleSheet-WorkandTurn.jdf | yes | yes |

| PDF | Pages | MediaBox (pts) | TrimBox (pts) | SurfaceContentsBox match | PaperRect match |
| --- | ---: | --- | --- | --- | --- |
| GangedPostcards-Perfecting.pdf | 4 | 0.00 0.00 2990.55 2298.90 | 55.28 121.89 2935.28 2137.89 | yes | yes |
| GangedPostcards-WorkandTurn.pdf | 2 | 0.00 0.00 2111.81 1870.87 | 119.91 96.38 1991.91 1536.38 | yes | yes |
| MixedWorkStyles.pdf | 8 | 0.00 0.00 2990.55 2298.90 | 559.28 121.89 2431.28 1561.89 | yes | yes |
| MultiSig-Perfecting.pdf | 276 | 0.00 0.00 2919.69 2239.37 | 199.84 93.54 2719.84 1749.54 | yes | yes |
| PB-BookCover2Versions-Ganged-Sheetwise.pdf | 8 | 0.00 0.00 2111.81 1870.87 | 155.91 96.38 1955.91 1464.38 | yes | yes |
| PB-BookText2Versions-Perfecting.pdf | 8 | 0.00 0.00 2990.55 2298.90 | 127.28 121.89 2863.28 1921.89 | yes | yes |
| SS-Book-SelfCover-Perfecting.pdf | 16 | 0.00 0.00 2990.55 2298.90 | 199.28 121.89 2791.28 1849.89 | yes | yes |
| SS-BookCover-WorkandTurn.pdf | 2 | 0.00 0.00 2111.81 1870.87 | 119.91 96.38 1991.91 1536.38 | yes | yes |
| SS-BookText-Perfecting.pdf | 4 | 0.00 0.00 2919.69 2239.37 | 163.84 93.54 2755.84 1821.54 | yes | yes |
| SS-SingleSheet-Perfecting.pdf | 4 | 0.00 0.00 2919.69 2239.37 | 163.84 93.54 2755.84 1821.54 | yes | yes |
| SingleSheet-Sheetwise.pdf | 4 | 0.00 0.00 2111.81 1870.87 | 119.91 96.38 1991.91 1536.38 | yes | yes |
| SingleSheet-SingleSide.pdf | 2 | 0.00 0.00 2990.55 2298.90 | 235.28 121.89 2755.28 1777.89 | yes | yes |
| SingleSheet-WorkandTurn.pdf | 2 | 0.00 0.00 2111.81 1870.87 | 119.91 96.38 1991.91 1536.38 | yes | yes |

| PDF | Pages | Marks RunList NPage | Document RunList NPage | PagePool NPage |
| --- | ---: | ---: | ---: | ---: |
| GangedPostcards-Perfecting.pdf | 4 | 4 | 6 | 4 |
| GangedPostcards-WorkandTurn.pdf | 2 | 2 | 8 | 4 |
| MixedWorkStyles.pdf | 8 | 8 | 16 | 16 |
| MultiSig-Perfecting.pdf | 276 | 276 | 1100 | 1136 |
| PB-BookCover2Versions-Ganged-Sheetwise.pdf | 8 | 8 | 4 | 36 |
| PB-BookText2Versions-Perfecting.pdf | 8 | 8 | 32 | 36 |
| SS-Book-SelfCover-Perfecting.pdf | 16 | 16 | 60 | — |
| SS-BookCover-WorkandTurn.pdf | 2 | 2 | 4 | 32 |
| SS-BookText-Perfecting.pdf | 4 | 4 | 28 | 32 |
| SS-SingleSheet-Perfecting.pdf | 4 | 4 | 16 | 16 |
| SingleSheet-Sheetwise.pdf | 4 | 4 | 2 | 4 |
| SingleSheet-SingleSide.pdf | 2 | 2 | 1 | 1 |
| SingleSheet-WorkandTurn.pdf | 2 | 2 | 2 | 2 |

## High-level structure (typical)
```
JDF (ProcessGroup)
  AuditPool
  Comment (CIP3Adm*)
  ResourcePool
    CustomerInfo
    Layout (top-level)
      HDM:SignaBLOB / HDM:SignaJDF / HDM:SignaJob / HDM:SignaGenContext
      Layout (Signature)
        Layout (Sheet)
          Layout (Side)
            MarkObject / ContentObject
          MediaRef
      TransferCurvePoolRef
    ConventionalPrintingParams
    Component (Sheet, Block, FinalProduct)
    TransferCurvePool
    CuttingParams / FoldingParams / Assembly (as needed)
    RunList (Document, Marks, PagePool)
    StrippingParams / BinderySignature (as needed)
    Media (Paper, Plate)
  ResourceLinkPool (links + CombinedProcessIndex)
```

## Root element: JDF (ProcessGroup)
- Role: Job container for a process group that includes Imposition and downstream processes.
- Key attributes:
  - `Type="ProcessGroup"` and `xsi:type="ProcessGroup"` are present in all samples.
  - `Types` lists the process chain in order (Imposition, ConventionalPrinting, Cutting, Folding, etc.).
  - `Version="1.3"` in all samples; `MaxVersion` varies (1.6 or 1.7).
  - `ICSVersions` appears in some samples and is absent in others.
  - XML namespaces observed: `xmlns="http://www.CIP4.org/JDFSchema_1_1"`, `xmlns:HDM="www.heidelberg.com/schema/HDM"`, `xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"`.
- Why order matters: `CombinedProcessIndex` in `ResourceLinkPool` uses the position in `Types`.
- Evidence:
  - Order and CombinedProcessIndex: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.
  - `ICSVersions` present: `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
  - `ICSVersions` absent: `Signa_Samples/SingleSheet-Sheetwise.jdf`.

## AuditPool and Comments
- `AuditPool/Created` provides generator and Signa build metadata (`Author` includes "PrinectSignaStation 21.xx build").
- `Comment` entries commonly include CIP3 fields:
  - `CIP3AdmArtist` (user or operator name).
  - `CIP3AdmMake` (Heidelberger Druckmaschinen AG).
  - `CIP3AdmJobCode` (job identifier).
- Stability: **Decorative** (provenance/metadata only).
- Evidence:
  - `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.

## ResourcePool core (imposition-relevant)

### Layout (imposition geometry)
- Role: Primary imposition geometry definition.
- Top-level attributes:
  - `PartIDKeys="SignatureName SheetName Side"` is consistent across samples.
  - `HDM:ChangeInfo` is present; likely Signa internal change counter.
- Stability: **Stable** for geometry and partitioning; `HDM:ChangeInfo` is **Decorative**.
- Signa-specific children:
  - `HDM:SignaBLOB` with `URL="SignaData.sdf"` and timestamp.
  - `HDM:SignaJDF` with `URL="data.jdf"` (companion file).
  - `HDM:SignaJob` and `HDM:SignaJobPart` (job-part naming).
  - `HDM:SignaGenContext` with Signa version/build and environment fields.
- Nested structure:
  - `Layout` (Signature level): `SignatureName` + `Name` + `HDM:OrigNameBySigna`.
  - `Layout` (Sheet level): `SheetName`, `SourceWorkStyle`, `SurfaceContentsBox`.
  - `Layout` (Side level): `Side="Front|Back"`, `HDM:PaperRect`, `DescriptiveName`.
  - `MarkObject` and `ContentObject` are children of side-level Layout.
  - `MediaRef` entries appear under the sheet-level Layout (typically paper + plate).
- Working theory: `SurfaceContentsBox` and `HDM:PaperRect` define the sheet content bounds and paper rectangle in the same coordinate system as `ContentObject` and `MarkObject`.
- Evidence:
  - Structure and Signa-specific nodes: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Work-and-turn single-side layout: `Signa_Samples/SingleSheet-WorkandTurn.jdf`.
- Working theory: `HDM:SignaGenContext` is the primary provenance block for Signa build, environment, and output profile tracking. Preserve it for traceability.

### MarkObject
- Role: Register marks and control strips for the sheet side.
- Key attributes: `CTM`, `ClipBox`, `Ord`.
- Children:
  - `RegisterMark` with `MarkType` (SideGuide, FoldingMark) and `Rotation`.
  - `ColorControlStrip` with `StripType`, `Size`, `Center`.
- Stability: **Stable** when marks PDF imposition is required; mark geometry is needed for imposed PDF generation.
- Why keep marks in JDF when a marks PDF exists:
  - The JDF encodes mark intent (e.g., SideGuide vs. FoldingMark) in a machine-readable form; the PDF is only a rendered result.
  - Systems can validate required mark types and positions without image analysis.
  - Marks can be regenerated or swapped for device-specific requirements if the PDF is missing or not trusted.
  - Coordinates are aligned with the imposition model, enabling automated layout edits and checks.
- Scope note: detailed mark geometry is often absent in non‑Signa JDFs (e.g., Metrix) and Prinect accepts marks PDFs without it. We treat per‑mark geometry as optional metadata and do not aim to reproduce it outside Signa exports.
- Evidence:
  - Register marks and control strips: `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.

### ContentObject
- Role: Individual imposed page placement.
- Key attributes:
  - `CTM`, `ClipBox`, `TrimCTM`, `TrimSize`, `Ord`, `DescriptiveName`.
  - `HDM:FinalPageBox`, `HDM:PageOrientation` (0/90/180/270).
  - `HDM:AssemblyIDs` and `HDM:AssemblyFB` (Front or Back).
- Stability: **Stable** for placement and page list behavior; `PositionX`/`PositionY` are **Advisory** alignment hints when present.
- Work-style implications:
  - In WorkAndTurn, a single side layout (Side="Front") contains both `HDM:AssemblyFB="Front"` and `HDM:AssemblyFB="Back"` placements.
  - In Perfecting and WorkAndBack, front and back placements are separated into side-level Layouts.
- Evidence:
  - WorkAndTurn mixed front/back in one side: `Signa_Samples/SingleSheet-WorkandTurn.jdf`.
  - Perfecting with distinct front/back sides: `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
- Working theory: `HDM:AssemblyFB` represents the final product face, independent of plate side, which allows WorkAndTurn and simplex layouts to map both faces under a single plate side.

### WorkStyle vocabulary and side handling
- WorkStyle values observed: `Perfecting`, `WorkAndBack`, `WorkAndTurn`, `Simplex`.
- Signa uses `WorkAndBack` where file naming and job intent indicate sheetwise; no `WorkStyle="Sheetwise"` appears in the sample set.
- Primer (user‑provided):
  - Sheetwise: separate plates for front/back; print one side, flip with same gripper edge, print reverse.
  - Work‑and‑Turn: same plate for both sides; print, flip with same gripper edge, print again, then cut to create two copies.
  - Work‑and‑Tumble: same plate for both sides; print, flip with opposite gripper edge, print again, then cut to create two copies.
  - Single‑sided: print only one side (posters, labels).
  - Perfector: press prints both sides in one pass by turning the sheet internally.
- Where WorkStyle appears:
  - `Layout` sheet level: `SourceWorkStyle`.
  - `ConventionalPrintingParams`: `WorkStyle`.
  - `StrippingParams`: `WorkStyle` (when present).
- Side handling by WorkStyle:
  - Perfecting: Front and Back side layouts present; `ConventionalPrintingParams` lists both sides.
    - Perfecting RunList uses `NPage=4` with front/back `LogicalPage` ranges, and back-side `ContentObject` placements are mirrored (`CTM="-1 0 0 -1 ..."`) with `HDM:PageOrientation="180"`.
    - Evidence: `Signa_Samples/PerfectingSample.jdf`.
  - WorkAndBack (sheetwise): Front and Back side layouts present; `ConventionalPrintingParams` lists both sides.
    - Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
    - WorkAndBack back-side placements use positive CTMs with `HDM:PageOrientation="0"` (no mirror), contrasting with Perfecting's mirrored back-side CTMs and `HDM:PageOrientation="180"`.
    - Evidence: `Signa_Samples/SheetwiseSample.jdf`.
  - WorkAndTurn: Only `Side="Front"` appears in `ConventionalPrintingParams` and `Component`; layout has a single side.
    - Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`.
    - WorkAndTurn RunList tends to collapse to a single side with `NPage=2` and no back-side partitions, whereas WorkAndBack uses front/back partitions and `NPage=4` with `LogicalPage` ranges. Evidence: `Signa_Samples/WorkAndTurnSample.jdf`, `Signa_Samples/SheetwiseSample.jdf`.
    - Sample set scan: no `ConventionalPrintingParams` or `Component` partitions with `Side="Back"` appear in the WorkAndTurn exports. (334 files scanned.)
  - WorkAndTumble: Same single-side model as WorkAndTurn, but back-side placements are rotated (`HDM:PageOrientation="180"`) within the single `Side="Front"` layout.
    - Evidence: `Signa_Samples/WorkAndTumbleSample.jdf`.
  - Simplex: Only `Side="Front"` appears in `ConventionalPrintingParams` and `Component`; layout has a single side.
    - Evidence: `Signa_Samples/SingleSheet-SingleSide.jdf`.
- Mixed work styles in one job:
  - `ConventionalPrintingParams` may assign `WorkStyle` per signature part when multiple work styles are present.
  - Evidence: `Signa_Samples/MixedWorkStyles.jdf`.

### ConventionalPrintingParams
- Role: Print process parameters that tie imposition sides to printing.
- Observed attributes: `PrintingType="SheetFed"`, `PartIDKeys="SignatureName SheetName Side"`.
- Structure:
  - A nested `ConventionalPrintingParams` for SignatureName, then SheetName, then Side.
  - `WorkStyle` may be at the top-level or at the signature part when styles vary.
- Evidence:
  - Single work style on top-level: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Mixed work styles per signature: `Signa_Samples/MixedWorkStyles.jdf`.

### StrippingParams (imposition cell layout)
- Role: Defines relative positions of signatures or cells on the press sheet.
- Observed attributes: `PartIDKeys`, `WorkStyle`.
- Structure:
  - May include nested `StrippingParams` parts with `AssemblyIDs`, `BinderySignatureName`, `SectionList`.
  - `Position` elements use `RelativeBox` coordinates.
  - `StripCellParams` can define `TrimSize`.
  - Includes `MediaRef` and `BinderySignatureRef` references.
- Part ID usage:
  - `PartIDKeys="SignatureName SheetName"` when a single bindery signature is implied.
  - `PartIDKeys="SignatureName SheetName BinderySignatureName"` when multiple bindery signatures are present.
  - `WorkStyle` and `SignatureName` can appear on the nested StrippingParams part when multiple styles are combined.
- Evidence:
  - Single bindery signature: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Multiple bindery signatures: `Signa_Samples/GangedPostcards-Perfecting.jdf`.
  - Mixed work styles and per-signature StrippingParams: `Signa_Samples/MixedWorkStyles.jdf`.
- Working theory: Layout provides absolute placement geometry, while StrippingParams provides relative cell placement and signature grouping for ganged or multi-up sheets.

### BinderySignature
- Role: Defines folding signature pattern and page ordering.
- Observed attributes: `BinderySignatureType="Fold"`, `FoldCatalog`, `NumberUp`.
- Structure:
  - `SignatureCell` with `FrontPages`, `BackPages`, plus HDM orientation arrays.
- Evidence:
  - `Signa_Samples/GangedPostcards-Perfecting.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Montage uses `BinderySignatureType="Grid"` with per-block `SignatureCell` orientation values (0/90/180), reflecting explicit page grid placement rather than a fold scheme. Evidence: `Signa_Samples/MontageSample.jdf`.

### Montage vs fold‑mode imposition (Signa distinction)
This comparison is specific to Signa. Some other imposition systems (e.g., Metrix) encode a folding scheme even for unfolded work; reproducing that for Signa montage is low value.
| Signal | Montage (Signa) | Fold‑mode (Signa) |
| --- | --- | --- |
| `BinderySignatureType` | `Grid` | `Fold` |
| `FoldCatalog` | Often `unnamed` or absent | Present when folding is modeled |
| `FoldingParams` | May be absent or `NoOp="true"` | Present with fold metadata |
| `HDM:CIP3FoldSheetIn_*` | Often omitted | Typically present when fold modeled |
| `AssemblyIDs` naming | `Block_*` style identifiers | Fold scheme‑style IDs (e.g., `F02-01...`) |
| Geometry | Explicit CTM/TrimCTM rotations per block | CTM/TrimCTM + fold scheme metadata |
| Process `Types` | Short list (e.g., `Imposition ConventionalPrinting Cutting Trimming`) | Adds folding/finishing steps (e.g., `Folding Gathering SpinePreparation SpineTaping`) |
| Practical impact | Focus on placement and marks PDF linkage; folding semantics low priority | Folding semantics needed for downstream finishing |
| Evidence | `Signa_Samples/MontageSample.jdf`, `Signa_Samples/4x6MontageSample.jdf` | `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`, `Signa_Samples/4x6wFoldSample.jdf` |
| Validator behavior | Skips fold-dimension warnings when `BinderySignatureType="Grid"` | Emits fold-dimension warnings when expected |
- 4x6 comparison: the montage version omits `FoldCatalog`, `FoldingParams`, `HDM:CIP3FoldSheetIn_*`, and closed/opened folding dimensions while keeping `BinderySignatureType="Grid"` and `AssemblyIDs="Block_*"`. The fold‑mode version adds `BinderySignatureType="Fold"`, `FoldCatalog="F2-1"`, `FoldingParams`, `HDM:CIP3FoldSheetIn_*`, and `HDM:Closed/OpenedFoldingSheetDimensions`, and uses `AssemblyIDs` tied to the fold scheme. Evidence: `Signa_Samples/4x6MontageSample.jdf`, `Signa_Samples/4x6wFoldSample.jdf`.

### RunList (page and marks mapping)
- Role: Connects imposition to source PDFs and page mapping.
- Observed patterns:
  - A RunList for document pages (`ProcessUsage="Document"` in ResourceLinkPool).
  - A RunList for marks (`ProcessUsage="Marks"`).
  - A RunList for page pools (`DescriptiveName="PagePool"`, `PartIDKeys="Run"`).
  - A RunList for output (`Usage="Output"`).
  - Stability notes:
    - Document/Marks RunLists are **Stable** for Signa exports and expected in most jobs.
    - PagePool RunList is **Transport-only** (used for Cockpit ↔ Signa PDF handoff).
    - Output RunList is **Advisory** (present but typically a placeholder).
  - `RunList` with `ElementType="Reservation"` can appear as a placeholder for Document or Output (observed in montage sample); treat it as a valid placeholder rather than an error.
  - `HDM:OFW="1.0"` appears on the document RunList in multiple samples.
  - The marks RunList references a PDF that contains printer marks (color bar, side guides, crop marks, etc.). Signa often names the marks PDF `data.pdf` inside the exported `<jobnumber>.jdf` bundle, but Prinect does not require this filename; any valid URL works.
  - Marks PDFs may include multiple pages per sheet to separate mark sets (e.g., crop/side guides vs. color bars and plate marks).
- Sample-wide counts: Document/Marks RunListLinks appear in every file; PagePool RunListLinks are missing in 278/2310 files; Output RunListLinks appear in every file and typically omit `ProcessUsage`.
- PagePool RunLists usually use `PartIDKeys="Run"`, but some builds omit it; treat missing `PartIDKeys` as a warning rather than an error.
- User observation: PagePool appears when PDF pages are assigned in Signa; layouts without assigned PDFs often omit PagePool.
- Cockpit-initiated jobs can still include PagePool with a PDF FileSpec even when pages are not manually assigned in Signa, suggesting PagePool is used to transfer source PDFs between Cockpit and Signa. Evidence: `Signa_Samples/PagePoolTest.jdf`.
- User observation: layouts created in Signa before PDFs are imported into Cockpit can omit PagePool entirely, even when Document/Marks PDFs exist later.
- PagePool is not required for downstream Prinect acceptance; third‑party imposition JDFs may not include PagePool at all. Treat it as a Signa/Cockpit transfer artifact unless Signa is in the workflow. (Stability: Transport-only)
  - Document RunList `PartIDKeys` is either omitted or `Run` (1547/2310 omitted, 763/2310 `Run` in this sample set).
  - Marks RunList `PartIDKeys` is consistently `SignatureName SheetName Side` in Signa exports.
  - Marks RunLists include `SeparationSpec` lists in most files (2260/2310), but a small subset omit them; treat missing separation specs as non-fatal. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2080 GPS_Q2080-data.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1513 INAC_Q1513-data.jdf`.
  - Montage example: the Document RunList can be a reservation placeholder (`ElementType="Reservation"`, `Status="Unavailable"`) while the Marks RunList carries the actual marks PDF and separations. Evidence: `Signa_Samples/MontageSample.jdf`.
  - Typical structure (simplified):
  ```
  RunList (Document) -> data.pdf
    RunList SignatureName/SheetName/Side/LogicalPage (SeparationSpec list)
  RunList (Marks) -> marks.pdf
  RunList (PagePool) -> content pool PDFs
  RunList (Output) -> output placeholder

  RunListLink ProcessUsage="Document" Usage="Input" -> RunList
  RunListLink ProcessUsage="Marks" Usage="Input" -> RunList
  RunListLink ProcessUsage="PagePool" Usage="Input" -> RunList (optional)
  RunListLink Usage="Output" -> RunList (often no ProcessUsage)
  ```
- Spec note:
  - JDF uses zero-based indexing for lists and ranges; negative indices count from the end (JDF 1.7 Section 1.7.2). This aligns with observed `LogicalPage` and `Pages` usage in Signa output.
- Typical structure in the document RunList:
  - `LayoutElement` with `FileSpec` to `data.pdf`.
  - Nested RunList parts by SignatureName, SheetName, Side, and `LogicalPage`.
  - `SeparationSpec` entries with HDM attributes (`HDM:IsMapRel`, `HDM:Type`, `HDM:SubType`).
- Evidence:
  - Document RunList with separation mapping: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Work-and-turn document RunList with single side (observed across all work-and-turn samples; 334 files scanned): `Signa_Samples/SingleSheet-WorkandTurn.jdf`.
  - Reservation placeholder: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - `HDM:OFW`: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SingleSheet-WorkandTurn.jdf`.

### Media
- Role: Defines paper and plate media for the sheet.
- Observed attributes: `MediaType="Paper"` or `MediaType="Plate"`, `Dimension`, `PartIDKeys="SignatureName SheetName"`.
- Stability: **Stable** for `MediaType` + `Dimension`; descriptive paper metadata is **Advisory**.
- Signa-specific attributes:
  - `HDM:LeadingEdge` appears on plate media.
  - `HDM:CalPapSort` and `HDM:CalPapSortBack` appear under paper media.
  - When a specific paper is selected in Signa, `Media` includes additional descriptive fields such as `Brand`, `Weight`, `Thickness`, `ProductID`, and lab color values. Evidence: `Signa_Samples/MontageSample.jdf`.
- Multi-signature packaging:
  - A single Media resource can include multiple nested `Media` parts with different dimensions per signature.
- Evidence:
  - Paper + plate pairing: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - `HDM:LeadingEdge`: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Multiple nested Media parts: `Signa_Samples/MixedWorkStyles.jdf`.

### Component
- Role: Represents outputs of processes (Sheet, Block, FinalProduct).
- Observed attributes:
  - `ComponentType` values observed: `PartialProduct Sheet`, `PartialProduct Block`, `FinalProduct`.
  - `PartIDKeys` align to nested `Component` parts (SignatureName, SheetName, Side, BlockName).
- Perfect-bound cover/body split: Signa emits nested Component entries per signature, with `ProductType="Cover"` + `HDM:IsCover="true"` for the cover signature and `ProductType="Body"` for text signatures. Evidence: `Signa_Samples/16PagePerfectBoundSampleCover.jdf`.
- Observed structure (from Signa output): a top-level `Component` with `ComponentType="PartialProduct Block"` contains per-signature `Component` entries with `AssemblyIDs`, fold dimensions, `SignatureName`, and `ProductType` (Cover/Body); those nest `Component` entries keyed by `SheetName`, which in turn nest `Component` entries keyed by `BlockName`.
- Verification note: cover/body tagging in Component blocks is observed in Signa exports, but downstream behavior is currently unverified in this environment (no Bindery/finishing licenses available). (Stability: Advisory)
- Evidence:
  - Sheet and block outputs: `Signa_Samples/SingleSheet-SingleSide.jdf`.

### CuttingParams / FoldingParams / Assembly (imposition-adjacent)
- Role: Finishing-related resources that reference imposition outputs.
- Observed patterns:
  - `CuttingParams` with `CutBlock` and `HDM:CIP3BlockTrf`.
- `FoldingParams` with `FoldCatalog`, `HDM:CIP3FoldSheetIn_*`, and `NoOp="true"` when folding is not applied.
- `FoldCatalog` is the explicit folding scheme name when Signa models folding; it is typically present when folding is defined, but some FoldingParams appear without it.
- Fold schemes may be implied by naming (e.g., `AssemblyIDs`/`BlockName` prefixes like `F01-01`) even when folding is not explicitly modeled; in these cases fold dimensions may be omitted even if the piece will be folded downstream.
  - Fold scheme page rotations can explain “non‑mirrored” back layouts in perfecting; some schemes (e.g., head‑in) rotate pages 180 degrees on both sides, so back-side rotations need not look mirrored. Reference rotations: `Signa_Samples/JDFFoldingSchemes/Folds.json`.
  - `Assembly` with `AssemblySection` carrying HDM fields like `HDM:BlockName`, `HDM:SheetName`, `HDM:SignatureName`.
  - `HDM:CombiningParams` is present in many samples, pairing multiple cut blocks into a combined block before folding.
- Stability: **Advisory** (finish-chain context); use for interpretation but not required for Cockpit import.
- Evidence:
  - `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Implied fold scheme naming: `Signa_Samples/SingleSheet-SingleSide.jdf`.

### TransferCurvePool
- Role: Provides transfer curves for paper and plate; linked from Layout via `TransferCurvePoolRef`.
- Structure:
  - `TransferCurvePool` parts by SignatureName and SheetName.
  - `TransferCurveSet` entries typically named `Paper` and `Plate`.
- Normalization note: when a job mixes sheet sizes by signature, a per‑signature `TransferCurvePool` (PartIDKeys=`SignatureName`) referenced at the signature‑level `Layout` can prevent Layout Preview anchoring to the first sheet size.
- Stability: **Advisory** (Layout Preview alignment) unless a workflow explicitly consumes curves.
- Evidence:
  - `Signa_Samples/SingleSheet-Sheetwise.jdf`.

## ResourceLinkPool and process indexing
- `ResourceLinkPool` links resources to process steps using `CombinedProcessIndex`.
- `CombinedProcessIndex` values map to the order of `Types` in the JDF root.
- RunList links also include `ProcessUsage` to distinguish Document, Marks, and PagePool.
- Stability: **Stable** for link integrity; `CombinedProcessIndex` must align with `Types`.
- Evidence:
  - `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.

## Behavior by product type (selected samples)
- Selection method: job descriptions in `Signa_Samples/Job_JDFs_with_descriptions/Q.csv` were used to choose example JDFs; descriptions are not authoritative for the JDF content.
- Perfect bound text includes extra finishing steps in `Types` (`Gathering`, `SpinePreparation`, `SpineTaping`); matching cover files stop at `Trimming` and are Simplex layouts. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1323 HealthCare Can_P1701 Text-data.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1323 HealthCare Can_P1701 Cover-data.jdf`.
- Perfect-bound text-only layouts (no cover part) still emit the full binding chain in `Types` and include `SpinePreparationParams`/`SpineTapingParams`, while `FoldingParams` carries `HDM:BindingRule="PerfectBound"`. Evidence: `Signa_Samples/16PagePerfectBoundSample.jdf`.
- Adding a cover on a separate press sheet introduces a second signature/sheet plus `CoverApplication` in `Types`, and emits `CoverApplicationParams`. Cover blocks are tagged with `HDM:IsCover="true"` and `ProductType="Cover"`, while text blocks remain `ProductType="Body"`. Evidence: `Signa_Samples/16PagePerfectBoundSampleCover.jdf`.
- User‑named cover/text sheets propagate into `SheetName`, `DescriptiveName`, and downstream block identifiers (`BlockName`, `HDM:OutputBlockName`, `FoldingParams` partitions), replacing default `FB 001`/`FB 002` naming. Evidence: `Signa_Samples/16PagePerfectBoundSampleCoverNamed.jdf`.
- Cover option “Single Page for Folding Sheet” emits cover spreads as single large pages (`TrimSize="792 1233"` in this sample) and adds placeholder ContentObject entries marked with `HDM:HiddenBy` and `DescriptiveName` like `--1`/`--19`. Evidence: `Signa_Samples/16PagePerfectBoundSampleSinglePageCover.jdf`.
- Enabling “Cover Bonding On the First Folding Sheet” did not introduce new JDF elements or attributes beyond the existing cover + cover‑application structure; no structural delta observed compared to the single‑page cover sample. Evidence: `Signa_Samples/16PagePerfectBoundSampleCoverBonding.jdf`.
- Cover glue line settings (“Glue Line: By Moving” + “Outermost Pages Only”) do not emit explicit glue-line attributes; the only observable changes are small `ContentObject` `ClipBox` boundary shifts (roughly 9 pt / 0.125 in) while `FinalPageBox` remains unchanged. Interpretation (user observation): Signa clips the art near the first/last pages so the glue line stays ink‑free where the cover adheres to the text block. Evidence: `Signa_Samples/16PagePerfectBoundSampleCoverGlueline.jdf`.
- Cover glue line settings (“By Scaling”, width 0.125 in) adjust `ContentObject` `CTM`/`TrimCTM` with a scale factor (e.g., `0.985294`) and shrink the corresponding `ClipBox` extents while keeping `FinalPageBox` unchanged; changes appear only on affected outermost pages. Evidence: `Signa_Samples/16PagePerfectBoundSampleCoverGluelineScaling.jdf`.
- Cover glue line settings (“By Clipping Only”, width 0.125 in) alter only the `ClipBox` boundaries on affected outermost pages; `CTM`/`TrimCTM` remain at unit scale and `FinalPageBox` remains unchanged. Evidence: `Signa_Samples/16PagePerfectBoundSampleCoverGluelineClipping.jdf`.
- Cover glue line settings (“By Moving”, width 0.125 in) keep unit scale but shift `CTM`/`TrimCTM` translations and adjust `ClipBox` boundaries for the affected outermost pages; `FinalPageBox` remains unchanged. Evidence: `Signa_Samples/16PagePerfectBoundSampleCoverGluelineMoving2.jdf`.
- Adding an extra 8‑page folding sheet introduces a new signature/sheet (`Sig003` / `Sig 2`) with its own `BinderySignature`/`FoldingParams` (F8‑7), `CuttingParams` blocks, `StrippingParams` section, and `AssemblySection`. RunList `NPage` increases to 28 and the gathered thickness increases accordingly. Evidence: `Signa_Samples/16PagePerfectBoundSamplePlus8Page.jdf`.
- Adding a 4‑page folding sheet adds another signature/sheet (`Sig004` / `Sig 3`) with its own `BinderySignature`/`FoldingParams` (F4‑1), four cut blocks (`Sig_3_B_4_1`‑`Sig_3_B_4_4`), `StrippingParams` section, `RunList` partition, and `AssemblySection`. RunList `NPage` increases to 32 and the gathered thickness increases accordingly. Evidence: `Signa_Samples/16PagePerfectBoundSamplePlus8PageAnd4.jdf`.
- Ganging the 8‑page and 4‑page folding sheets onto one press sheet collapses them into a single signature/sheet (`Sig003` / `Sig 2 & 3`) with multiple `AssemblyIDs` on the same `Layout` and `StrippingParams`. The separate `Sig004` partition disappears; `StrippingParams` nests per‑assembly partitions (with `BinderySignatureName`) under the ganged sheet, and `Component`/`AssemblySection` entries list both block sets under the shared sheet name. Sheet names are user‑assigned in this sample. Evidence: `Signa_Samples/16PagePerfectBoundSamplePlus8PageAnd4Ganged.jdf`.
- Enabling collation marks did not change the JDF structure or mark metadata (same `MarkObject`/`RegisterMark` counts and values) relative to the ganged baseline; any collation‑mark output appears to live in the marks PDF and/or Signa SDF blob instead of the JDF. Evidence: `Signa_Samples/16PagePerfectBoundSamplePlus8PageAnd4GangedCollation.jdf`.
- Multiple product parts are declared only via `HDM:SignaJobPart` (e.g., `Book 1`, `Book 2`) with no `Product`/`ProductPart` elements; the per‑part association is carried on each `ContentObject` via `HDM:JobPart` plus `HDM:RunlistIndex`. Evidence: `Signa_Samples/16PagePerfectBoundSample2Books.jdf`.
- A ganged sheet can mix product parts by splitting `AssemblyIDs` on the same sheet: the shared sheet (`Book 1 8 Page - Book 2 4 Page`, `Sig003`) carries two assemblies (`F08-07_li_2x2_0_3` for Book 1 and `F04-01_ui_2x1_1_3` for Book 2), with `StrippingParams` nesting both assemblies under the same sheet/SignatureName and separate `BinderySignatureName` entries. Evidence: `Signa_Samples/16PagePerfectBoundSample2Books.jdf`.
- `HDM:StitchJobName` encodes the product‑part index (`B_0` for Book 1, `B_1` for Book 2) on each `AssemblySection`, but downstream finishing outputs (`Gathering`, `CoverApplication`, `Trimming`) are emitted as a single combined block with total thickness (52 pages) rather than per‑part outputs. Evidence: `Signa_Samples/16PagePerfectBoundSample2Books.jdf`.
- Compared to the single‑book ganged sample, the two‑book file increases signature partitions from 3 to 5 (`Sig001`‑`Sig005`) and `AssemblySection` count from 4 to 6; the new sections map to Book 2’s cover and text signature while the ganged sheet remains a single `SignatureName` with two assemblies. Evidence: `Signa_Samples/16PagePerfectBoundSample2Books.jdf`.
- The top‑level marks `RunList` increases from `NPage="12"` to `NPage="20"` while the document `RunList` increases from `NPage="32"` to `NPage="52"`, reflecting two separate page stacks merged into one job. Evidence: `Signa_Samples/16PagePerfectBoundSample2Books.jdf`.
- Marks `RunList` pages are grouped contiguously by signature/sheet (`Sig001`/Book 1 Cover → `Sig005`/Book 2 Sig 1) with a single `Pages="0 ~ 19"` block; the document `RunList` remains a reservation‑only node (no page partitions or sheet association). Evidence: `Signa_Samples/16PagePerfectBoundSample2Books.jdf`.
- When Book 2 is switched to saddle‑stitch, the root `Types` list adds `Collecting` and `Stitching`, and new `CollectingParams` + `StitchingParams` resources appear (e.g., `NumberOfStitches="2"`, `StapleShape="Crown"`). Evidence: `Signa_Samples/16PagePerfectBoundSample2BooksSS.jdf`.
- Mixed bindings in one job are expressed in `FoldingParams` via `HDM:BindingRule`, with Book 1 assemblies staying `PerfectBound` and Book 2 assemblies switching to `Saddlestitch`, while `ConventionalPrintingParams` `WorkStyle` stays `Perfecting` across the job. Evidence: `Signa_Samples/16PagePerfectBoundSample2BooksSS.jdf`.
- Finishing outputs are still combined at the job level: `SpinePreparationParams`, `SpineTapingParams`, and `CoverApplicationParams` remain present for the perfect‑bound part, and a single `Stitching` output component is added for the saddle‑stitch part (not split by product part). Evidence: `Signa_Samples/16PagePerfectBoundSample2BooksSS.jdf`.
- A single‑product saddle‑stitched job omits perfect‑bind resources (`SpinePreparationParams`, `SpineTapingParams`, `CoverApplicationParams`) and uses `Collecting` + `Stitching` only; `WorkStyle` remains `Perfecting` and `FoldingParams` `HDM:BindingRule` is `Saddlestitch` for all signatures. Evidence: `Signa_Samples/Saddle28P.jdf`.
- With creeping turned off, no explicit creep or spine‑adjustment attributes are emitted in the JDF; creep behavior appears implicit to Signa rather than encoded in the ticket. Evidence: `Signa_Samples/Saddle28P.jdf`.
- Declaring the 4‑page section as a cover in a saddle‑stitched job marks the F4 signature as cover: `HDM:IsCover="true"` appears on the `FoldingParams` partition and related `Component` outputs, and those blocks switch to `ProductType="Cover"` while other signatures remain `ProductType="Body"`. The overall process chain remains `Collecting` + `Stitching` with no perfect‑bind resources. Evidence: `Signa_Samples/Saddle28P+cover.jdf`.
- Switching the saddle‑stitch cover to sheetwise sets `SourceWorkStyle="WorkAndBack"` on the cover sheet only and adds a `ConventionalPrintingParams` partition with `WorkStyle="WorkAndBack"` for `Sig001`; other signatures remain `Perfecting`. Binding stays `Saddlestitch` and the cover remains flagged with `HDM:IsCover="true"`. Evidence: `Signa_Samples/Saddle28PSWCover.jdf`.
- Switching the saddle‑stitch cover to work‑and‑turn sets `SourceWorkStyle="WorkAndTurn"` on the cover sheet and assigns `WorkStyle="WorkAndTurn"` to the `Sig001` `ConventionalPrintingParams` partition; the cover sheet no longer partitions by `Side="Back"` in printing params, and the marks `RunList` drops to `NPage="10"` (front-only for the cover) while other signatures remain perfecting. Evidence: `Signa_Samples/Saddle28PWATCover.jdf`.
- With a work‑and‑turn cover and a sheetwise 8‑page signature, `WorkStyle` partitions per signature: `Sig001` is `WorkAndTurn`, `Sig002` remains `Perfecting`, and `Sig003` becomes `WorkAndBack`. `SourceWorkStyle` matches per sheet (`FB 001` WAT, `FB 003` WAB), and the marks `RunList` still uses front‑only pages for the cover while keeping front/back pages for other signatures. Evidence: `Signa_Samples/Saddle28PWATCoverSW8.jdf`.
- Gapping the imposition (creep still off) shifts `ContentObject` geometry and mark placement (CTM/ClipBox/FinalPageBox/`RegisterMark` centers) but does not introduce any explicit creep attributes; structure and work styles remain unchanged. Evidence: `Signa_Samples/Saddle28PWATCoverSW8NoCreep.jdf`.
- Enabling “Creep by Offset” still does not emit any explicit creep attributes; the only differences are geometric shifts in `ContentObject` boxes/CTMs and mark positions, plus resulting block sizes in `CuttingParams`/`FoldingParams`. Evidence: `Signa_Samples/Saddle28PWATCoverSW8CreepOffset.jdf`.
- Enabling “Creep by Scaling” likewise emits no creep attributes; instead `ContentObject` `CTM`/`TrimCTM` gain scale factors (e.g., `0.998765`, `0.997941`) and corresponding `FinalPageBox` shifts, while marks shift slightly to match the adjusted geometry. In this sample the work‑and‑turn cover (`Sig001`) stays unscaled; scale factors appear on the perfecting/work‑and‑back signatures (`Sig002`/`Sig003`). Evidence: `Signa_Samples/Saddle28PWATCoverSW8CreepScaling.jdf`.
- When a montage is introduced as a separate product part, Signa adds a new signature (`Sig004`) whose `BinderySignatureType="Grid"` and `FoldCatalog="unnamed"` with `FoldingParams NoOp="true"`. The montage part uses its own `HDM:JobPart` tag (`Montage`) and a separate `RunlistIndex` range, while the marks `RunList` appends a new front/back range for the montage sheet. Evidence: `Signa_Samples/Saddle28PWATCoverSW8AndMontage.jdf`.
- Saddle-stitched/self-cover jobs include `Collecting` and `Stitching` in `Types`, and may include multiple `HDM:SignaJobPart` entries for inserts/text in one file. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1137 CPS_Q1137-data.jdf`.
- Varnish work shows up as additional PagePool RunList PDFs (e.g., filenames containing "varnish") while `Types` still omits a varnish/coating process; working theory is varnish is treated as a content layer file in PagePool. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1038 Xerox_Q1038 Varnish-data.jdf`.
- Tabs layouts use `WorkStyle="WorkAndBack"` and a PagePool that contains separate cover/tabs/text PDFs; `SeparationSpec` includes `HDM_DarkColor` alongside CMYK, likely to target the darkest separation for marks. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1124 GGLS - BRP SEA CANeng_Q1124 tabs-data.jdf`.
- Map/large-flat jobs can be Simplex with `FoldingParams` marked `NoOp="true"` even when the job description references folding; working theory is folding is handled outside Signa for these cases. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1039 Aviation_Q1039 map-data.jdf`.

## Complexity-driven structural shifts
- Multi-signature jobs: `Layout` gains multiple signature branches (`SignatureName`), while `StrippingParams`/`Media`/`BinderySignature` use `PartIDKeys="SignatureName SheetName BinderySignatureName"` with per-signature parts. Evidence: `Signa_Samples/MultiSig-Perfecting.jdf`, `Signa_Samples/MixedWorkStyles.jdf`.
- Ganged or multi-up sheets: `StrippingParams` nests multiple `BinderySignatureName` parts with `Position/RelativeBox`, and `HDM:CombiningParams` plus `Assembly` enumerate multiple `BlockName`/`AssemblyIDs` at the block level. Evidence: `Signa_Samples/GangedPostcards-Perfecting.jdf`, `Signa_Samples/MixedWorkStyles.jdf`.
- Work-and-turn/back layouts: `Layout` may only define `Side="Front"`, but `ContentObject` uses `HDM:AssemblyFB="Front|Back"` to tag product faces; printing params follow the work style. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`.
- Multi-job-part/inserts: job-level `HDM:SignaJobPart` appears, `HDM:JobPart` tags `ContentObject`, and the PagePool aggregates multiple source PDFs (text/insert/cover). Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1137 CPS_Q1137-data.jdf`.
- Multiple Signa product parts can duplicate the full layout/signature tree: each part gets its own `SignatureName`/`SheetName`, while `HDM:JobPart` and `HDM:RunlistIndex` tag `ContentObject` placements. Marks RunList expands to include logical page ranges per part (e.g., `NPage=8` with Front/Back ranges per part). Evidence: `Signa_Samples/2ProductPartsSample.jdf`.
- Product parts are independent product intents: each part can define its own product type (e.g., perfect bound vs saddle‑stitch) and uses only its own pages for spine/creep calculations and finishing decisions. Source: user observation.
- Longer finishing chains: extra `Types` steps (e.g., `Collecting`, `Stitching`, `SpinePreparation`) add `Component` outputs and finishing params partitioned by `AssemblyIDs`/`BlockName`. Evidence: `Signa_Samples/SS-BookText-Perfecting.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1323 HealthCare Can_P1701 Text-data.jdf`.
- RunList depth: document RunLists add nested parts by `SignatureName`/`SheetName`/`Side`/`LogicalPage`, while PagePool stays `PartIDKeys="Run"` but grows across multiple PDFs. Evidence: `Signa_Samples/MultiSig-Perfecting.jdf`, `Signa_Samples/PB-BookCover2Versions-Ganged-Sheetwise.jdf`.

## Attribute hoisting (partitioning behavior)
- Signa often hoists attributes to the highest partition where the value stays true, relying on partition inheritance to avoid repeating the same attribute on every leaf part.
- When values vary by signature/sheet, Signa pushes attributes down to nested parts (e.g., `WorkStyle` on `ConventionalPrintingParams` or `StrippingParams` parts); when uniform, the higher-level partition carries the value and leaf parts omit it. Evidence: `Signa_Samples/MixedWorkStyles.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- Mixed work styles within one job are expressed as per-signature partitions rather than a single root `WorkStyle`; the cover can be `WorkAndBack` while text signatures remain `Perfecting`. Evidence: `Signa_Samples/Saddle28PSWCover.jdf`.
- Binding rules are not hoisted when they vary; `HDM:BindingRule` stays on `FoldingParams` partitions so different signatures (or product parts) can be `PerfectBound` vs `Saddlestitch`. Evidence: `Signa_Samples/16PagePerfectBoundSample2BooksSS.jdf`.
- Media attributes like `Dimension`, `Thickness`, and `Weight` tend to live on the root `Media` resource, with nested parts only identifying `SignatureName`/`SheetName`. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- Example (uniform `WorkStyle` hoisted):
  ```xml
  <ConventionalPrintingParams WorkStyle="WorkAndBack">
    <ConventionalPrintingParams SignatureName="Sig001">
      <ConventionalPrintingParams SheetName="FB 001"/>
    </ConventionalPrintingParams>
  </ConventionalPrintingParams>
  ```
- Example (per-signature `WorkStyle`):
  ```xml
  <ConventionalPrintingParams>
    <ConventionalPrintingParams SignatureName="Sig001" WorkStyle="Perfecting"/>
    <ConventionalPrintingParams SignatureName="Sig002" WorkStyle="WorkAndBack"/>
  </ConventionalPrintingParams>
  ```
- Non-hoisted redundancy: document RunList repeats the same `FileSpec` (and often identical `SeparationSpec` lists) at leaf parts even when all leaves share the same file; this is redundant but still compliant, since JDF allows attributes/elements to be repeated at lower partitions. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- Implication for consumers: resolve attribute values by walking up the partition tree; absence at a leaf does not imply "unknown".

## Spec-only reader pitfalls
- Heidelberg/Signa extensions (`HDM:*`) are pervasive and undocumented in the core spec; assume vendor extensions until proven otherwise. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.
- Marks are separate PDFs linked with `ProcessUsage="Marks"` and can have more pages than sheet sides; do not expect PagePool/Document counts to align with marks PDF pages. Evidence: `Signa_Samples/SingleSheet-SingleSide.pdf`, `Signa_Samples/SingleSheet-SingleSide.jdf`.
- RunList partitions repeat `FileSpec`/`SeparationSpec` at leaf parts even when identical; this is compliant but may look redundant if you expect maximum hoisting. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- PagePool RunListLink is build-dependent: older Signa 21.00 builds (e.g., 22103.35, 21142.24) often omit `ProcessUsage="PagePool"` entirely while still linking Document/Marks/Output; newer 21.10 builds emit it consistently. Treat missing PagePool links as a warning unless a PagePool RunList exists. Evidence: `Signa_Samples/SS-Book-SelfCover-Perfecting.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2423 ASCM - CPIM Book2_Q2423 book 2 text-data.jdf`.
- Placeholder separations (`B/C/M/Y`, `X/Z/U/V/S1...S8`, `HDM_DarkColor`) appear instead of explicit ink names; treat these as remappable slots in Signa/Prinect workflows. Evidence: `Signa_Samples/SingleSheet-SingleSide.pdf`, `Signa_Samples/SS-SingleSheet-Perfecting.pdf`.
- `Version="1.3"` with higher `MaxVersion` and inconsistent `ICSVersions` presence can look contradictory but reflects Signa export defaults. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
- WorkStyle and SourceWorkStyle appear on multiple resources (Layout, ConventionalPrintingParams, StrippingParams) with inheritance/partitioning; do not assume a single source of truth without resolving partitions. Evidence: `Signa_Samples/MixedWorkStyles.jdf`.
- Folding can be encoded with `NoOp="true"` while fold metadata remains populated; indicates no folding applied in Signa, not an invalid fold definition. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1039 Aviation_Q1039 map-data.jdf`.
- `AssemblyIDs` and `HDM:AssemblyFB` are not always present on `ContentObject` (notably Simplex or older builds), yet are used on other resources; avoid treating their absence as invalid. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2137 Barrhaven_Optometric_Q2137-data.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1250 Cibles Can_Q1250 blk-data.jdf`.
- `FoldCatalog="unnamed"` typically indicates montage/custom layouts where folding is not modeled; folding hints can exist but are not deterministic. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2206 GGLS - SHRM_Q2206 Org text-data.jdf`.

## Consumer guide (implementation focus)
- **Minimum skeleton**: JDF root `Type="ProcessGroup"` with `Types` including `Imposition`, `ResourcePool/Layout` partitioned by `SignatureName SheetName Side`, `RunList` links for `Document`, `Marks`, and `PagePool`, and `ResourceLinkPool` with `CombinedProcessIndex`.
- **WorkStyle mapping**: treat `Layout/@SourceWorkStyle`, `ConventionalPrintingParams/@WorkStyle`, and `StrippingParams/@WorkStyle` as a resolved value via partition inheritance; always walk up the partition tree.
- **Marks vs content**: `ProcessUsage="Marks"` points to separate marks PDFs whose page counts may exceed sheet sides; `Document` and `PagePool` describe source pages, not marks.
- **Prinect acceptance checklist (minimal)**:
  - `Layout` with `PartIDKeys="SignatureName SheetName Side"` and Front/Back (or Front only) side layouts.
  - `RunListLink` entries for `ProcessUsage="Document"` and `ProcessUsage="Marks"` that point to `RunList` resources with `FileSpec URL` values.
  - `ResourceLinkPool` with `CombinedProcessIndex` values consistent with the job `Types` chain.
  - `ConventionalPrintingParams` partitions matching the `Layout` `SignatureName/SheetName/Side` keys.
  - PagePool is optional unless Signa is part of the workflow; marks PDF URL is required for Prinect output.
- **Separations**: interpret `B/C/M/Y` and `X/Z/U/V/S1...S8` as remappable slots; `HDM_DarkColor` is a mark-targeting placeholder, not a named ink.
- **Finishing chains**: as `Types` grows, expect `Component` and finishing params to partition by `AssemblyIDs`/`BlockName`; do not assume a single block in `CuttingParams` or `FoldingParams`.
- **Validation strategy**: verify box geometry (`SurfaceContentsBox`, `HDM:PaperRect`, PDF boxes) and RunList/ResourceLink alignment first, then treat HDM fields as advisory unless confidence is High.
- Example: minimal imposition skeleton (illustrative only; names/IDs omitted for brevity).
  ```xml
  <JDF Type="ProcessGroup" Types="Imposition ConventionalPrinting Cutting Folding Trimming">
    <ResourcePool>
      <Layout PartIDKeys="SignatureName SheetName Side">
        <Layout SignatureName="Sig001">
          <Layout SheetName="Sheet1" SourceWorkStyle="Perfecting">
            <Layout Side="Front">
              <MarkObject/>
              <ContentObject/>
            </Layout>
            <Layout Side="Back">
              <MarkObject/>
              <ContentObject/>
            </Layout>
          </Layout>
        </Layout>
      </Layout>
      <RunList ID="r_doc" PartIDKeys="SignatureName SheetName Side">
        <LayoutElement><FileSpec URL="data.pdf"/></LayoutElement>
      </RunList>
      <RunList ID="r_marks">
        <LayoutElement><FileSpec URL="marks.pdf"/></LayoutElement>
      </RunList>
      <RunList ID="r_pagepool" PartIDKeys="Run" DescriptiveName="PagePool"/>
      <ConventionalPrintingParams PartIDKeys="SignatureName SheetName Side" WorkStyle="Perfecting"/>
    </ResourcePool>
    <ResourceLinkPool>
      <LayoutLink CombinedProcessIndex="0 1" Usage="Input"/>
      <RunListLink CombinedProcessIndex="0" ProcessUsage="Document" Usage="Input" rRef="r_doc"/>
      <RunListLink CombinedProcessIndex="0" ProcessUsage="Marks" Usage="Input" rRef="r_marks"/>
      <RunListLink CombinedProcessIndex="0" ProcessUsage="PagePool" Usage="Input" rRef="r_pagepool"/>
    </ResourceLinkPool>
  </JDF>
  ```

## Minimum Prinect acceptance (Metrix sample)
Metrix JDFs configured for Prinect can be accepted even when many Signa‑specific details are missing. This is not a Metrix spec, but it shows what Prinect tolerates at a basic level.
- Required: `Layout` with `Signature/Sheet/Surface/ContentObject` geometry, `RunListLink` for `Document` and `Marks`, and a valid marks PDF URL.
- Optional (Prinect still accepts): HDM mark geometry (`MarkObject` details), Signa metadata (`HDM:Signa*`), and full workstyle fidelity.
- Evidence: `MetrixSample/Metrix_Impo.jdf`.

## Naming and ID patterns
- IDs are deterministic-looking:
  - `n_...` for the root JDF node.
  - `a_...` for AuditPool entries.
  - `c_...` for Comment entries.
  - `r_...` for Resource IDs.
- `Name`, `DescriptiveName`, and `HDM:OrigNameBySigna` often differ; keep all for traceability.
- Evidence:
  - `Signa_Samples/SingleSheet-SingleSide.jdf`.

## Spec-backed clarifications (JDF 1.7)
- Units are fixed to JDF default units; implementations SHALL use the defaults and SHALL NOT use alternate units (JDF 1.7 Section 1.7.1).
  - Length defaults to points (1/72 inch); microscopic lengths use microns where explicitly stated (Table A.106 Units in Appendix A.5.25; see Media/@Thickness note).
  - Speed is specified as the default unit per hour (JDF 1.7 Section 1.7.1).
- Coordinate systems use a lower-left origin, with X increasing to the right and Y increasing upward (JDF 1.7 Section 2.6.1).
  - Each resource has its own coordinate system; mappings between resource and process coordinate systems are explicit via transformation matrices.
  - No implicit rotations or transforms are assumed; rotations or transformations SHALL be specified explicitly (e.g., ResourceLink/@Transformation or @Orientation) (JDF 1.7 Section 2.6.1 and 2.6.1.1).
- Partitioning and `PartIDKeys` rules:
  - `PartIDKeys` order defines the partition tree from root to leaf; each key SHALL appear exactly once (JDF 1.7 Section 3.10.5.3).
  - Keys may only be omitted from the end of the list in incomplete partitions.
  - Partition key values are unique among sibling parts.
  - Exactly one partition key is specified per leaf or node (excluding the root).

## Schema comparison findings (1.3 vs 1.7)
- Most Signa samples show a single 1.3‑only error: `MaxVersion="1.7"` is invalid in the 1.3 schema. This indicates Signa emits 1.7‑era metadata even when the root `Version` is `1.3`.
- `AssemblyIDs` is rejected by both 1.3 and 1.7 base schemas, reinforcing that it is a Signa/HDM dialect attribute rather than a versioned core attribute.
- Naming constraints (e.g., parentheses in `BinderySignatureName`/`BlockName`) fail in both schemas; these are naming‑rule violations, not version differences. Evidence: `Signa_Samples/MixedWorkStyles.jdf`.
- Run: `dotnet run --project src/Signa.Jdf.Cli.Validate -- --schema-compare <path-to-jdf>` to see counts and the top 10 unique/shared schema messages.

### Generator quick start (Signa-like structure)
- Example: `dotnet run --project src/Signa.Jdf.Cli.Generate -- --output /tmp/signa-minimal.jdf --job-id Job001 --job-part-id Part001 --desc unnamed --work-style Perfecting --signature Sig001 --sheet "FB 001" --sides front,back --document-pdf data.pdf --marks-pdf data.pdf`
- Omit Signa-specific elements with `--no-signa`, PagePool with `--no-pagepool`, or the Output RunList with `--no-output-runlist`.

### Minimal JDF emitter contract (Cockpit importability)
Use this as the “must have” checklist for generating a JDF that Cockpit will accept and impose.
- **JDF root:** `Type="Combined"` and `Types` must include `Imposition` + `ConventionalPrinting`. (Stability: Stable)
- **Layout:** `ResourcePool/Layout` with `SignatureName`, `SheetName`, `Side`, `PartIDKeys="SignatureName SheetName Side"`, `SurfaceContentsBox`, and `HDM:PaperRect` (paper offset).
- **Media:** paper + plate `Media` resources with `Dimension` and `PartIDKeys="SignatureName SheetName"`. Both must be linked via `MediaLink` and referenced via `MediaRef` from the layout.
- **Marks RunList:** required for import/imposition; `FileSpec/@URL` must resolve to a valid PDF and its `TrimBox` must align to the sheet placement. `NPage`/`Pages` must match the marks PDF page count (2 total is tolerated). (Stability: Stable)
- **MarkObject geometry:** include `MarkObject/@CTM` and `@ClipBox`; missing geometry causes imposed PDF failure. (Stability: Stable)
- **ContentObject placement:** required for a page list; `Ord` and `DescriptiveName` drive Cockpit’s page list labels. (Stability: Stable)
- **TransferCurvePool:** optional for import, but required for consistent Layout Preview alignment. (Stability: Advisory)
- **Document RunList / PagePool:** optional for import; Cockpit accepts layouts with only marks + ContentObject. (Stability: Advisory)
- Note: This project does not attempt to generate bindery-executable JDF (folding machines, cutters, stitchers). Bindery components require licensed Prinect modules and physical device integration, which are not available in this environment.

### Minimal JDF import experiments (Cockpit)
#### JobPart/Product split rules (summary)
- `HDM:SignaJob` + `HDM:SignaJobPart` + `HDM:JobPart` on `ContentObject` together drive Cockpit product/page list splitting.
- `HDM:JobPart` is required for any page list to appear; without it Cockpit collapses to “End Product” and often no page list.
- A single declared job part (only `A` or only `B`) still collapses to “End Product”; Cockpit only creates named products when at least two job parts are declared.
- Untagged signatures/sheets default to Product A when A/B are declared.
- Undeclared job parts create an “End Product” bucket and can desync page lists (missing positions).

#### JobPart/Product split table (selected cases)
| JDF | SignaJobPart list | ContentObject JobPart mapping | Cockpit products | Page list behavior |
| --- | --- | --- | --- | --- |
| `minimal-0058.jdf` | A,B | Sig001→A, Sig002→B | A + B | Two page lists, positions split by product |
| `minimal-0061.jdf` | (none) | Sig001→A, Sig002→B | End Product | Single page list |
| `minimal-0065.jdf` | A,B | Sig001→A, Sig002→B | A + B | Two page lists, no warnings |
| `minimal-0066.jdf` | (missing `SignaJobPart`) | Sig001→A, Sig002→B | End Product | Single page list |
| `minimal-0067.jdf` | A,B | Sig001→A, Sig002→C (undeclared) | A + End Product | Page list missing position for undeclared part |
| `minimal-0079.jdf` | A,B | All sheets → A | A only | Single page list |
| `minimal-0088.jdf` | A,B,C | Sig002→D (undeclared) | A + C + End Product | Page list missing position for undeclared part |
| `minimal-0089.jdf` | A,B,C | (none) | End Product | No page list |

- `GeneratedJDFs/minimal-0001.jdf`: absolute minimum (front side only; no Signa metadata, PagePool, or Output RunList; Types=Imposition+ConventionalPrinting). Cockpit error: “The layout to be imported does not contain a media resource that specifies the size.”
- `GeneratedJDFs/minimal-0002.jdf`: added Paper Media (`Dimension=2592x1728`) and MediaLink; still no Signa metadata/PagePool/Output RunList. Cockpit error persists: “The layout to be imported does not contain a media resource that specifies the size.”
- `GeneratedJDFs/minimal-0003.jdf`: added `MediaRef` under sheet Layout plus a Media Part with `SignatureName`/`SheetName` and `PartIDKeys="SignatureName SheetName"`. Awaiting Cockpit result.
- `GeneratedJDFs/minimal-0004.jdf`: added Plate Media (`Dimension=2592x1728`) plus `MediaRef`/`MediaLink`, and set `Layout/@SurfaceContentsBox` to plate size. Cockpit selection shows “Layout is Valid” with Media Width/Height, but import fails because marks PDF (`data.pdf`) is missing/cannot be copied.
- `GeneratedJDFs/data.pdf`: placeholder marks PDF with `MediaBox` set to plate size (36x24 in) and `TrimBox` set to paper size (36x24 in) for `minimal-0004.jdf` import testing.
- Cockpit import result with `minimal-0004.jdf` + placeholder marks PDF: imports cleanly; workstyle is locked to Perfecting; page list created with a single position named `-1`; plate size and paper size both 36x24; materials fields default to zeros/unknown (thickness/grammage/grain/feed/ink consumption).
- Additional note on `minimal-0004.jdf`: preview shows no page placement (ContentObject missing), and attempting to create an imposed PDF fails with “No PDF file created, Error during imposition.”
- `GeneratedJDFs/minimal-0005.jdf`: adds paper attributes (`Thickness=120`, `Weight=135`, `GrainDirection=LongEdge`, `HDM:FeedDirection=LongEdge`). Cockpit shows Width/Height, Thickness=0.12mm, Grammage=135, Grain Direction=Long Grain; Feed Direction remains Unknown; Ink Consumption Factor stays 0.
- `GeneratedJDFs/minimal-0006.jdf`: adds `HDM:LeadingEdge=1728` on Plate Media to probe Feed Direction behavior. Cockpit Feed Direction still shows Unknown.
- `GeneratedJDFs/minimal-0007.jdf`: sets `GrainDirection="XDirection"` (feed-direction probe) with plate LeadingEdge. Cockpit shows Feed Direction Unknown and Grain Direction Unknown.
- `GeneratedJDFs/minimal-0008.jdf`: sets `GrainDirection="YDirection"` (feed-direction probe) with plate LeadingEdge. Cockpit shows Feed Direction Unknown and Grain Direction Unknown.
- `GeneratedJDFs/minimal-0009.jdf`: adds Paper `Brand`/`DescriptiveName`, `ProductID=12339`, and `Grade=1` to match a Cockpit catalog paper (ProPrint Gloss Text 24 x 36 - 127M). Cockpit shows description/brand/article number and paper type fields populated from catalog; paper white stays blank (matches catalog). Grain Direction shows Long Grain, but Feed Direction remains Unknown. Thickness/grammage remain from JDF (0.12mm/135) rather than catalog (0.081/103.61), suggesting JDF values override catalog for those fields.
- `GeneratedJDFs/minimal-0010.jdf`: adds a minimal `ContentObject` placement (TrimSize=612x792 at 0,0) and a Document RunList page mapping for LogicalPage 0. Cockpit preview shows the page at bottom-left; assigning a PDF updates preview. Imposed proof still fails (“No PDF file created, Error during imposition”), even with a two‑page dummy marks PDF.
- `GeneratedJDFs/minimal-0011.jdf`: adds Marks RunList partitions (`NPage=2`, `Pages=0 ~ 1`) plus Document RunList page counts (`NPage=1`, `Pages=0`). Imposed proof still fails (“No PDF file created, Error during imposition”); doubling marks PDF pages has no effect.
- Additional note: Cockpit’s “Layout Preview” (plate/sheet view) works properly even when imposed PDF generation fails.
- `GeneratedJDFs/minimal-0012.jdf`: adds `TransferCurvePool` (Paper/Plate CTM), `StrippingParams` with `Position`/`StripCellParams`, and `BinderySignature` + `BinderySignatureRef`. Imposed PDF still fails with the standard error, but the attempt runs noticeably longer before aborting.
- `GeneratedJDFs/minimal-0013.jdf`: adds marks `SeparationSpec` list and points Document RunList to `content.pdf` (1-page dummy) while Marks RunList remains `data.pdf`. Imposed PDF still fails with the same error; Cockpit appears to ignore `content.pdf`.
- `GeneratedJDFs/content.pdf`: placeholder content PDF (MediaBox 36x24, TrimBox 8.5x11) used by `minimal-0013.jdf`.
- `GeneratedJDFs/minimal-0014.jdf`: marks RunList now mirrors Signa nesting (Signature → Sheet → Front/Back with `LogicalPage`/`Pages`), marks `NPage=4`, back side Layout included, plate size matches Signa sample. Awaiting Cockpit result.
- `GeneratedJDFs/minimal-0015.jdf`: Document RunList switched to reservation-only (no FileSpec), marks RunList mirrors Signa nesting with front/back `LogicalPage` ranges and `NPage=4`. `data.pdf` regenerated to 4 pages sized to the plate. Imposed PDF still fails; Imposer log reports “MarkObject: no CTM attribute available.”
- `GeneratedJDFs/minimal-0016.jdf`: adds MarkObject `CTM`/`ClipBox`/`Ord`. Imposed PDF now succeeds; assigned page appears on both front/back at bottom-left. Layout Preview shows front sheet bottom-left, back sheet bottom-right (content partly off-sheet), while Imposed PDF shows sheet trim box bottom-left on both sides, indicating a layout-preview vs imposed-PDF discrepancy.
- `GeneratedJDFs/minimal-0017.jdf`: adds a back-side ContentObject with mirrored CTM. Cockpit still shows a single position in the page list, places the page on both sides, and rotates the back 180° while keeping it bottom-left. Layout Preview still draws the back sheet bottom-right of the plate.
- `GeneratedJDFs/minimal-0018.jdf`: adds ConventionalPrintingParams Signature/Sheet/Side partitions to mirror Signa. No observable changes in Cockpit.
- `GeneratedJDFs/minimal-0019.jdf`: adds `StrippingParams SheetLay="Right"` to test back-side preview alignment. Awaiting Cockpit result.
- `GeneratedJDFs/minimal-0020.jdf`: centers the sheet on the plate by offsetting `HDM:PaperRect` and Paper `TransferCurveSet` CTM (x = 199.2755). Cockpit shows the sheet centered, but the page remains bottom-left of the plate on both sides (partially off paper). Layout preview matches (sheet centered, page bottom-left). Imposed PDF still places sheet/page bottom-left on both sides.
- `GeneratedJDFs/minimal-0021.jdf`: offsets ContentObject placement by the same PaperRect offset (x = 199.2755). Cockpit shows the page bottom-left of the sheet (not the plate). Layout Preview looks correct (sheet centered, page bottom-right of the sheet). Imposed PDF still draws the sheet bottom-right of the plate on both sides, though the page shifts right with the sheet offset.
- `GeneratedJDFs/minimal-0022.jdf`: centers StrippingParams `Position RelativeBox` (0.1332701, 0.2483346, 0.8667299, 0.7516654) to force plate-level placement in the imposition engine. No change observed; suspicion shifts to marks PDF TrimBox alignment (it was still bottom-left on all pages).
- `GeneratedJDFs/data.pdf`: marks PDF regenerated with a centered TrimBox to match the PaperRect offset (left=199.2755, bottom=0, right=2791.2755, top=1728) while keeping MediaBox at plate size.
- `GeneratedJDFs/minimal-0023.jdf`: re-runs 0022 settings with the regenerated marks PDF TrimBox. Result: alignment issues resolved across Layout Preview and Imposed PDF, indicating marks PDF TrimBox alignment is required for consistent placement.
- `GeneratedJDFs/data-2p.pdf`: two-page marks PDF (MediaBox=plate size, TrimBox centered to match PaperRect offset) to test reduced marks page counts.
- `GeneratedJDFs/minimal-0024.jdf`: marks page-count split test (1 page per side; `NPage=2`, `Pages=0~1`) using `data-2p.pdf` to check whether Cockpit tolerates 2 total marks pages vs 4. Initial attempts failed because `data-2p.pdf` was corrupt; PDF was regenerated from `data.pdf` via Ghostscript and needs a retry.
- `GeneratedJDFs/minimal-0024.jdf`: result after Ghostscript regeneration: imports cleanly, layout preview and imposed PDF succeed. Cockpit tolerates 2 total marks pages (1 per side) when the marks PDF is valid and TrimBox is aligned.
- `GeneratedJDFs/minimal-0025.jdf`: marks-only layout test with no ContentObject and no Document RunList (marks PDF only). Cockpit warns “layout is empty” and “no layout positions,” but still allows import; imposed PDF and previews succeed (no pages).
- `GeneratedJDFs/minimal-0026.jdf`: adds ContentObject placement while still omitting the Document RunList. Result: no Cockpit warnings; previews and imposed PDF succeed.
- `GeneratedJDFs/minimal-0027.jdf`: working variant that omits PagePool and Output RunList while keeping ContentObject + marks RunList. Result: no Cockpit warnings; previews and imposed PDF succeed.
- `GeneratedJDFs/minimal-0028.jdf`: removes TransferCurvePool while keeping other working settings. Cockpit shows no warnings; imposed PDF/thumbnail preview are correct, but Layout Preview misplaces the sheet (front bottom-left, back bottom-right) so the back page appears off‑sheet. TransferCurvePool appears required for consistent Layout Preview alignment.
- `GeneratedJDFs/minimal-0029.jdf`: removes StrippingParams while keeping TransferCurvePool. Result: no warnings; previews and imposed PDF remain normal.
- `GeneratedJDFs/minimal-0030.jdf`: removes BinderySignature while keeping TransferCurvePool. Result: no warnings; previews and imposed PDF remain normal.
- `GeneratedJDFs/minimal-0031.jdf`: removes ConventionalPrintingParams partitions while keeping other working settings. Result: no warnings; previews and imposed PDF remain normal.
- `GeneratedJDFs/minimal-0032.jdf`: removes MarkObject geometry (CTM/ClipBox/Ord). Result: Cockpit shows no warnings and layouts look normal, but imposed PDF aborts (“Error during imposition”). MarkObject geometry appears required for imposition.
- `GeneratedJDFs/minimal-0033.jdf`: keeps MarkObject geometry but removes marks RunList partitions. Result: no warnings; previews and imposed PDF remain normal.
- `GeneratedJDFs/minimal-0034.jdf`: removes marks SeparationSpec list. Result: no warnings; previews and imposed PDF remain normal.
  - Note: “Allow spot colors for BCMY” remains enabled despite the missing SeparationSpec list.
- `GeneratedJDFs/minimal-0035.jdf`: removes `HDM:PaperRect` entirely (offsets set to 0). Result: Cockpit shows no warnings, but previews misplace the sheet/page (bottom-left on plate), and the imposed PDF places the page relative to the plate rather than the sheet. `HDM:PaperRect` is required for consistent sheet-relative placement.
- `GeneratedJDFs/minimal-0036.jdf`: resets ContentObject offsets to 0 while keeping PaperRect and TransferCurvePool. Result: both previews and imposed PDF place the page at the bottom-left of the plate (partially off-sheet) on both sides. ContentObject placement remains plate-relative unless offsets account for the PaperRect shift.
- `GeneratedJDFs/minimal-0037.jdf`: removes ContentObject while keeping PaperRect + TransferCurvePool. Cockpit warns that the layout is empty/no positions; no page list and no pages appear in previews/imposed PDF, but the sheet placement looks normal (centered at bottom of plate on both sides).

#### Minimal importability scan (Signa_Samples batch)
Quick scan of `Signa_Samples` using the new checklist warnings (2025-01-01 run).
- Most common missing items: `layout:surface_contents_box`, `layout:paper_rect`, `marks:missing`, `content:missing` (each in 2,351 files).
- Less common: `media:paper_dimension` (45 files), `media:plate_dimension` (19 files).
- Highest warning counts (7 each): `Signa_Samples/MixedWorkStyles.jdf`, plus several `Signa_Samples/Job_JDFs_with_descriptions/Q/*-data.jdf` files where Signa emits layout metadata without marks/content objects.
- Interpretation: Signa exports are assumed canonically correct; repeated warnings across hundreds of files indicate the checklist may be too strict or scoped to a narrower JDF subset. Treat large‑volume warnings as validator‑tuning signals, not Signa errors.

### Minimal import findings: required vs optional (Cockpit)
Based on the minimal JDF experiments above. “Required” means removal causes import or imposition failures or visibly incorrect placement.

| Item | Status | Evidence |
| --- | --- | --- |
| Marks PDF (`data.pdf`) with valid MediaBox/TrimBox | Required | Missing/corrupt marks PDF blocks import or imposition (`minimal-0004`, `minimal-0024` before fix). |
| Marks PDF TrimBox aligned with `HDM:PaperRect` | Required for correct placement | Fixing TrimBox alignment resolves preview/imposition misalignment (`minimal-0023`). |
| `HDM:PaperRect` on per‑side Layout | Required for correct sheet‑relative placement | Without it, pages stay plate‑relative and misalign (`minimal-0035`). |
| TransferCurvePool (Paper/Plate CTM) | Required for consistent Layout Preview | Without it, Layout Preview misplaces sheet/back while imposed PDF still looks correct (`minimal-0028`). |
| MarkObject geometry (`CTM`/`ClipBox`/`Ord`) | Required for imposition | Removing it aborts imposed PDF (`minimal-0032`). |
| ContentObject placement | Required for page positions | Without it, Cockpit warns “empty layout” and shows no page list (`minimal-0025`, `minimal-0037`). |
| ContentObject offsets matching PaperRect shift | Required for correct page placement | If offsets are zero while PaperRect is shifted, pages stay plate‑relative (`minimal-0036`). |
| Marks RunList partitions | Optional | Removing partitions still imposes normally (`minimal-0033`). |
| Marks SeparationSpec list | Optional | Removing list does not affect preview/imposition (`minimal-0034`). |
| Document RunList | Optional (for layout/marks) | Cockpit accepts layout without it if ContentObject exists (`minimal-0026`). |
| PagePool RunList | Optional | Removing it does not affect preview/imposition (`minimal-0027`). |
| Output RunList | Optional | Removing it does not affect preview/imposition (`minimal-0027`). |
| StrippingParams | Optional | Removing it does not affect preview/imposition (`minimal-0029`). |
| BinderySignature | Optional (single‑sheet test) | Removing it does not affect preview/imposition (`minimal-0030`). |
| ConventionalPrintingParams partitions | Optional | Removing partitions does not affect preview/imposition (`minimal-0031`). |

### Minimal working recipe (single sheet, marks + one page)
Use this as the smallest known‑good starting point for a Cockpit‑importable JDF with correct sheet/page placement and imposed PDF output.

Required elements and attributes:
- Paper `Media` and Plate `Media` with `Dimension` and links.
- `Layout` with `SignatureName`, `SheetName`, and per‑side child `Layout` elements (`Side="Front"`/`"Back"`).
- Per‑side `Layout` with `HDM:PaperRect` matching the sheet location on the plate.
- `TransferCurvePool` with `TransferCurveSet Name="Paper"` and `Name="Plate"` CTMs aligned to the same sheet/plate origins as `HDM:PaperRect` and `SurfaceContentsBox`.
- `MarkObject` with `CTM`, `ClipBox`, and `Ord`.
- `ContentObject` with `CTM`/`TrimCTM`, `TrimSize`, and `ClipBox` positioned relative to the sheet (i.e., include the PaperRect offset).
- Marks `RunList` with a valid `FileSpec` to the marks PDF (`data.pdf`).
- Marks PDF with MediaBox = plate size and TrimBox aligned to `HDM:PaperRect`.

Optional in this minimal case:
- Document `RunList` (can be omitted if ContentObject is present).
- PagePool and Output RunLists.
- StrippingParams and BinderySignature.
- ConventionalPrintingParams partitions.
- Marks RunList partitions and SeparationSpec list.

### Page list behavior (Cockpit, importer)
- Page list entries are driven by `ContentObject/@Ord` (placement group) and `ContentObject/@DescriptiveName` (label).
- Distinct `Ord` values create distinct page list entries even when labels are identical (`minimal-0041.jdf`).
- Changing `DescriptiveName` alone updates labels but does not create distinct placements when `Ord` is constant (`minimal-0042.jdf`).
- Document RunList logical pages are not sufficient by themselves to create separate page list entries for multi‑sheet layouts (`minimal-0039.jdf`).

### Multi-signature deltas (Signa sample)
Sample: `Signa_Samples/16PagePerfectBoundSamplePlus8Page.jdf` (cover + two text signatures, single product part).

Structural differences vs a single‑sheet layout:
- **Multiple signature nodes.** `Layout` contains multiple child `Layout` nodes with distinct `SignatureName` values (`Sig001`, `Sig002`, `Sig003`). Each signature node nests a single sheet `Layout` (`SheetName="Cover"`, `"Sig 1"`, `"Sig 2"`).
  - XPath: `/JDF/ResourcePool/Layout/Layout[@SignatureName]`
- **Per‑signature/per‑sheet partitions.** Many resources expand to `PartIDKeys="SignatureName SheetName"` with one part per sheet: `Media` (paper/plate), `TransferCurvePool`, `StrippingParams`, `ConventionalPrintingParams`, and `Component` blocks.
  - XPath (Media parts): `/JDF/ResourcePool/Media[@PartIDKeys="SignatureName SheetName"]/Media[@SignatureName][@SheetName]`
  - XPath (TransferCurvePool parts): `/JDF/ResourcePool/TransferCurvePool[@PartIDKeys="SignatureName SheetName"]/TransferCurvePool[@SheetName]`
  - XPath (StrippingParams parts): `/JDF/ResourcePool/StrippingParams[@PartIDKeys="SignatureName SheetName"]/StrippingParams[@SignatureName][@SheetName]`
  - XPath (ConventionalPrintingParams parts): `/JDF/ResourcePool/ConventionalPrintingParams[@PartIDKeys="SignatureName SheetName Side"]/ConventionalPrintingParams[@SheetName]`
  - XPath (Component blocks): `/JDF/ResourcePool/Component[@PartIDKeys="SignatureName SheetName BlockName"]/Component[@SignatureName][@SheetName]`
- **Marks RunList ranges advance per sheet.** Marks `RunList` partitions use `LogicalPage`/`Pages` ranges that increment by sheet (e.g., cover 0–1 / 2–3, Sig 1 4–5 / 6–7, Sig 2 8–9 / 10–11). This implies marks pages are allocated per sheet in order of the signature list.
  - XPath: `/JDF/ResourcePool/RunList[@ProcessUsage="Marks" or @HDM:OFW]/RunList[@SignatureName]/RunList[@SheetName]/RunList[@Side]`
- **Marks logical pages must reset per sheet in multi‑signature, multi‑sheet layouts to preserve trim boxes.** In generated tests, Cockpit drops the imposed PDF trim box for secondary sheets unless the marks `LogicalPage` counter is reset per sheet (and per signature). Evidence: `GeneratedJDFs/minimal-0055.jdf` (missing Text1/Text2 trim), `GeneratedJDFs/minimal-0056.jdf` (reset per signature restores Text2 only), `GeneratedJDFs/minimal-0057.jdf` (reset per sheet restores all trim boxes).
- **Assembly + finishing resources appear.** Multi‑signature jobs emit `Component` blocks and `Assembly`/`AssemblySection` resources that link signatures to finishing (gathering/spine/cover). These are keyed by `AssemblyIDs` and `SignatureName`/`SheetName`.
  - XPath (Assembly sections): `/JDF/ResourcePool/Assembly/AssemblySection[@HDM:SignatureName][@HDM:SheetName]`
- **BinderySignature per fold scheme.** Each signature sheet references a `BinderySignature` (`FoldCatalog` varies by signature) via `BinderySignatureRef` inside its `StrippingParams`.
  - XPath: `/JDF/ResourcePool/StrippingParams/StrippingParams/BinderySignatureRef`

### Multi-sheet, single-signature experiment (generator)
- `GeneratedJDFs/minimal-0038.jdf`: two sheets in the same signature (`Sheet1`, `Sheet2`) with marks partitions and ContentObject placements. Result: no warnings; Cockpit still shows a single page placed once per surface (no per‑sheet duplication yet), so page mapping likely needs explicit per‑sheet logical pages.
- `GeneratedJDFs/minimal-0039.jdf`: same two-sheet setup but with per‑sheet document LogicalPage offsets (1 page per sheet, `content-2p.pdf`). Result: still only one page in the page list, placed on each surface; page mapping likely needs additional structure beyond document logical pages.
- `GeneratedJDFs/minimal-0040.jdf`: adds per‑sheet ContentObject `Ord`/`DescriptiveName` increments plus document LogicalPage offsets. Result: Cockpit shows two pages; Page 1 maps to Sheet1 (front/back) and Page 2 maps to Sheet2 (front/back). Previews and imposed PDF appear normal.
  - User observation: Cockpit’s page list is driven by `ContentObject/@Ord` and `ContentObject/@DescriptiveName`; document runlists are not the primary source for page list entries on import.
- `GeneratedJDFs/minimal-0041.jdf`: control test with per‑sheet `Ord` increments only (DescriptiveName constant).
- `GeneratedJDFs/minimal-0041.jdf`: result: two pages appear, both labeled “1”; ord seems to drive distinct entries even with identical DescriptiveName.
- `GeneratedJDFs/minimal-0042.jdf`: control test with per‑sheet `DescriptiveName` increments only (Ord constant).
- `GeneratedJDFs/minimal-0042.jdf`: result: page list shows “1 (2)” and “2 (2)”; Ord appears to control position (placement group), DescriptiveName controls the label. Same content placed on both sheets/sides when Ord is constant.
  - Note: “Allow spot colors for BCMY” remains enabled despite the missing SeparationSpec list.

### Ganged sheets deltas (Signa sample)
Sample: `Signa_Samples/GangedPostcards-WorkandTurn.jdf` (Work-and-turn, multiple postcards ganged on one sheet).

Structural markers vs a single-piece layout:
- **Multiple AssemblyIDs within one sheet.** ContentObjects cycle through `HDM:AssemblyIDs` suffixes (`F02-01_ui_1x1_1` … `_4`) to represent distinct ganged pieces on the same press sheet.
  - XPath: `/JDF/ResourcePool/Layout//ContentObject[@HDM:AssemblyIDs]`
- **CuttingParams enumerates each ganged block.** `CutBlock` elements repeat per `AssemblyIDs` with distinct `BlockName` entries (`FB_001_B_1_1`, `FB_001_B_1_2`, ...).
  - XPath: `/JDF/ResourcePool/CuttingParams/CutBlock[@AssemblyIDs][@BlockName]`
- **Component blocks map AssemblyIDs → BlockName pairs.** Each ganged piece has a `Component` entry tied to its `AssemblyIDs` and block name.
  - XPath: `/JDF/ResourcePool/Component//Component[@AssemblyIDs][@BlockName]`
- **FoldingParams partitions per ganged piece.** Nested `FoldingParams` include `AssemblyIDs` + `BlockName` for each ganged block within the sheet.
  - XPath: `/JDF/ResourcePool/FoldingParams//FoldingParams[@AssemblyIDs][@BlockName]`
- **StrippingParams repeat per ganged piece.** Multiple `StrippingParams` entries exist for the same sheet, each with a distinct `AssemblyIDs` + `BinderySignatureName` pair.
  - XPath: `/JDF/ResourcePool/StrippingParams/StrippingParams[@AssemblyIDs][@BinderySignatureName]`
- **Assembly sections list each ganged piece separately.** `AssemblySection` entries repeat per `AssemblyIDs` to track the pieces in finishing order.
  - XPath: `/JDF/ResourcePool/Assembly/AssemblySection[@AssemblyIDs]`

### Ganged sheets experiment (generator)
- `GeneratedJDFs/minimal-0043.jdf`: 2x2 content grid on one sheet with per‑slot `AssemblyIDs`, `Ord`, and `DescriptiveName` increments. Result: 4 page positions appear and map to the 2x2 grid, but the left column is offset off‑sheet; back side rotates each page but does not flip the grid order (not a true work‑and‑turn backing‑up).
- `GeneratedJDFs/minimal-0044.jdf`: grid aligned to the PaperRect origin with back‑side column order reversed. Result: thumbnail preview shows the sheet offset left; layout preview flips sheet left/right between sides, causing pages to fall off on the back; imposed PDF centers sheet but left column pages still fall off‑sheet.
- `GeneratedJDFs/minimal-0045.jdf`: grid anchored to the centered PaperRect with adjusted content offsets. Result: sheet centers correctly on both sides and all pages are on‑sheet; page cluster is slightly high (more top margin than bottom).
- `GeneratedJDFs/minimal-0046.jdf`: vertical centering tweak for the 2x2 grid (ContentObject Y offset centered). Result: page cluster centered vertically on the sheet; no issues observed.
- `GeneratedJDFs/minimal-0047.jdf`: adds an `Assembly` resource with per‑slot `AssemblySection` entries matching the grid’s `HDM:AssemblyIDs`. Result: no observable differences from `minimal-0046.jdf`.
- `GeneratedJDFs/minimal-0048.jdf`: uses two `HDM:AssemblyIDs` prefixes split across the grid (first two slots vs last two). Result: no problems observed.
- `GeneratedJDFs/minimal-0049.jdf`: multi‑signature split (Cover + Body) with separate `SignatureName` branches and per‑sheet page list entries. Result: two sheets created (Cover, Body) and a page list with two positions; page 1 appears on both sides of Cover, page 2 appears on both sides of Body; no problems observed.
- `GeneratedJDFs/minimal-0049.jdf`: Cockpit shows WorkStyle as editable for both Cover and Body; switching Body from Perfecting to Sheetwise hides the paper/trimbox rendering (page placement still present).
- `GeneratedJDFs/minimal-0049.jdf`: correction — since `minimal-0049.jdf` the Body has no sheet/trim box in the imposed PDF regardless of workstyle; this is not caused by switching Perfecting→Sheetwise.
- `GeneratedJDFs/minimal-0050.jdf`: multi‑signature split with per‑signature workstyle (Cover=Sheetwise, Body=Perfecting). Result: no problems observed; cover shows Sheetwise; changing Cover to Perfecting does not hide the paper/trimbox (unlike switching Body from Perfecting to Sheetwise in `minimal-0049.jdf`).
- `GeneratedJDFs/minimal-0051.jdf`: adds per‑signature `StrippingParams` and `BinderySignature` refs on top of the cover/body workstyle split. Result: no noticeable change from `minimal-0050.jdf`.
- `GeneratedJDFs/minimal-0052.jdf`: resets marks logical pages per signature (uses 2‑page marks PDF) to test missing Body trim box. Result: Body sheet/trim box appears in the imposed PDF and stays visible when switching to Sheetwise.
- `GeneratedJDFs/minimal-0053.jdf`: resets marks logical pages per signature but uses 4‑page marks PDF (tests reset vs page count). Result: no observable difference from `minimal-0052.jdf` (Body trim box still appears).
- `GeneratedJDFs/minimal-0054.jdf`: emits a separate Marks RunList per signature while keeping continuous logical pages. Result: no observable changes.
- `GeneratedJDFs/minimal-0055.jdf`: true multi‑signature layout (Sig001 has Cover + Text1, Sig002 has Text2) with per‑sheet resources and marks pages expanded to 6. Result: layout shows Cover, Text1, Text2 with three page list positions; Text1 and Text2 missing sheet/trim box in imposed PDF; no other issues observed.
- `GeneratedJDFs/minimal-0056.jdf`: true multi‑signature layout with marks logical pages reset per signature (tests trim box disappearance). Result: trim box appears on Cover and Text2, but still missing on Text1.
- `GeneratedJDFs/minimal-0057.jdf`: true multi‑signature layout with marks logical pages reset per signature and per sheet. Result: trim boxes appear on all three sheets.
- `GeneratedJDFs/minimal-0058.jdf`: true multi‑signature layout with two job parts (A/B) mapped per signature and `HDM:JobPart`/`HDM:RunlistIndex` on ContentObjects. Result: Cockpit warns it cannot copy the Signa Station data file (`SignaData.sdf`), but import succeeds; two page lists appear (`minimal-0058_A` and `minimal-0058_B`) and two products show in the Imposition list. Cover + Text1 are grouped under Product A (Sig001), Text2 under Product B (Sig002). Previews and imposed PDF appear normal.
- `GeneratedJDFs/minimal-0059.jdf`: same as `minimal-0058.jdf` but omits `HDM:SignaBLOB`. Result: no warning about missing Signa Station data file; two page lists/products still appear and previews/imposed PDF remain normal.
- `GeneratedJDFs/minimal-0060.jdf`: same as `minimal-0059.jdf` but omits `HDM:SignaJDF` as well. Result: no Cockpit warnings; two page lists/products still appear and previews/imposed PDF remain normal.
- `GeneratedJDFs/minimal-0061.jdf`: omits `HDM:SignaJob` (and `HDM:SignaBLOB`/`HDM:SignaJDF`). Result: only one product appears in Cockpit (default “End Product”), with a single page list containing three positions; signature breakdown remains `Sig001=Cover+Text1`, `Sig002=Text2`. Previews and imposed PDF are normal.
- `GeneratedJDFs/minimal-0062.jdf`: same as `minimal-0061.jdf` but retains `HDM:JobPart` on ContentObjects (no `HDM:SignaJob` or `HDM:SignaJobPart`, no `HDM:RunlistIndex`). Result: no observable difference from `minimal-0061.jdf` (single product/page list).
- `GeneratedJDFs/minimal-0063.jdf`: includes `HDM:SignaJob` + `HDM:SignaJobPart` but omits `HDM:JobPart` and `HDM:RunlistIndex` on ContentObjects. Result: still a single product (“End Product”), signature structure remains, and no page list is created.
- `GeneratedJDFs/minimal-0064.jdf`: adds `HDM:RunlistIndex` but still omits `HDM:JobPart` on ContentObjects (with `HDM:SignaJob` + `HDM:SignaJobPart`). Result: no observable difference from `minimal-0063.jdf`; no page list created.
- `GeneratedJDFs/minimal-0065.jdf`: includes `HDM:JobPart` on ContentObjects but omits `HDM:RunlistIndex` (with `HDM:SignaJob` + `HDM:SignaJobPart`). Result: two page lists and two products are created; previews and imposed PDF are normal.
- `GeneratedJDFs/minimal-0066.jdf`: keeps `HDM:SignaJob` but omits `HDM:SignaJobPart` entries (still has `HDM:JobPart` on ContentObjects). Result: Cockpit returns to a single product and single page list.
- `GeneratedJDFs/minimal-0067.jdf`: mismatch test (`HDM:SignaJobPart` = A,B while ContentObjects use A,C). Result: Cockpit shows two products (A and “End Product”), page list has only positions 1–2 (Cover/Text1), while Text2 displays position 3 that is not present in the page list.
- `GeneratedJDFs/minimal-0068.jdf`: inverse mismatch (`HDM:SignaJobPart` = A,C while ContentObjects use A,B). Result: no observable difference from `minimal-0067.jdf` (A + “End Product”; page list desync remains).
- `GeneratedJDFs/minimal-0069.jdf`: only Sig001 ContentObjects carry `HDM:JobPart` (Sig002 omits it) with `HDM:SignaJobPart` A/B present. Result: no observable difference from the mismatch case (still A + “End Product” with page list desync).
- `GeneratedJDFs/minimal-0070.jdf`: only Sig002 ContentObjects carry `HDM:JobPart` (Sig001 omits it) with `HDM:SignaJobPart` A/B present. Result: products are B and “End Product”; Product B contains Sig002/Text2, End Product contains Sig001/Cover+Text1. Only position 3 appears in the page list.
- `GeneratedJDFs/minimal-0071.jdf`: single signature with two sheets using per-sheet `HDM:JobPart` (Cover=A, Text1=B). Result: two products (A and B). Product A contains Sig001/Cover; Product B contains Sig001/Text1. Two page lists appear (`PL_minimal-0071_A` position 1, `PL_minimal-0071_B` position 2). Previews and imposed PDF are normal.
- `GeneratedJDFs/minimal-0072.jdf`: single signature with three sheets using per-sheet `HDM:JobPart` (Cover=A, Text1=B, Text2=A). Result: two products. Product A contains Sig001/Cover and Sig001/Text2; Product B contains Sig001/Text1. Page lists: `PL_minimal-0072_A` has positions 1 and 3; `PL_minimal-0072_B` has position 2. Previews and imposed PDF are normal.
- `GeneratedJDFs/minimal-0073.jdf`: single signature with three sheets where only Cover/Text2 have `HDM:JobPart=A` and Text1 omits JobPart (SignaJobPart A/B declared). Result: only Product A is created; it contains Sig001 with all three sheets. One page list (`PL_minimal-0073_A`) includes all three positions.
- `GeneratedJDFs/minimal-0074.jdf`: single signature with three sheets where only Text1 has `HDM:JobPart=B` (Cover/Text2 omit JobPart). Result: two products (A and B) with two page lists; Cover and Text2 land under Product A, Text1 under Product B.
- `GeneratedJDFs/minimal-0075.jdf`: same as `minimal-0074.jdf` but with no default signature job part (`--signature-job-parts` omitted). Result: no observable difference from `minimal-0074.jdf` (A contains Cover/Text2, B contains Text1).
- `GeneratedJDFs/minimal-0076.jdf`: only `HDM:SignaJobPart=B` declared; Text1 has `HDM:JobPart=B`, Cover/Text2 omit JobPart. Result: only “End Product” exists; one page list with positions 1–3.
- `GeneratedJDFs/minimal-0077.jdf`: only `HDM:SignaJobPart=A` declared while Text1 is tagged `HDM:JobPart=B`. Result: everything collapses into “End Product.”
- `GeneratedJDFs/minimal-0078.jdf`: only `HDM:SignaJobPart=A` declared and all sheets tagged `HDM:JobPart=A`. Result: everything still collapses into “End Product” with a single page list.
- `GeneratedJDFs/minimal-0079.jdf`: `HDM:SignaJobPart` declares A/B but all sheets tagged `HDM:JobPart=A`. Result: a single Product A is created containing all sheets and one page list.
- `GeneratedJDFs/minimal-0080.jdf`: `HDM:SignaJobPart` declares A/B but all sheets tagged `HDM:JobPart=B`. Result: a single Product B is created containing all sheets and one page list.
- `GeneratedJDFs/minimal-0081.jdf`: three signatures with JobPart mapping A/A/B. Result: two products; Product A contains Sig001/Cover and Sig002/Text1, Product B contains Sig003/Text2. Page lists: `PL_minimal-0081_A` has positions 1–2; `PL_minimal-0081_B` has position 3.
- `GeneratedJDFs/minimal-0082.jdf`: same signatures but Sig002 has no JobPart. Result: no observable difference from `minimal-0081.jdf` (still A and B split with positions 1–2 and 3).
- `GeneratedJDFs/minimal-0083.jdf`: only Sig003 has JobPart B; Sig001/Sig002 omit JobPart. Result: same as `minimal-0082.jdf` (A/B split with positions 1–2 and 3).
- `GeneratedJDFs/minimal-0084.jdf`: all signatures tagged JobPart B. Result: single Product B containing all signatures/sheets and one page list.
- `GeneratedJDFs/minimal-0085.jdf`: two signatures with JobPart B assigned only to Sig001; Sig002 is untagged. Result: two products; Product B contains Sig001/Cover and Product A contains Sig002/Text1. Page lists: `PL_minimal-0085_B` has position 1, `PL_minimal-0085_A` has position 2.
- `GeneratedJDFs/minimal-0086.jdf`: `HDM:SignaJobPart` declares A/B/C and all signatures tagged JobPart B. Result: single Product B containing all signatures/sheets and one page list.
- `GeneratedJDFs/minimal-0087.jdf`: `HDM:SignaJobPart` declares A/B/C and signatures map A/C/A. Result: two products (A and C). Product A contains Sig001/Cover and Sig003/Text2; Product C contains Sig002/Text1. Page lists: A has positions 1 and 3; C has position 2.
- `GeneratedJDFs/minimal-0088.jdf`: `HDM:SignaJobPart` declares A/B/C but Sig002 uses undeclared JobPart D. Result: products A, C, and “End Product.” Product A contains Sig001/Cover; Product C contains Sig003/Text2; End Product contains Sig002/Text1. Page lists exist for A (position 1) and C (position 3); position 2 is missing.
- `GeneratedJDFs/minimal-0089.jdf`: `HDM:SignaJobPart` declares A/B/C but no `HDM:JobPart` is present on any ContentObjects. Result: only “End Product” is created and no page list appears.

#### JobPart/Product takeaways
- Cockpit uses `HDM:JobPart` to build page lists; without it, product/page list splitting does not occur.
- `HDM:SignaJobPart` must declare at least two parts for Cockpit to create named products (A/B/etc); a single declared part collapses to “End Product.”
- Untagged signatures/sheets default to Product A when A/B are declared.
- Undeclared JobPart values introduce “End Product” and can drop page list positions.
- Observation: even with `WorkStyle="Perfecting"`, Cockpit does not enforce backing‑up correctness; it will accept placements that cannot back up properly (e.g., identical bottom-left placement on both sides). Practical safety checks appear to live in Signa, while Cockpit applies the JDF as-is.

## Schema deviations (top occurrences, JDF 1.7)
These are the most frequent schema validation errors from `schema-summary.csv`; they highlight where Signa JDFs diverge from the base 1.7 schema.
- `AssemblyIDs` attribute not allowed (11,222 occurrences). Signa uses `AssemblyIDs` widely on layout/content and finishing resources.
- `PositionX`/`PositionY` attributes not allowed (7,129 each). These appear on `ContentObject` with values like `Center`; Signa uses them as alignment hints even though the base schema does not allow these attributes there.
- `BlockName` values with parentheses violate name token rules (multiple occurrences; e.g., `Part_1_Form_1_(Sig_1)_B_1_1`). This is a naming‑rule issue, not a version issue.

### Schema hotspots
Top files by schema error count (useful for future deep dives):
- `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0753 GGLS - Korn Ferry_Q0753 text w barcode-data.jdf` (1,040)
- `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1688 GGLS - Korn Ferry_Q1688 text w barcode-data.jdf` (1,040)
- `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2478 GGLS - Korn Ferry_Q2478 text w barcode-data.jdf` (1,040)
- `Signa_Samples/Job_JDFs_with_descriptions/Q/P1701 HealthCare_P1701 Text-data.jdf` (965)
- `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1323 HealthCare Can_P1701 Text-data.jdf` (965)
- `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1946 CAOT_Q1946 Text-data.jdf` (957)
- Full list: `schema-hotspots.csv`.

Top invalid tokens (schema message frequency):
- `AssemblyIDs` (11,222)
- `PositionX` (7,129)
- `PositionY` (7,129)
- `BlockName` (2,658)
- `BinderySignatureName` (865)
- Hotspots are good candidates for targeted schema‑vs‑dialect analysis and deeper rule extraction.

## HDM usage patterns (sample‑wide)
- Most common HDM attributes in the sample set: `PoolIndex` (RunList), `Type`/`IsMapRel` (SeparationSpec), `FinalPageBox`/`PageOrientation` (ContentObject), `AssemblyFB`/`AssemblyIDs` (ContentObject), `CIP3BlockTrf` (CutBlock), `PaperRect` (Layout), `ClosedFoldingSheetDimensions`/`OpenedFoldingSheetDimensions` (Component), `CIP3FoldSheetIn_1/2` (FoldingParams), `BlankPage` (RunList). (Stability: mostly Stable, but see notes below.)
- Most common HDM elements: `HDM:CombiningParams` and `HDM:Signa*` metadata (`SignaBLOB`, `SignaJDF`, `SignaGenContext`, `SignaJob`, `SignaJobPart`). (Stability: Signa* is Transport-only; CombiningParams is Advisory)
- Near‑universal per‑file presence (in this sample set): `HDM:PaperRect`, `HDM:FinalPageBox`, `HDM:PageOrientation`, `HDM:CIP3BlockTrf`, `HDM:OFW`, `HDM:OrigNameBySigna` appear in essentially every JDF; `HDM:BlankPage` is more sporadic. (Stability: `PaperRect`/`FinalPageBox`/`PageOrientation` Stable; `OFW`/`OrigNameBySigna` Decorative)
### Hotspot deep dive: Q0753 (Korn Ferry text w barcode)
Schema errors are dominated by two patterns:
- `PositionX`/`PositionY` appear only on `ContentObject` (512 placements) with values like `Center`; treat as alignment hints alongside CTM/TrimCTM rather than required geometry inputs.
- `AssemblyIDs` appears on `Component`, `CutBlock`, `FoldingParams`, `AssemblySection`, and `StrippingParams` as a cross‑resource join key; the base schema rejects it, but Signa uses it to tie block/assembly context. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0753 GGLS - Korn Ferry_Q0753 text w barcode-data.jdf`.

### Dialect rules extracted from hotspots
- `PositionX`/`PositionY` on `ContentObject` (typically `Center`) act as alignment hints and can be ignored when CTM/TrimCTM are present. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0753 GGLS - Korn Ferry_Q0753 text w barcode-data.jdf`.
- `AssemblyIDs` is used across multiple resources (`Component`, `CutBlock`, `FoldingParams`, `AssemblySection`, `StrippingParams`) as a join key despite being invalid in the base schema. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0753 GGLS - Korn Ferry_Q0753 text w barcode-data.jdf`.
- `BlockName` and `BinderySignatureName` may include parentheses, which violates schema name-token rules but is common in Signa naming (seen on `Component`, `CutBlock`, `FoldingParams`, `CombiningParams`, `StrippingParams`). Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/P1701 HealthCare_P1701 Text-data.jdf`.

## Remaining gaps and working theories
- `HDM:*` attributes (e.g., `HDM:ChangeInfo`, `HDM:OFW`, `HDM:Purpose`, `HDM:FoldRule`) are Heidelberg extensions and not defined in the core JDF spec. Treat them as vendor-specific until Heidelberg/HDM documentation is available.
- `SignaData.sdf` is a binary blob; no structure has been decoded from it yet.
- Terminology note: we use "job part" or "product part" to describe multiple products in a single job; "versioning" typically refers to plate-level replacements (often the K plate) to create multiple product versions.

## Invariants across observed Signa 21 JDFs
- Root `Types` always include `Imposition` and consistently include `ConventionalPrinting`.
- `ResourceLinkPool` is present and `CombinedProcessIndex` follows the order of `Types`.
- `ResourcePool/Layout` is present with `PartIDKeys="SignatureName SheetName Side"` and Signature → Sheet → Side nesting.
- Sheet-level `Layout` includes `SurfaceContentsBox`.
- Side-level `Layout` includes `HDM:PaperRect` and `Side="Front|Back"` (work‑and‑turn can emit only `Front`).
- `MarkObject` and `ContentObject` are children of side-level Layout when a layout is present.
- `ContentObject` carries `CTM`/`TrimCTM`, `TrimSize`, and `Ord`/`DescriptiveName` for page list labeling.
- Paper and plate `Media` resources include `Dimension` and `PartIDKeys="SignatureName SheetName"`.
- `RunListLink` entries for `ProcessUsage="Document"` and `ProcessUsage="Marks"` appear in every sample.
- Marks RunList uses `PartIDKeys="SignatureName SheetName Side"` and links a marks PDF via `FileSpec/@URL`.
- `HDM:PaperRect`, `HDM:FinalPageBox`, and `HDM:PageOrientation` appear in essentially all samples.
- `HDM:CIP3BlockTrf` is present whenever `CutBlock` appears.
- `HDM:OFW` appears on RunList in nearly all samples (keep verbatim; vendor-specific).
- `Version="1.3"` with higher `MaxVersion` is the common Signa export pattern.

## Validator investigation notes (current batch patterns)
- Investigation flags are not errors; they highlight where Signa varies across samples so we can form hypotheses.
- Minimal importability warnings dropped to zero after accounting for Media `Dimension` on nested `Media` parts; several Signa exports carry dimensions only on per‑signature Media parts rather than the top-level Media node.
- `HDM:CIP3FoldSheetIn_1/2` is frequently missing when `FoldingParams@NoOp=true` or when no `FoldCatalog` and no `Fold` elements are present, suggesting Signa omits folded-sheet dimensions when fold mechanics are not modeled.
  - Evidence: `Signa_Samples/GangedPostcards-WorkandTurn.jdf`, `Signa_Samples/PB-BookText2Versions-Perfecting.jdf`.
- `HDM:CIP3FoldSheetIn_1/2` can also be absent even when `FoldCatalog` is present (e.g., book text layouts), so the presence of a fold catalog is not sufficient to assume folded-sheet dimensions are emitted.
  - Evidence: `Signa_Samples/SS-BookText-Perfecting.jdf`.
- `HDM:CIP3FoldSheetIn_1/2` appears to be emitted on the top-level `FoldingParams` node; nested `FoldingParams` partitions (Signature/Sheet/Block) often omit it even when `FoldCatalog` is present.
  - Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0306 GRepro_Q0306 stepped brochure-data.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0216 DLI - Taradel_Q0216-data.jdf`.
- `FoldCatalog="unnamed"` likely denotes a custom or montage-style layout that does not use a formal folding scheme; in these cases `HDM:CIP3FoldSheetIn_*` may be absent.
  - Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q0905 MYC Sunshine 1A_Q0905-data.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2009 MHCC_Q2009 text-data.jdf`.
- Low-priority investigation: a few top-level `FoldingParams` have a named `FoldCatalog` but only one of `HDM:CIP3FoldSheetIn_*` is present; this appears isolated (e.g., `F4-1` stepped brochure) and should not block interpretation.
- Low-priority investigation: some JDFs omit `HDM:AssemblyIDs` and `HDM:AssemblyFB` on `ContentObject` while still using `AssemblyIDs` on `CutBlock`, `Component`, or `StrippingParams`; this appears to be a variation in certain builds or job types, not necessarily an invalid layout.
- Low-priority investigation: Simplex jobs often omit `HDM:AssemblyIDs`/`HDM:AssemblyFB` on `ContentObject`, relying on block-level AssemblyIDs instead.
- Low-priority investigation: `HDM:SignaJob` is missing in some exports from older Signa builds (observed in Hotfix builds with lower build numbers).
- Low-priority investigation: some jobs with a single plate `Media` omit `HDM:LeadingEdge` even in newer builds.
- `HDM:LeadingEdge` matches the plate `Media/@Dimension` height (second value) in all sampled files that provide both values. When missing, the plate height can be used as an inferred leading edge. If a consumer emits it preemptively, Prinect is generally tolerant of extra metadata and will ignore what it does not need.
- Low-priority investigation: many missing `HDM:LeadingEdge` cases also omit plate `Media/@Dimension`, which suggests a more general plate metadata omission rather than a specific LeadingEdge rule.
- `RunListLink` for `ProcessUsage="PagePool"` is optional in Signa exports. When the PagePool RunList is omitted entirely, there is no corresponding link. When a PagePool RunList is present (`DescriptiveName="PagePool"`), a matching `RunListLink` is expected.
- `HDM:ClosedFoldingSheetDimensions` and `HDM:OpenedFoldingSheetDimensions` are often missing on Component blocks even when FoldingParams exist, implying block-level folded/unfolded sizes may only appear when downstream finishing or folding data is explicit.
  - Evidence: `Signa_Samples/MultiSig-Perfecting.jdf`, `Signa_Samples/GangedPostcards-Perfecting.jdf`.
- `HDM:ClosedFoldingSheetDimensions` and `HDM:OpenedFoldingSheetDimensions` correlate with block components that carry `AssemblyIDs`; blocks without `AssemblyIDs` frequently omit these attributes.
  - Evidence: `Signa_Samples/MultiSig-Perfecting.jdf`, `Signa_Samples/GangedPostcards-Perfecting.jdf`.
- `HDM:OutputBlockName` appears in two patterns: either on the top-level `HDM:CombiningParams`, or on leaf `HDM:CombiningParams` entries with `BlockName` while the top-level element is blank. This suggests the attribute applies at the level where the block combination is resolved, not uniformly at every node.
  - Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
  - Evidence (leaf-level OutputBlockName): `Signa_Samples/GangedPostcards-WorkandTurn.jdf`.

## HDM inferences (observed)
These inferences are based on patterns across the Signa samples. They are best-effort and should be treated as hypotheses until contradicted by new evidence.

**Inference Rules**
- Preserve all `HDM:*` attributes and elements as-is even when meaning is unclear.
- Prefer inferences that are repeated across multiple work styles and job types.
- Mark confidence as High when behavior is consistent across samples, Medium when limited to a few, Low when ambiguous.
- Avoid using HDM attributes as validation requirements unless a failure mode is known.
- High-confidence HDM fields are treated as validator errors; medium/low confidence fields are emitted as investigation flags (non-fatal) with contextual hints.
- Validator reports include a per-file context line (work styles, layout counts, AssemblyIDs) plus a job-shape summary to cluster where fields are missing.
- For high-confidence fields, the validator also checks expected value shapes (e.g., `HDM:PaperRect` and `HDM:FinalPageBox` are four numbers; `HDM:PageOrientation` is 0/90/180/270; `HDM:SignaBLOB` and `HDM:SignaJDF` URLs match `SignaData.sdf` and `data.jdf`).
- The validator derives `HDM:PageOrientation` from `ContentObject/@TrimCTM` (or `@CTM` when TrimCTM is absent) by extracting the rotation angle; mismatches are flagged.
- The validator derives `HDM:FinalPageBox` by applying `ContentObject/@TrimCTM` to the rectangle defined by `ContentObject/@TrimSize` (swapping width/height for 90/270 orientations) when TrimCTM has uniform scale; non-uniform matrices are skipped.
- If `TrimCTM` is absent, the validator can fall back to deriving `HDM:FinalPageBox` by centering `TrimSize` inside `ContentObject/@ClipBox` (low-confidence, informational only).
- The validator derives `HDM:PaperRect` by using the Paper `Media/@Dimension` (width/height) anchored at `(-PaperCTM.e, -PaperCTM.f)` where `PaperCTM` is the `TransferCurveSet Name="Paper"` CTM from the referenced `TransferCurvePool`.
  - Validation note: multiple Paper TransferCurveSet entries can exist (e.g., multi-signature jobs); the validator accepts any CTM that matches the `HDM:PaperRect`.
- The validator derives `HDM:LeadingEdge` from plate `Media/@Dimension` height when present and flags mismatches.
- The validator derives `HDM:CIP3BlockTrf` as `BlockTrf` translated by the matching `HDM:PaperRect` lower-left (using signature/sheet partitions when available); if multiple PaperRect values conflict for that part, the check is skipped.
- The validator derives `HDM:CIP3FoldSheetIn_1/2` from matching Component Block folding dimensions (`HDM:OpenedFoldingSheetDimensions` or `HDM:ClosedFoldingSheetDimensions`) using AssemblyIDs and BlockName partitions; if a FoldingParams references an output block, the validator expands it using `HDM:CombiningParams` (favoring matching `SignatureName`/`SheetName`) before matching. It accepts swapped order (width/height) and skips validation if blocks are ambiguous or missing fold dimensions.
- The validator’s `HDM:FinalPageBox` and `HDM:PageOrientation` derivations align with converter behavior in `Old_Code/metrix_to_signa.py` (trim‑based box/orientation population).
- The validator derives `Layout/@SurfaceContentsBox` by using the Plate `Media/@Dimension` (width/height) anchored at `(-PlateCTM.e, -PlateCTM.f)` where `PlateCTM` is the `TransferCurveSet Name="Plate"` CTM from the referenced `TransferCurvePool`. Multiple Plate TransferCurveSet entries are accepted.
- The validator checks `ContentObject/@ClipBox` only when its width/height matches the derived TrimCTM+TrimSize box; it then verifies the trim box contains the clip bounds (low-confidence, low-priority).
- The validator skips the perfecting back-geometry mismatch check when the matched `BinderySignature` reports identical front/back scheme orientations (`HDM:FrontSchemePageOrientation` == `HDM:BackSchemePageOrientation`), since some fold schemes rotate pages the same on both sides.

**Provenance and Metadata**
- `HDM:SignaGenContext` appears to capture the Signa build, environment, and output profile used to generate the JDF. Confidence: High. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
- `HDM:SignaBLOB` points to the Signa data blob (`SignaData.sdf`) used by Cockpit to hand the binary back to Signa when a user chooses "edit layout." Confidence: High. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-SingleSide.jdf`.
- `HDM:SignaJDF` points to a file named `data.jdf` inside Signa's exported `<jobnumber>.jdf` bundle; it is not a separate layout JDF, just Signa's fixed filename for the ticket inside that folder. Confidence: High. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.
- `HDM:SignaJob` and `HDM:SignaJobPart` encode Signa job-part naming that maps to Signa UI or workflow parts. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.
- `HDM:ChangeInfo` likely represents a Signa internal change counter or revision marker. Confidence: Low. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
- `CustomerInfo` is emitted when a customer is assigned in Signa (e.g., `CustomerID`, `CustomerJobName`). Evidence: `Signa_Samples/MontageSample.jdf`.

**Layout Geometry and Imposition**
- `HDM:PaperRect` appears to define the paper rectangle within the sheet coordinate system for each side and matches the PDF `TrimBox` in print-ready outputs. Confidence: High. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SingleSheet-SingleSide.pdf`. Converter corroboration: `Old_Code/metrix_to_signa.py` (MediaOrigin/centering).
- `HDM:FinalPageBox` is the final page box for a placed page after imposition transforms. Confidence: High. Evidence: `Signa_Samples/SS-SingleSheet-Perfecting.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`. Converter corroboration: `Old_Code/metrix_to_signa.py` (sets FinalPageBox from trim data).
- `HDM:PageOrientation` matches the rotation applied to the page (0/90/180/270). Confidence: High. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`. Converter corroboration: `Old_Code/metrix_to_signa.py` (sets PageOrientation from trim data).
- Cockpit behavior note (Metrix → Prinect normalization): when ContentObject CTM is rotated 90/270 for mixed‑orientation layouts, setting `HDM:PageOrientation` to 90/270 can cause page assignment failures due to TrimBox size mismatches; forcing `PageOrientation="0"` for those rotated CTMs allowed assignments while previews/imposed PDFs remained correct. Stability: **Advisory** (single Metrix ganged sample, S2313).
- `HDM:AssemblyFB` identifies whether the placed page belongs to the front or back of the assembled product, independent of plate side. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
- `HDM:AssemblyIDs` links a placed page to a specific assembly or bindery signature identifier. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/GangedPostcards-Perfecting.jdf`.
- `HDM:OrigNameBySigna` preserves the original Signa naming for signatures and sheets even when `Name` changes. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-SingleSheet-Perfecting.jdf`.
- `PositionX`/`PositionY` appear on `ContentObject` in some Signa outputs (often `Center`); treat them as alignment hints that can be ignored when CTM/TrimCTM are present, since the base schema does not allow these attributes on `ContentObject`. Evidence: `Signa_Samples/Job_JDFs_with_descriptions/Q/Q2397 David Berman_Q2397 Tabs-data.jdf`.
- Montage layouts: `ContentObject` CTM/TrimCTM values drive explicit rotations (0/90/180/270) without fold schemes; block‑level `AssemblyIDs` (e.g., `Block_1_1`, `Block_3_3`) identify distinct montage placements. Evidence: `Signa_Samples/MontageSample.jdf`.

### Preview placement troubleshooting (low effort diagnostics)
- Symptom: CIP3 preview or plate visualization shows the sheet in the bottom-right while pages are placed correctly.
- Likely causes: missing or incorrect `TransferCurvePool` CTM entries (Paper/Plate), mismatched `HDM:PaperRect`, or missing `SurfaceContentsBox`/`SSi:MediaOrigin` offsets.
- Quick checks:
  - Confirm `TransferCurvePool` has `TransferCurveSet Name="Paper"` and `Name="Plate"` with CTM translation values that align with `HDM:PaperRect`/`SurfaceContentsBox`.
  - Verify `HDM:PaperRect` matches the intended paper origin; if `SSi:MediaOrigin` is missing, paper rectangles often need to be centered within `SurfaceContentsBox`.
  - Ensure `SurfaceContentsBox` matches plate `Media/@Dimension` and the plate CTM origin.

**Finishing and Block Construction**
- Stability: **Advisory** (downstream finishing context; not required for Cockpit import).
- `HDM:CIP3BlockTrf` encodes a CIP3 transform for cut blocks; it matches `BlockTrf` translated by the `HDM:PaperRect` lower-left when the job uses a single paper rectangle. Confidence: High. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-SingleSide.jdf`. Converter corroboration: `Old_Code/metrix_to_signa.py`.
- `HDM:CIP3FoldSheetIn_1` and `HDM:CIP3FoldSheetIn_2` appear to encode fold-sheet dimensions used by downstream folding logic. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- `HDM:ClosedFoldingSheetDimensions` and `HDM:OpenedFoldingSheetDimensions` appear to represent folded vs. unfolded sheet sizes for a block. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-SingleSide.jdf`.
- `HDM:OutputBlockName` appears to name the combined block produced by `HDM:CombiningParams`. Confidence: Medium. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- `HDM:StitchJobName` looks like a human-readable stitching label for assembly sections. Confidence: Low. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-Sheetwise.jdf`.
- CIP3-aligned interpretations (naming-based; not confirmed): `HDM:CIP3BlockTrf` likely corresponds to block placement transforms, `HDM:CIP3FoldSheetIn_1/2` to folded-sheet dimensions in orthogonal directions, and `HDM:CIP3BlockNames` to a block list used by downstream finishing automation. Confidence: Low. Evidence: `Signa_Samples/SingleSheet-WorkandTurn.jdf`, `Signa_Samples/GangedPostcards-WorkandTurn.jdf`, `Signa_Samples/PB-BookCover2Versions-Ganged-Sheetwise.jdf`.

**Color, Media, and RunList Hints**
- Stability: **Advisory** (observed behaviors and operator settings; do not treat as strict requirements).
- Process separations appear as spot-style placeholders: `B/C/M/Y` are process slots that can be remapped to actual inks (e.g., mapping `C` to Pantone 186), and `X/Z/U/V/S1...S8` provide additional spot slots for remapping; `HDM_DarkColor` likely marks the darkest separation so marks can print on that ink only. Confidence: Low (user observation; not vendor-documented). Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/Job_JDFs_with_descriptions/Q/Q1124 GGLS - BRP SEA CANeng_Q1124 tabs-data.jdf`.
- User-sourced rule (observation): Signa exposes process colors as remappable placeholders and uses `HDM_DarkColor` to target the darkest ink for specific marks.
- Toggling “Allow Spot Colors to BCMY” does not appear to change the JDF separation placeholder list; `B/C/M/Y/X/Z/U/V/S1...S8` remain present in the marks RunList with the same `HDM:IsMapRel` pattern, and no explicit flag is emitted in the JDF. Evidence: `Signa_Samples/NoSpotsToBCMYSample.jdf`, `Signa_Samples/SpotsToBCMYSample.jdf`.
- When the source PDF includes explicit spot colors, Signa emits the actual spot names in `ColorantControl/ColorantParams` and `ColorPool`, but the marks RunList still uses placeholder separations (`B/C/M/Y/X/Z/U/V/S1...S8`). Evidence: `Signa_Samples/SpotsSample.jdf`.
- Setting spot “Background”/“Dark” options in Signa reorders the `ColorantControl` and `ColorPool` entries (dark first in this sample) but does not change the marks RunList placeholder separations or add explicit `HDM_DarkColor` in the JDF. Evidence: `Signa_Samples/SpotsSampleBackgroundDark.jdf` (comparison vs `Signa_Samples/SpotsSample.jdf`).
- When the “Dark” selection is switched to a different spot color, the `ColorantControl`/`ColorPool` ordering flips to keep the selected dark color first, with no additional explicit flag in the JDF. Evidence: `Signa_Samples/SpotsSampleBackgroundDarkv2.jdf` (comparison vs `Signa_Samples/SpotsSampleBackgroundDark.jdf`).
- When the source PDF includes process CMYK plus spot colors, `ColorantOrder` expands to include CMYK (`Black`, `Cyan`, `Magenta`, `Yellow`) ahead of the spots, and `ColorPool` includes CMYK definitions alongside spot colors. Spot names remain listed in `ColorantParams`, while the marks RunList still uses placeholder separations. Evidence: `Signa_Samples/SpotsCMYKSample.jdf`.
- In the 3‑spot sample (no CMYK in the PDF), `ColorantOrder` follows the dark‑first ordering of the spot list (`PANTONE Yellow 012 C` first, then background spots). Evidence: `Signa_Samples/3SpotsCMYKSample.jdf`.
- Some samples show `X`/`Z` with `HDM:IsMapRel="true"` (and `HDM:SubType="Control"`), while others mark the same placeholders as `HDM:IsMapRel="false"`. User observation: this likely reflects whether the selected marks set (e.g., 6‑color bar with X/Z slots) is intended to be remapped by Prinect, even when the job doesn’t carry those separations. Evidence: `Signa_Samples/GangedPostcards-WorkandTurn.jdf`, `Signa_Samples/SingleSheet-SingleSide.jdf` (compare `X`/`Z` entries).
  ```xml
  <SeparationSpec Name="B"/>
  <SeparationSpec Name="C"/>
  <SeparationSpec Name="M"/>
  <SeparationSpec Name="Y"/>
  <SeparationSpec Name="HDM_DarkColor"/>
  ```
- `HDM:OFW="1.0"` on RunList is likely a workflow or output format tag used by Signa. Confidence: Low. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SingleSheet-WorkandTurn.jdf`. Converter corroboration: `Old_Code/metrix_to_signa.py` (Marks RunList normalization uses BCMY).
- `HDM:LeadingEdge` on plate media equals the plate height from `Media/@Dimension`. Confidence: High. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`. Converter corroboration: `Old_Code/metrix_to_signa.py`.

### Implementation evidence (converter behavior)
These notes come from `Old_Code/metrix_to_signa.py` (a working Metrix→Signa converter). They are not vendor documentation, but they reflect behaviors that successfully produce Prinect‑accepted JDFs.

- `HDM:LeadingEdge` is set to the plate `Media/@Dimension` height when missing.
- `HDM:PaperRect` is derived from `SSi:MediaOrigin` when available; otherwise it is centered within `SurfaceContentsBox`.
- `HDM:CIP3BlockTrf` is emitted as a translation transform for cut blocks; the translation is computed from the sheet origin used for `HDM:PaperRect`.
- Marks RunList normalization explicitly uses BCMY separation specs (`HDM:IsMapRel="true"`) to enable late spot‑color remapping. See also: `Old_Code/metrix_to_signa.py`.
- `ConventionalPrintingParams` is injected using SSi workstyle codes (e.g., Perfecting, WorkAndTurn), with partitions by Signature/Sheet/Side. See also: `Old_Code/metrix_to_signa.py`.
- `HDM:CalPapSort` and `HDM:CalPapSortBack` appear to be paper calibration or catalog identifiers. Confidence: Low. Evidence: `Signa_Samples/SingleSheet-Sheetwise.jdf`, `Signa_Samples/GangedPostcards-Perfecting.jdf`.
- `HDM:BlankPage` on RunList appears to flag blank logical pages in page pools. Confidence: Low. Evidence: `Signa_Samples/SS-SingleSheet-Perfecting.jdf`, `Signa_Samples/MultiSig-Perfecting.jdf`.
- `HDM:Purpose` appears in some layouts or resources and may indicate Signa workflow intent. Confidence: Low. Evidence: `Signa_Samples/SingleSheet-SingleSide.jdf`, `Signa_Samples/SS-BookText-Perfecting.jdf`.
- `HDM:RunlistIndex` appears in multi-job-part layouts and may map a sheet or job part to a RunList index. Confidence: Low. Evidence: `Signa_Samples/PB-BookText2Versions-Perfecting.jdf`, `Signa_Samples/PB-BookCover2Versions-Ganged-Sheetwise.jdf`.

## Quick conformance checklist (imposition-focused)
- JDF root is `Type="ProcessGroup"` with `Types` including `Imposition`.
- `ResourcePool/Layout` exists with `PartIDKeys="SignatureName SheetName Side"`.
- Layout hierarchy is Signature -> Sheet -> Side -> MarkObject/ContentObject.
- `SourceWorkStyle` on the sheet layout aligns with `WorkStyle` in `ConventionalPrintingParams`.
- `ConventionalPrintingParams` and `Component` side parts match the expected work style (front/back vs front-only).
- `ResourceLinkPool` links Layout, RunList, Media, and printing resources with `CombinedProcessIndex` aligned to `Types`.

## Glossary (project-local terms)
- Signature: A logical grouping of pages imposed together (JDF `SignatureName`).
- Sheet: The press sheet layout within a signature (JDF `SheetName`).
- Side: A plate side in the layout (`Front` or `Back`).
- WorkStyle: Signa's print-side model for the sheet (Perfecting, WorkAndBack, WorkAndTurn, WorkAndTumble, Simplex).

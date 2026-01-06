# JDF Elements and Attributes (Observed, with Descriptions)

This document is a **human-readable primer** describing JDF elements and attributes commonly observed in **Signa-produced JDFs** (and some Metrix samples). It is not a restatement of the CIP4 specification; instead, it explains **what these elements mean in practice**, how Signa uses them, and what a consumer should reasonably infer.

The intent is to give tooling authors and human readers enough semantic context to interpret Signa JDFs without needing deep prior JDF or print-industry experience.

---

## AmountPool
Container for quantity information used by resource links (e.g. number of sheets, blocks, or products).

---

## Assembly
Represents a logical assembly of blocks or sections used during finishing (folding, gathering, stitching, etc.). In Signa, this often maps to a **bindery-level construct** rather than a physical object.

---

## AssemblyLink
Links an `Assembly` resource to a process step via `CombinedProcessIndex`.

---

## AssemblySection
Defines a section within an assembly, typically corresponding to a bindery signature or folded block. Frequently used to associate block names, sheet names, and stitching context.

---

## AuditPool
Holds audit metadata such as creation records. Signa uses this primarily for provenance, not logic.

---

## BinderySignature
Defines how pages are grouped and ordered for folding.

- `BinderySignatureType="Fold"` indicates a folding-based signature.
- `BinderySignatureType="Grid"` indicates a montage (no folding semantics, observed in montage samples).

This element is central to understanding whether a layout carries **folding meaning** or is purely positional.

---

## BinderySignatureLink / BinderySignatureRef
Used to associate bindery signatures with other resources (e.g. stripping, folding, components).

---

## CalPapSort / CalPapSortBack
Signa-specific references to calibrated paper sorting profiles for front and back sides. These affect press calibration, not geometry.

---

## CollectingParams
Describes collecting behavior prior to stitching or binding. Presence usually implies saddle stitching or multi-section gathering.

---

## Color / ColorPool / ColorPoolRef
Defines available colors and inks. In Signa JDFs, this is often minimal and uses placeholder separations (CMYK, spot slots).

---

## ColorControlStrip
Describes a printed color control strip (e.g. color bars). Geometry is advisory; the actual marks are rendered into a marks PDF.

---

## ColorantControl / ColorantControlLink
Associates color handling parameters with specific sheets, sides, or signatures.

---

## CombiningParams
Describes how multiple blocks are combined into a single output block before or after cutting/folding. Often used in ganged or multi-up jobs.

---

## Comment
Free-form metadata comments. Signa uses these heavily for CIP3-style administrative fields.

---

## Component
Represents an output of a process step (sheet, block, or final product).

Key concepts:
- `ComponentType` distinguishes sheets, blocks, and final products.
- Folding dimensions may appear here even if folding is implicit.
- `AssemblyIDs` often link components back to bindery logic.

---

## ComponentLink
Connects components to process steps and indicates which operations produce them.

---

## ContentObject
One of the **most important elements** in a Signa JDF.

Represents a single placed page on a sheet side.

Key meanings:
- `CTM` and `TrimCTM` define placement and rotation.
- `TrimSize` defines the page size.
- `FinalPageBox` describes the page’s final bounding box after transforms.
- `PageOrientation` expresses the page’s orientation in the final product.
- `AssemblyFB` indicates whether the page belongs to the front or back of the assembled product (independent of plate side).

---

## ConventionalPrintingParams
Defines how the imposition maps to printing.

- `WorkStyle` is critical (Perfecting, WorkAndBack, WorkAndTurn, etc.).
- Often partitioned by signature, sheet, and side.

This resource bridges imposition geometry and press behavior.

---

## CoverApplicationParams
Describes cover application behavior in perfect-bound jobs. Rarely detailed in Signa exports.

---

## Created
Audit record indicating when and by what software the JDF was generated.

---

## CustomerInfo
Identifies customer metadata associated with the job. Informational only.

---

## CutBlock
Defines a block resulting from cutting operations, including transformations from sheet to block coordinates.

---

## CuttingParams
Describes cutting operations applied to sheets or blocks.

---

## DeliveryIntent / DropIntent / DropItemIntent
Logistics-related intent metadata. These are usually placeholders in Signa JDFs.

---

## FileSpec
Points to an external file (PDF, marks file, etc.). URLs are critical; filenames are not.

---

## Fold
Describes an individual fold operation when folding is explicitly modeled.

---

## FoldingParams
Defines folding behavior.

Important notes:
- `FoldCatalog` names the folding scheme.
- `NoOp="true"` indicates folding is *not* applied in Signa, even if the job will be folded later.
- Fold dimensions may be absent even when folding exists conceptually.

---

## GatheringParams
Describes gathering behavior prior to stitching or binding.

---

## JDF (root)
The job container.

Key attributes:
- `Type="ProcessGroup"` in Signa exports.
- `Types` defines the ordered process chain.
- `Version` and `MaxVersion` may not align with schema purity.
- `CombinedProcessIndex` and resource links define the process chain wiring (see `ResourceLinkPool`).

---

## Namespaces and Prefixes
Signa uses vendor extensions in the `HDM:` namespace alongside CIP4 elements.

- CIP4 elements carry the core JDF structure and are broadly portable.
- `HDM:` elements/attributes capture Signa-specific semantics and should generally be preserved even if not fully understood.

---

## IDs, References, and Linking
Most objects are connected by `ID`/`rRef` pairs across pools.

- Resources live in `ResourcePool` and are linked by `ResourceLinkPool` entries.
- Inline references (`MediaRef`, `BinderySignatureRef`, etc.) connect nested resources.
- Missing or mismatched `rRef` values is a common cause of consumer failures.

---

## Layout
Defines the imposition geometry.

Nested structure:
- Signature level
- Sheet level
- Side level

Key attributes:
- `SurfaceContentsBox` defines the usable sheet area.
- `PaperRect` defines the paper rectangle.
- `SourceWorkStyle` reflects intended press behavior.

---

## LayoutElement
Connects a layout or runlist to a file or placeholder.

---

## MarkObject / RegisterMark
Define mark intent (side guides, folding marks, etc.). The actual marks live in a separate marks PDF.

---

## Media
Defines paper and plate media.

- `Dimension` is in points (observed in samples).
- Plate media may include `LeadingEdge`.
- Paper media may include weight and thickness.

---

## PageData / PageList
Used to index pages within a runlist.

---

## Part / PartAmount
Partitioning helpers used to scope resources by signature, sheet, or bindery signature.

---

## PartIDKeys and Partitions
Many resources are partitioned by `PartIDKeys` so the same resource type can be scoped per signature, sheet, or side.

Common keys:
- `SignatureName`
- `SheetName`
- `Side`
- `BinderySignatureName`

---

## Position
Defines relative placement boxes, typically in stripping contexts.

---

## ResourcePool / ResourceLinkPool
Contain resources and their connections to process steps. `CombinedProcessIndex` ties everything together.

---

## CombinedProcessIndex
Indexes the process steps listed in the root `Types` attribute.

- Each `ResourceLink` can apply to one or more process steps using this index.
- This is the primary way Signa maps resources to process stages.

---

## RunList
Maps source PDFs, marks PDFs, and page pools into the job.

Key points:
- `ProcessUsage="Document"` vs `"Marks"` is critical.
- Page counts do *not* need to match across runlists.
- PagePool is optional and Signa-specific; it is often absent in third-party JDFs.

---

## Units and Coordinate Space
Signa samples use point-based coordinates for geometry and CTMs.

- `CTM`/`TrimCTM` live in the JDF coordinate space.
- `ClipBox`, `FinalPageBox`, and `PaperRect` align with that same coordinate system.
- When in doubt, derive values from geometry rather than assuming fixed units.

---

## SeparationSpec
Defines ink/separation placeholders. Names are often abstract slots, not real ink names.

---

## SignaBLOB / SignaJDF / SignaGenContext
Heidelberg-specific metadata linking the JDF back to Signa’s internal state, build version, and environment.

These should be preserved verbatim.

---

## SpinePreparationParams / SpineTapingParams
Indicate perfect binding operations. Presence implies a flat-spine binding model.

---

## StitchingParams
Defines saddle stitching parameters (staple count, placement, etc.).

---

## StrippingParams
Defines how signatures or blocks are positioned on the press sheet. Critical for ganging and multi-up layouts.

---

## TransferCurvePool / TransferCurveSet
Define tone reproduction curves; Signa sometimes attaches CTM metadata for paper/plate alignment. Often referenced indirectly via CTMs.

---

## TrimmingParams
Describes trimming operations and final product dimensions.

---

## Final Note

This document describes **observed Signa behavior**, not idealized JDF usage. When in doubt, trust the geometry and work style semantics over schema purity.

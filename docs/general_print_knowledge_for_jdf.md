## General Print and Imposition Knowledge (Context for JDF Consumers)

Related primers:
- `docs/Signa-JDF-Dialect.md`
- `docs/jdf-elements-attributes.md`

This document captures **print‑industry concepts and assumptions** that are often implicit in imposition systems like Signa, but are not fully expressed (or sometimes not expressible) in JDF alone. These concepts are critical for correctly interpreting layouts, work styles, folding schemes, and finishing intent.

It is intended to complement technical JDF dialect documentation by providing the **domain knowledge** that human planners, press operators, and bindery staff rely on implicitly.

---

## Why This Exists

JDF is a *descriptive* format, not a prescriptive one.

A JDF can be syntactically valid and still represent a job that:
- cannot be printed,
- cannot be bound,
- or cannot be finished as described.

Systems like Signa assume that consumers already understand the physical realities of printing. This document makes those assumptions explicit.

---

## Binding Methods

### Perfect Binding

Perfect binding produces a book block with a **flat spine**, created by milling or notching folded signatures and gluing them into a cover.

**Key characteristics:**
- Pages do **not nest**; all pages terminate at the spine edge.
- Spine width depends on **page count × paper thickness**.
- The cover is typically a **separate product part** from the text.
- Covers are often **simplex** or **work‑and‑turn**, even when the text is perfecting.

**Practical implications:**
- Page count must be divisible by **2**, not necessarily 4.
- Folding schemes must produce a **common spine edge**.
- Some folding grids are mathematically valid but **physically useless** for perfect binding.

**JDF implications:**
- There is no explicit “spine” flag in JDF.
- Perfect binding is inferred from finishing `Types` such as `SpinePreparation` and `SpineTaping`.
- Covers frequently omit folding metadata entirely even though they are part of a perfect‑bound product.

---

### Saddle Stitching

Saddle stitching creates a booklet by nesting folded sheets and stapling through the fold.

**Key characteristics:**
- Pages must **nest** around a central fold.
- Page count must be divisible by **4**.
- All pages share a **single final fold line** (the spine).
- Inner pages experience **creep**. This is horizontal displacement away from the spine to keep the face margins consistent after trimming.
- The outermost spread (often the cover) does not creep because nothing wraps around it.

**Practical implications:**
- Not all folds that produce 4+ pages are saddle‑stitch capable.
- The folding sequence must include a **final parallel fold**.

**JDF implications:**
- Saddle stitching is inferred from finishing steps like `Collecting` and `Stitching`.
- JDF does not encode nesting; it is assumed.
- A fold catalog may be valid but still produce a non‑stitchable layout.

---

## Folding Schemes: Physical Meaning

### Folding Is Not Just Page Count

A folding scheme defines:
- fold **order**,
- fold **direction** (parallel vs cross),
- page **orientation changes**,
- and whether a **spine edge** is produced.

Examples:
- A `3×1` fold produces 6 pages but **cannot form a book**.
- A `3×2` fold *can* form a spine if the `×2` is the final parallel fold.
- A `×6` parallel fold is mechanically unrealistic.

**Rule of thumb:**
> A book‑capable folding grid must contain **at least one power‑of‑two dimension** applied as the final parallel fold.

This is a physical rule, not a JDF rule.

---

## Montage vs Fold‑Mode Imposition

Signa distinguishes between two fundamentally different layout models:

### Montage Layouts
- Explicit page placement
- `BinderySignatureType="Grid"`
- Folding metadata absent or `NoOp="true"`
- Used for flat work, postcards, ganging, or external folding

### Fold‑Mode Layouts
- `BinderySignatureType="Fold"`
- `FoldCatalog` present
- Folding semantics are meaningful to downstream finishing

**Important:**
A montage layout may look identical on paper to a folded layout, but **does not carry the same meaning** for bindery automation.

---

## Work Styles (Press Logic)

Work styles describe **plate reuse**, not just sidedness.

### Common Work Styles
- Simplex
- Perfecting
- Work‑and‑Back (Sheetwise)
- Work‑and‑Turn
- Work‑and‑Tumble

Key distinctions:
- Work‑and‑Turn and Work‑and‑Tumble often have **only one logical side**.
- Front/back product faces are tracked independently from plate sides.

**JDF implications:**
- `Side="Back"` may not exist even when the product is double‑sided.
- Orientation and CTM carry meaning instead.
- Naive consumers may misinterpret these jobs as simplex.

---

## Page Orientation vs Placement Rotation

These are related but distinct concepts:

- **Page orientation** describes the page in the final product.
- **Placement rotation** describes how it sits on the sheet.

Examples:
- Perfecting often mirrors back‑side pages.
- Sheetwise often does not.
- Some fold schemes rotate pages 180° on *both* sides.

**Conclusion:**
Rotation values alone are insufficient to infer work style or binding.

---

## JDF Is Descriptive, Not Prescriptive

JDF describes **intent**, not feasibility.

Consequences:
- Physically impossible jobs can be valid JDF.
- Vendor extensions encode assumptions without documentation.
- Schema validation is necessary but never sufficient.

This document exists to record the **assumptions Signa makes but never states**.

---

## Suggested Use

- Pair this document with JDF dialect or schema documentation.
- Use it to guide validators, heuristics, and sanity checks.
- Treat it as living documentation; print logic evolves slower than software, but it does evolve.

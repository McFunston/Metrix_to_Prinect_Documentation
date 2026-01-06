#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
metrix_to_signa.py

Transform a Metrix imposition JDF (+ MXML) into a Signa/Prinect‑friendly JDF.

What this does (per spec agreed with Mica):
  • Paper media: create/reuse partitioned Media(@MediaType="Paper") by SignatureName/SheetName; set Dimension from MXML StockSheet (points),
    set GrainDirection (ShortEdge/LongEdge), optional Weight (gsm via lb→gsm heuristic) and Thickness (microns from inch caliper), with safe defaults.
  • Plate media: create/reuse partitioned Media(@MediaType="Plate"); set Dimension from JDF Surface/@SSi:Dimension; hard‑error if missing.
  • Page labels: write ContentObject/@DescriptiveName
      - mixed/ganged flats: from PageList/PageData ranges → base-(Ord+1) (flip‑proof)
      - single‑product books: folio from MXML Page[@Folio] by position (Ord+1)
      - multi‑product books: "<ProductDescription>_<Folio>" by position
      - mixed jobs: postcard rule where PageData has names, otherwise (multi)book folio rule
    Hard‑error on any Ord coverage mismatch.
  • Marks RunList (to enable "Allow spot colors to BCMY"):
      - single partitioned RunList with PartIDKeys="SignatureName SheetName Side" and HDM:OFW="1.0"
      - leaf per (Sig,Sheet,Side) with LayoutElement/FileSpec (URL from existing JDF) + four SeparationSpec Name=B/C/M/Y with HDM:IsMapRel="true"
      - RunListLink ProcessUsage="Marks" CombinedProcessIndex="0"
      - Hard‑error if any leaf missing FileSpec or HDM ns missing.
  • ColorantControl: ensure minimal unpartitioned r_Colorants (ProcessColorModel=DeviceCMYK; ColorantOrder: Black) and link with CombinedProcessIndex="1".
  • CombinedProcessIndex normalization:
      - RunListLink ProcessUsage Document/PagePool/Marks → CPI 0
      - ColorantControlLink + ConventionalPrintingParamsLink → CPI 1
      - MediaLink (Paper & Plate) → CPI "1 2"
  • Fail‑fast validators: contiguous ContentObject Ord set; label coverage exact; required namespaces present; required media present.

CLI:
  python metrix_to_signa.py JOB INPUT_DIR OUTPUT_DIR \
      [--validate-only] [--labels auto|postcards|book|multiproduct] \
      [--no-paper] [--no-plate] [--no-marks] [--verbosity info|debug]
    - JOB refers to the base filename without extension (JOB.jdf / JOB.mxml)
    - OUTPUT_DIR will receive Data.jdf (mirrors jdf_to_prinect_fixer CLI)

Logs are ASCII only, with prefixes: OK:, WARN:, ERROR:
Writes an optional OUT.summary.txt with human-readable (4-dec inch) sheet/plate sizes and chosen labeling mode.

Python 3.8+, requires lxml.
"""
from __future__ import annotations
import argparse
import copy
import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

from lxml import etree

# ------------------------------ Namespaces ------------------------------
NS_JDF = "http://www.CIP4.org/JDFSchema_1_1"
NS_HDM = "www.heidelberg.com/schema/HDM"
NS_SSI = "http://www.creo.com/SSI/JDFExtensions.xsd"
NS_MXML = "http://www.lithotechnics.com"
NS = {"jdf": NS_JDF, "HDM": NS_HDM, "SSi": NS_SSI, "m": NS_MXML}

# ------------------------------ Constants ------------------------------
PAPER_MEDIA_ID = "r_Paper_Metrix"
PLATE_MEDIA_ID = "r_Plate_Metrix"
MARKS_RUNLIST_ID = "r_Marks_Metrix"
COLORANTS_ID = "r_Colorants"

DEFAULT_WEIGHT_GSM = 135  # per spec if unknown
DEFAULT_THICKNESS_MICRON = 120  # per spec if unknown (~0.12 mm)

# gsm per lb factors by grade family
LB_TO_GSM_FACTORS = {
    "TEXT": 1.480,
    "COVER": 2.708,
    "INDEX": 1.810,
    "TAG": 1.629,
    "BRISTOL": 2.197,
    "BOND": 3.760,
}
GRADE_KEYWORDS = {
    "TEXT": ["text", "book", "offset"],
    "COVER": ["cover", "c1s", "c2s"],
    "INDEX": ["index"],
    "TAG": ["tag"],
    "BRISTOL": ["bristol"],
    "BOND": ["bond", "writing"],
}
COATED_HINT_WORDS = ["gloss", "silk", "satin", "matte", "c2s", "c1s", "coated"]

# Map Metrix/SSi WorkStyle codes to JDF WorkStyle values
SSI_TO_JDF_WORKSTYLE = {
    "PE": "Perfecting",
    "TN": "WorkAndTurn",
    "TO": "WorkAndTumble",
    "SH": "Sheetwise",
    "SF": "Simplex",
    "SS": "Simplex",
    "SW": "Sheetwise",
}

# ------------------------------ Utilities ------------------------------

def log(level: str, msg: str) -> None:
    # ASCII-only logging
    sys.stdout.write(f"{level.upper()}: {msg}\n")
    sys.stdout.flush()


def inches_to_points(v: float) -> float:
    return v * 72.0


def points_to_inches(v: float) -> float:
    return v / 72.0


def inch_to_microns(v: float) -> int:
    # 1 inch = 25.4 mm = 25400 microns
    return int(round(v * 25400.0))


def round4_in(x: float) -> str:
    return f"{x:.4f}"


def parse_float(s: Optional[str]) -> Optional[float]:
    if s is None:
        return None
    try:
        return float(s)
    except Exception:
        return None


def require(cond: bool, message: str) -> None:
    if not cond:
        log("ERROR", message)
        sys.exit(1)


def read_xml(path: str) -> etree._ElementTree:
    with open(path, "rb") as f:
        return etree.parse(f)


def write_xml(tree: etree._ElementTree, path: str) -> None:
    data = etree.tostring(
        tree,
        pretty_print=True,
        xml_declaration=True,
        encoding="UTF-8",
        standalone=False,
    )
    with open(path, "wb") as f:
        f.write(data)

# ------------------------------ ConventionalPrintingParams (WorkStyle) ------------------------------

def _unique_id(existing: set, prefix: str = "r_ConvPrint_") -> str:
    n = 1
    while True:
        cand = f"{prefix}{n:03d}"
        if cand not in existing:
            return cand
        n += 1


def inject_workstyle_from_ssi(root: etree._Element) -> bool:
    """Read SSi:WorkStyle from Sheet nodes and inject a partitioned
    ConventionalPrintingParams resource, linking it at CPI 1.

    - Partitions by SignatureName / SheetName / Side (Front/Back)
    - Sets top-level WorkStyle only if uniform across all sheets
    Returns True if a resource was created, False if no SSi:WorkStyle found.
    """
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    require(pool is not None and rlp is not None, "Missing ResourcePool/ResourceLinkPool in JDF")

    # Collect per-sheet WorkStyle
    sig_map: dict = {}
    for sheet in root.xpath(".//jdf:Sheet", namespaces=NS):
        code = sheet.get(f"{{{NS_SSI}}}WorkStyle")
        if not code:
            continue
        ws = SSI_TO_JDF_WORKSTYLE.get(code)
        if not ws:
            continue
        # Ascend to the parent Signature
        parent = sheet.getparent()
        while parent is not None and parent.tag != f"{{{NS_JDF}}}Signature":
            parent = parent.getparent()
        if parent is None:
            continue
        sig_name = parent.get("Name") or parent.get("SignatureName")
        sheet_name = sheet.get("Name") or sheet.get("SheetName")
        if not sig_name or not sheet_name:
            continue
        sig_map.setdefault(sig_name, {})[sheet_name] = ws

    if not sig_map:
        return False

    existing_ids = {el.get("ID") for el in pool.xpath(".//*[@ID]", namespaces=NS) if el.get("ID")}
    conv_id = _unique_id(existing_ids)

    conv = etree.Element(f"{{{NS_JDF}}}ConventionalPrintingParams")
    conv.set("ID", conv_id)
    conv.set("Class", "Parameter")
    conv.set("Status", "Available")
    conv.set("PrintingType", "SheetFed")
    conv.set("PartIDKeys", "SignatureName SheetName Side")

    # Top-level WorkStyle only if uniform
    all_ws = {ws for m in sig_map.values() for ws in m.values()}
    if len(all_ws) == 1:
        conv.set("WorkStyle", next(iter(all_ws)))

    # Partitions: Signature -> Sheet -> Front/Back
    for sig_name, sheets in sig_map.items():
        p_sig = etree.SubElement(conv, f"{{{NS_JDF}}}ConventionalPrintingParams")
        p_sig.set("SignatureName", sig_name)
        for sheet_name, ws in sheets.items():
            p_sheet = etree.SubElement(p_sig, f"{{{NS_JDF}}}ConventionalPrintingParams")
            p_sheet.set("SheetName", sheet_name)
            if len(all_ws) != 1:
                p_sheet.set("WorkStyle", ws)
            etree.SubElement(p_sheet, f"{{{NS_JDF}}}ConventionalPrintingParams").set("Side", "Front")
            etree.SubElement(p_sheet, f"{{{NS_JDF}}}ConventionalPrintingParams").set("Side", "Back")

    pool.append(conv)

    link = etree.SubElement(rlp, f"{{{NS_JDF}}}ConventionalPrintingParamsLink")
    link.set("Usage", "Input")
    link.set("CombinedProcessIndex", "1")
    link.set("rRef", conv_id)
    return True

# ------------------------------ MXML parsing ------------------------------

class StockSheet:
    def __init__(self, width_in: float, height_in: float, grain_long: Optional[bool],
                 brand: Optional[str], descriptive: Optional[str], manufacturer: Optional[str],
                 grade: Optional[str], basis_weight_lb: Optional[float], caliper_in: Optional[float]):
        self.width_in = width_in
        self.height_in = height_in
        self.grain_long = grain_long
        # Stock metadata (prefer values from parent Stock)
        self.brand = brand
        self.descriptive = descriptive or brand
        self.manufacturer = manufacturer
        self.grade = grade
        # Physicals
        self.basis_weight_lb = basis_weight_lb
        self.caliper_in = caliper_in

    @property
    def dim_pt(self) -> Tuple[float, float]:
        return (inches_to_points(self.width_in), inches_to_points(self.height_in))

    def grain_attr(self) -> Optional[str]:
        if self.grain_long is None:
            return None
        return "LongEdge" if self.grain_long else "ShortEdge"

    def weight_gsm(self) -> Optional[int]:
        if self.basis_weight_lb is None:
            return None
        # Determine grade family for lb→gsm: use known family names only.
        fam: Optional[str] = None
        if isinstance(self.grade, str):
            g = self.grade.strip().upper()
            if g in LB_TO_GSM_FACTORS:
                fam = g
        if fam is None:
            fam = infer_grade_from_name(self.descriptive) or infer_grade_from_name(self.brand)
        factor = LB_TO_GSM_FACTORS.get(fam) if fam else None
        if factor is None:
            return None
        return int(round(self.basis_weight_lb * factor))

    def thickness_microns(self) -> Optional[int]:
        if self.caliper_in is None:
            return None
        return inch_to_microns(self.caliper_in)


def infer_grade_from_name(name: Optional[str]) -> Optional[str]:
    if not name:
        return None
    s = name.lower()
    for grade, words in GRADE_KEYWORDS.items():
        for w in words:
            if w in s:
                return grade
    return None


def mxml_read_layout_stock_sequence(mxml: etree._ElementTree) -> List[StockSheet]:
    """Return StockSheet sequence in *layout order*.
    We expect /MetrixXML/.../Layout elements that reference StockSheet via StockSheetRef/@rRef.
    Fallback: if no explicit Layout order, read all StockSheet nodes in doc order.
    """
    root = mxml.getroot()
    ns = {"m": NS_MXML}

    # Map StockSheet @ID -> StockSheet instance (augmented with parent Stock metadata)
    ss_nodes = root.xpath(".//m:StockSheet", namespaces=ns)
    id_to_ss: Dict[str, StockSheet] = {}
    for ss in ss_nodes:
        sid = ss.get("ID") or ss.get("Id") or ss.get("IdRef")
        if not sid:
            # skip nameless
            continue
        # Dimensions (inches)
        w = parse_float(ss.get("Width"))
        h = parse_float(ss.get("Height"))
        # Grain (best-effort)
        # 1) boolean-style flags that explicitly mean long/short grain
        grain_long = None
        for key in ("LongGrain", "GrainLong", "IsLongGrain"):
            val = ss.get(key)
            if val is None:
                continue
            val_l = val.lower()
            if val_l in ("true", "yes", "1"):
                grain_long = True
                break
            if val_l in ("false", "no", "0"):
                grain_long = False
                break
        # 2) textual orientation: Grain="horizontal"/"vertical"
        if grain_long is None:
            gtxt = ss.get("Grain")
            if gtxt is not None and w is not None and h is not None:
                gl = gtxt.lower()
                if gl in ("horizontal", "horiz", "h"):
                    grain_long = (w >= h)
                elif gl in ("vertical", "vert", "v"):
                    grain_long = (h >= w)
        # Parent Stock metadata (preferred source for brand/description/manufacturer/grade/weight/thickness)
        stock_node = ss.getparent()
        # climb until Stock or root
        while stock_node is not None and stock_node.tag != f"{{{NS_MXML}}}Stock":
            stock_node = stock_node.getparent()
        brand = None
        descriptive = None
        manufacturer = None
        grade = None
        basis = None
        stock_thick_in = None
        if stock_node is not None:
            brand = stock_node.get("Name") or stock_node.get("Brand") or stock_node.get("Description")
            descriptive = stock_node.get("Description") or brand
            manufacturer = stock_node.get("Vendor") or stock_node.get("Manufacturer")
            grade = stock_node.get("Grade")
            # Weight + units
            basis = parse_float(stock_node.get("Weight"))
            wu = (stock_node.get("WeightUnit") or "").lower()
            if wu and basis is not None and wu not in ("lb", "lbs", "pound", "pounds"):
                # Not pounds → ignore for lb→gsm logic; we'll try to infer from names
                basis = None
            stock_thick_in = parse_float(stock_node.get("Thickness"))

        # Fallbacks from StockSheet if Stock missing
        if brand is None or descriptive is None or manufacturer is None or grade is None:
            ss_brand = ss.get("Brand") or ss.get("Name")
            ss_desc = ss.get("Description") or ss_brand
            brand = brand or ss_brand
            descriptive = descriptive or ss_desc
            manufacturer = manufacturer or ss.get("Vendor")
            grade = grade or ss.get("Grade")

        # Basis weight (lb) fallback: parse on StockSheet/human text if needed
        if basis is None:
            ss_weight = parse_float(ss.get("BasisWeight")) or parse_float(ss.get("Weight"))
            if ss_weight is not None:
                basis = ss_weight
            else:
                hay = " ".join(x for x in [descriptive, brand] if x)
                m = re.search(r"(\d+(?:\.\d+)?)\s*lb", hay, re.IGNORECASE)
                if m:
                    basis = float(m.group(1))
        # Caliper in inches
        caliper_in = None
        for key in ("Caliper", "CaliperInches", "ThicknessInches", "Thickness"):
            val = ss.get(key)
            if val is None:
                continue
            v = parse_float(val)
            if v is not None:
                caliper_in = v
                break
        if caliper_in is None and stock_thick_in is not None:
            caliper_in = stock_thick_in
        if w is None or h is None:
            continue
        id_to_ss[sid] = StockSheet(w, h, grain_long, brand, descriptive, manufacturer, grade, basis, caliper_in)

    # Collect layouts in document order
    layouts = root.xpath(".//m:Layout", namespaces=ns)
    sequence: List[StockSheet] = []
    for layout in layouts:
        ref = layout.find("m:StockSheetRef", namespaces=ns)
        if ref is None:
            continue
        rref = ref.get("rRef") or ref.get("Ref")
        if not rref:
            continue
        ss = id_to_ss.get(rref)
        if ss:
            sequence.append(ss)

    # Fallback if no layouts found
    if not sequence and id_to_ss:
        sequence = list(id_to_ss.values())

    return sequence


def mxml_read_folios(mxml: etree._ElementTree) -> Tuple[List[str], List[str]]:
    """Return (labels_single_product, labels_multi_product) derived from PagePools.
    Order is document order: for each Product, for each Page in its PagePool.
    labels_single_product = ["1", "2", ...]
    labels_multi_product = ["<Product>_1", "<Product>_2", ...]
    """
    root = mxml.getroot()
    ns = {"m": NS_MXML}

    products = root.xpath(".//m:Product", namespaces=ns)
    single: List[str] = []
    multi: List[str] = []
    for prod in products:
        desc = prod.get("Description") or prod.get("Name") or prod.get("ID") or "Product"
        pages = prod.xpath(".//m:PagePool/m:Page", namespaces=ns)
        for p in pages:
            fol = p.get("Folio") or p.get("FolioNumber") or p.get("Number")
            if fol is None:
                # Fallback to index+1 if no folio
                # (we will still hard-error on mismatch later if counts diverge)
                idx = len(single)
                fol = str(idx + 1)
            single.append(str(fol))
            multi.append(f"{desc}_{fol}")
    return single, multi

# ------------------------------ JDF helpers ------------------------------

def jdf_root(tree: etree._ElementTree) -> etree._Element:
    return tree.getroot()


def ensure_namespaces(root: etree._Element) -> None:
    # HDM must exist per spec; SSi should exist in Metrix JDF; default JDF ns must be CIP4.
    nsmap = root.nsmap
    require(nsmap.get(None) == NS_JDF, "Root JDF has unexpected namespace; expected CIP4 JDF 1.1")
    require("HDM" in nsmap and nsmap["HDM"] == NS_HDM, "Missing HDM namespace on JDF root; aborting")


def get_contentobject_ords(root: etree._Element) -> List[int]:
    ords: List[int] = []
    for co in root.xpath(".//jdf:ContentObject", namespaces=NS):
        o = co.get("Ord")
        if o is None:
            continue
        try:
            ords.append(int(o))
        except Exception:
            pass
    uniq = sorted(set(ords))
    return uniq


def validate_contiguous_ords(ords: List[int]) -> None:
    if not ords:
        require(False, "No ContentObject Ord values found; cannot label pages")
    expected = list(range(0, ords[-1] + 1))
    if ords != expected:
        require(False, f"ContentObject Ord sequence is not contiguous 0..{ords[-1]}")


def build_postcard_base_map_from_pagelist(root: etree._Element) -> Dict[int, str]:
    """Map Ord -> base name by expanding PageList/PageData ranges."""
    base_map: Dict[int, str] = {}
    for pd in root.xpath(".//jdf:PageData", namespaces=NS):
        base = pd.get("DescriptiveName")
        pidx = pd.get("PageIndex")
        if not base or not pidx:
            continue
        token = pidx.replace(" ", "")
        if "~" in token:
            parts = token.split("~", 1)
            try:
                start, end = int(parts[0]), int(parts[1])
                if start <= end:
                    for i in range(start, end + 1):
                        base_map[i] = base
            except Exception:
                continue
        else:
            try:
                i = int(token)
                base_map[i] = base
            except Exception:
                continue
    return base_map


def derive_label_mode(arg_mode: str, root_jdf: etree._Element, mxml: etree._ElementTree) -> str:
    if arg_mode != "auto":
        return arg_mode
    # Multi-product if >1 Product in MXML
    mroot = mxml.getroot()
    prods = mroot.xpath(".//m:Product", namespaces=NS)
    if len(prods) > 1:
        return "multiproduct"
    # Postcards if any PageData has a non-cover DescriptiveName
    for pd in root_jdf.xpath(".//jdf:PageData", namespaces=NS):
        name = (pd.get("DescriptiveName") or "").strip().lower()
        if name and not name.startswith("cover"):
            return "postcards"
    return "book"


def build_labels(root_jdf: etree._Element, mxml: etree._ElementTree, mode: str) -> Dict[int, str]:
    ords = get_contentobject_ords(root_jdf)
    validate_contiguous_ords(ords)

    labels: Dict[int, str] = {}

    # postcard mapping first (even in mixed jobs)
    base_map = build_postcard_base_map_from_pagelist(root_jdf)
    if base_map:
        for o in ords:
            if o in base_map:
                labels[o] = f"{base_map[o]}-{o+1}"

    single, multi = mxml_read_folios(mxml)
    # choose folio stream: single or multi-product
    folio_stream = multi if mode == "multiproduct" else single

    # fill the rest from folios
    require(len(folio_stream) >= (max(ords) + 1),
            "Folio list from MXML shorter than ContentObject count")

    for o in ords:
        if o in labels:
            continue
        labels[o] = str(folio_stream[o])

    # final coverage check
    if len(labels) != (max(ords) + 1):
        require(False, "Page label coverage mismatch: some Ords could not be labeled")

    return labels

# ------------------------------ Media (Paper/Plate) ------------------------------

def find_or_create_media(root: etree._Element, media_id: str, media_type: str) -> etree._Element:
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    require(pool is not None, "Missing ResourcePool in JDF")
    candidates = pool.xpath(f".//jdf:Media[@MediaType='{media_type}']", namespaces=NS)
    target = None
    for m in candidates:
        if m.get("PartIDKeys") == "SignatureName SheetName":
            target = m
            break
    if target is None:
        target = etree.SubElement(pool, f"{{{NS_JDF}}}Media")
        target.set("ID", media_id)
        target.set("Class", "Consumable")
        target.set("Status", "Available")
        target.set("MediaType", media_type)
        target.set("PartIDKeys", "SignatureName SheetName")
    else:
        # ensure ID
        if not target.get("ID"):
            target.set("ID", media_id)
        target.set("Class", "Consumable")
        target.set("Status", "Available")
        target.set("PartIDKeys", "SignatureName SheetName")
    return target


def ensure_media_link(root: etree._Element, media_id: str) -> None:
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    require(rlp is not None, "Missing ResourceLinkPool in JDF")
    link = None
    for l in rlp.xpath(".//jdf:MediaLink", namespaces=NS):
        if l.get("rRef") == media_id:
            link = l
            break
    if link is None:
        link = etree.SubElement(rlp, f"{{{NS_JDF}}}MediaLink")
        link.set("rRef", media_id)
    link.set("Usage", "Input")
    # Enforce CPI "1 2"
    link.set("CombinedProcessIndex", "1 2")


def ensure_media_refs(root: etree._Element, media_id: str) -> None:
    """Ensure a MediaRef is present on each Signature under ResourcePool/Layout.
    Some workflows (e.g., Signa preview) resolve sheet media via Signature-level refs.
    """
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    if pool is None:
        return
    layout = pool.find(".//jdf:Layout", namespaces=NS)
    if layout is None:
        return
    for sig in layout.xpath(".//jdf:Signature", namespaces=NS):
        has = False
        for ref in sig.xpath("./jdf:MediaRef", namespaces=NS):
            if ref.get("rRef") == media_id:
                has = True
                break
        if not has:
            etree.SubElement(sig, f"{{{NS_JDF}}}MediaRef").set("rRef", media_id)


def enumerate_sig_sheet_pairs(root: etree._Element) -> List[Tuple[str, str, etree._Element]]:
    """Return list of (SignatureName, SheetName, sheet_node) in document order."""
    result: List[Tuple[str, str, etree._Element]] = []
    for sig in root.xpath(".//jdf:Layout/jdf:Signature", namespaces=NS):
        sname = sig.get("Name") or sig.get("SignatureName") or "Signature"
        for sheet in sig.xpath("./jdf:Sheet", namespaces=NS):
            shname = sheet.get("Name") or sheet.get("SheetName") or "Sheet"
            result.append((sname, shname, sheet))
    require(result != [], "Could not enumerate Signature/Sheet pairs from JDF Layout")
    return result


def set_paper_media(root: etree._Element, stocks: List[StockSheet]) -> List[Tuple[str, str, Tuple[float, float], Optional[str], int, int, str]]:
    """Create/reuse Paper media partitions and set dimensions & attrs.
    Returns a list of rows for summary: (Sig, Sheet, (w_pt,h_pt), grain, gsm, microns, human_name)
    """
    media = find_or_create_media(root, PAPER_MEDIA_ID, "Paper")

    pairs = enumerate_sig_sheet_pairs(root)
    require(len(pairs) <= len(stocks), "More JDF sheets than MXML layouts; cannot map sizes")

    summary = []
    dim_set: set = set()
    for idx, (sig_name, sheet_name, sheet_node) in enumerate(pairs):
        ss = stocks[idx]
        # Ensure partition chain exists
        sig_part = None
        for child in media.xpath(f"./jdf:Media[@SignatureName='{sig_name}']", namespaces=NS):
            sig_part = child
            break
        if sig_part is None:
            sig_part = etree.SubElement(media, f"{{{NS_JDF}}}Media")
            sig_part.set("SignatureName", sig_name)
        leaf = None
        for child in sig_part.xpath(f"./jdf:Media[@SheetName='{sheet_name}']", namespaces=NS):
            leaf = child
            break
        if leaf is None:
            leaf = etree.SubElement(sig_part, f"{{{NS_JDF}}}Media")
            leaf.set("SheetName", sheet_name)
        # Prefer per-sheet paper size from SSi:Dimension on the first Surface
        surface = sheet_node.find("jdf:Surface", namespaces=NS)
        dim = surface_plate_dimension(surface) if surface is not None else None
        if dim is None:
            w_pt, h_pt = ss.dim_pt
        else:
            w_pt, h_pt = dim
        leaf.set("Dimension", f"{w_pt:.4f} {h_pt:.4f}")
        dim_set.add((round(w_pt, 4), round(h_pt, 4)))
        # Stock metadata: Brand, DescriptiveName, Manufacturer, Grade
        if ss.brand:
            leaf.set("Brand", ss.brand)
        if ss.descriptive:
            leaf.set("DescriptiveName", ss.descriptive)
        if ss.manufacturer is not None:
            leaf.set("Manufacturer", ss.manufacturer)
        if ss.grade is not None:
            leaf.set("Grade", ss.grade)
        leaf.set("MediaUnit", "Sheet")
        # Grain
        grain = ss.grain_attr()
        if grain:
            leaf.set("GrainDirection", grain)
        else:
            # leave unset if unknown
            pass
        # Weight & thickness
        gsm = ss.weight_gsm()
        if gsm is None:
            gsm = DEFAULT_WEIGHT_GSM
        leaf.set("Weight", str(gsm))
        mic = ss.thickness_microns()
        if mic is None:
            mic = DEFAULT_THICKNESS_MICRON
        leaf.set("Thickness", f"{mic}")
        human = ss.descriptive or ss.brand or ""
        summary.append((sig_name, sheet_name, (w_pt, h_pt), grain or "", gsm, mic, human))

    ensure_media_link(root, media.get("ID") or PAPER_MEDIA_ID)
    if len(dim_set) == 1 and not media.get("Dimension"):
        w_pt, h_pt = next(iter(dim_set))
        media.set("Dimension", f"{w_pt:.4f} {h_pt:.4f}")
    return summary


def surface_plate_dimension(surface: etree._Element) -> Optional[Tuple[float, float]]:
    dim = surface.get(f"{{{NS_SSI}}}Dimension") or surface.get("SSi:Dimension")
    if not dim:
        return None
    parts = str(dim).strip().split()
    if len(parts) != 2:
        return None
    try:
        return float(parts[0]), float(parts[1])
    except Exception:
        return None


def set_plate_media(root: etree._Element) -> List[Tuple[str, str, Tuple[float, float]]]:
    media = find_or_create_media(root, PLATE_MEDIA_ID, "Plate")

    pairs = enumerate_sig_sheet_pairs(root)
    summary = []
    dim_set: set = set()
    for (sig_name, sheet_name, sheet_node) in pairs:
        # Plate dimension = SurfaceContentsBox width/height (plate canvas),
        # fallback to SSi:Dimension only if SCB missing.
        scb = sheet_node.get("SurfaceContentsBox")
        if not scb:
            surf0 = sheet_node.find("jdf:Surface", namespaces=NS)
            scb = surf0.get("SurfaceContentsBox") if surf0 is not None else None
        w_pt = h_pt = None
        if scb:
            try:
                xs, ys, xe, ye = [float(x) for x in scb.strip().split()[:4]]
                w_pt = xe - xs
                h_pt = ye - ys
            except Exception:
                w_pt = h_pt = None
        if w_pt is None or h_pt is None:
            # fallback to SSi:Dimension on first surface (paper size)
            surface = sheet_node.find("jdf:Surface", namespaces=NS)
            require(surface is not None, f"Missing Surface under sheet (Signature='{sig_name}', Sheet='{sheet_name}')")
            dim = surface_plate_dimension(surface)
            require(dim is not None, f"Missing SSi:Dimension/SurfaceContentsBox (Signature='{sig_name}', Sheet='{sheet_name}')")
            w_pt, h_pt = dim
        # Ensure partition chain exists
        sig_part = None
        for child in media.xpath(f"./jdf:Media[@SignatureName='{sig_name}']", namespaces=NS):
            sig_part = child
            break
        if sig_part is None:
            sig_part = etree.SubElement(media, f"{{{NS_JDF}}}Media")
            sig_part.set("SignatureName", sig_name)
        leaf = None
        for child in sig_part.xpath(f"./jdf:Media[@SheetName='{sheet_name}']", namespaces=NS):
            leaf = child
            break
        if leaf is None:
            leaf = etree.SubElement(sig_part, f"{{{NS_JDF}}}Media")
            leaf.set("SheetName", sheet_name)
        leaf.set("Dimension", f"{w_pt:.4f} {h_pt:.4f}")
        dim_set.add((round(w_pt, 4), round(h_pt, 4)))
        summary.append((sig_name, sheet_name, (w_pt, h_pt)))

    ensure_media_link(root, media.get("ID") or PLATE_MEDIA_ID)
    if len(dim_set) == 1 and not media.get("Dimension"):
        w_pt, h_pt = next(iter(dim_set))
        media.set("Dimension", f"{w_pt:.4f} {h_pt:.4f}")
    return summary

# ------------------------------ Labels ------------------------------

def apply_labels(root: etree._Element, labels: Dict[int, str]) -> None:
    for co in root.xpath(".//jdf:ContentObject", namespaces=NS):
        o = co.get("Ord")
        if o is None:
            continue
        try:
            idx = int(o)
        except Exception:
            continue
        name = labels.get(idx)
        if name is None:
            require(False, f"Missing label for Ord={idx}")
        co.set("DescriptiveName", name)

# ------------------------------ Marks RunList (BCMY) ------------------------------

def find_marks_filespec_url(root: etree._Element) -> Optional[str]:
    # Prefer existing Marks RunList leaf if present; else any RunList FileSpec containing 'Marks'
    # else first RunList FileSpec (last resort)
    # 1) by Marks link
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    if rlp is not None:
        for link in rlp.xpath(".//jdf:RunListLink[@ProcessUsage='Marks']", namespaces=NS):
            rref = link.get("rRef")
            if not rref:
                continue
            rl = root.find(f".//jdf:RunList[@ID='{rref}']", namespaces=NS)
            if rl is None:
                continue
            fs = rl.find(".//jdf:LayoutElement/jdf:FileSpec", namespaces=NS)
            if fs is not None and fs.get("URL"):
                return fs.get("URL")
    # 2) any RunList FileSpec containing 'Marks'
    for fs in root.xpath(".//jdf:RunList//jdf:LayoutElement/jdf:FileSpec", namespaces=NS):
        url = fs.get("URL") or ""
        if "marks" in url.lower():
            return url
    # 3) fallback to first FileSpec under any RunList
    fs = root.find(".//jdf:RunList//jdf:LayoutElement/jdf:FileSpec", namespaces=NS)
    if fs is not None and fs.get("URL"):
        return fs.get("URL")
    return None

def ensure_marks_runlist(root: etree._Element) -> None:
    # Ensure partitioned Marks RunList exists and includes BCMY map-rel seps
    # while preserving an existing Marks RunList structure (attributes and extras).
    url = find_marks_filespec_url(root)
    require(url is not None, "Missing FileSpec URL for marks RunList")

    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    require(pool is not None and rlp is not None, "Missing ResourcePool/ResourceLinkPool in JDF")

    # Prefer an existing Marks RunList if linked; else create a new canonical one
    link = next(iter(rlp.xpath(".//jdf:RunListLink[@ProcessUsage='Marks']", namespaces=NS)), None)
    rl_top = None
    if link is not None and link.get("rRef"):
        rl_top = pool.find(f".//jdf:RunList[@ID='{link.get('rRef')}']", namespaces=NS)
    if rl_top is None:
        rl_top = etree.SubElement(pool, f"{{{NS_JDF}}}RunList")
        rl_top.set("Class", "Parameter")
        rl_top.set("Status", "Available")
        rl_top.set("ID", MARKS_RUNLIST_ID)
        rl_top.set("PartIDKeys", "SignatureName SheetName Side")
        rl_top.set(f"{{{NS_HDM}}}OFW", "1.0")
        # NPage will be set after we count leaf sides
        # Link it
        l = etree.SubElement(rlp, f"{{{NS_JDF}}}RunListLink")
        l.set("Usage", "Input")
        l.set("ProcessUsage", "Marks")
        l.set("CombinedProcessIndex", "0")
        l.set("rRef", rl_top.get("ID"))
    else:
        # Ensure required attributes exist
        rl_top.set("Class", rl_top.get("Class") or "Parameter")
        rl_top.set("Status", rl_top.get("Status") or "Available")
        rl_top.set("PartIDKeys", "SignatureName SheetName Side")
        rl_top.set(f"{{{NS_HDM}}}OFW", rl_top.get(f"{{{NS_HDM}}}OFW") or "1.0")
        # We will normalize NPage after building leaves
        link.set("CombinedProcessIndex", "0")
        link.set("Usage", "Input")
        link.set("ProcessUsage", "Marks")

    # Ensure a top-level LayoutElement FileSpec exists
    if rl_top.find("jdf:LayoutElement/jdf:FileSpec", namespaces=NS) is None:
        layout_stub = etree.SubElement(rl_top, f"{{{NS_JDF}}}LayoutElement")
        layout_stub.set("Class", "Parameter")
        fs_stub = etree.SubElement(layout_stub, f"{{{NS_JDF}}}FileSpec")
        fs_stub.set("Class", "Parameter")
        fs_stub.set("MimeType", "application/pdf")
        fs_stub.set("URL", url)

    # Enumerate (Sig, Sheet, Side) and ensure leaf RunList + LayoutElement
    pairs = enumerate_sig_sheet_pairs(root)
    # Build a flat list of side contexts to assign page windows deterministically
    side_contexts: List[Tuple[str, str, str]] = []
    for (sig_name, sheet_name, sheet_node) in pairs:
        # Sides present
        sides = []
        for surf in sheet_node.xpath("./jdf:Surface", namespaces=NS):
            side = (surf.get("Side") or "Front")
            side = side if side in ("Front", "Back") else "Front"
            if side not in sides:
                sides.append(side)
        if not sides:
            sides = ["Front"]
        for side in sides:
            side_contexts.append((sig_name, sheet_name, side))

    # Now ensure partition chains and set leaf paging attributes
    for index, (sig_name, sheet_name, side) in enumerate(side_contexts):
        # Ensure partition chain exists
        def ensure_child(parent, tag, key_attr, key_val):
            for n in parent.xpath(f"./jdf:{tag}[@{key_attr}='{key_val}']", namespaces=NS):
                return n
            n = etree.SubElement(parent, f"{{{NS_JDF}}}{tag}")
            n.set(key_attr, key_val)
            return n

        rl_sig = ensure_child(rl_top, "RunList", "SignatureName", sig_name)
        rl_sheet = ensure_child(rl_sig, "RunList", "SheetName", sheet_name)
        rl_side = ensure_child(rl_sheet, "RunList", "Side", side)

        # Per-leaf paging: 2 pages per side in sequence (0~1, 2~3, ...)
        start = index * 2
        rl_side.set("LogicalPage", str(start))
        rl_side.set("NPage", "2")
        rl_side.set("Pages", f"{start} ~ {start+1}")

        # Ensure LayoutElement/FileSpec
        le = rl_side.find("jdf:LayoutElement", namespaces=NS)
        if le is None:
            le = etree.SubElement(rl_side, f"{{{NS_JDF}}}LayoutElement")
            le.set("Class", "Parameter")
            fs = etree.SubElement(le, f"{{{NS_JDF}}}FileSpec")
            fs.set("Class", "Parameter")
            fs.set("MimeType", "application/pdf")
            fs.set("URL", url)
        else:
            fs = le.find("jdf:FileSpec", namespaces=NS)
            if fs is None:
                fs = etree.SubElement(le, f"{{{NS_JDF}}}FileSpec")
            fs.set("Class", fs.get("Class") or "Parameter")
            fs.set("MimeType", fs.get("MimeType") or "application/pdf")
            fs.set("URL", fs.get("URL") or url)

        # Ensure BCMY map-rel seps exist; preserve any existing ones untouched
        existing = {s.get("Name"): s for s in le.xpath("./jdf:SeparationSpec", namespaces=NS) if s.get("Name")}
        for name in ("B", "C", "M", "Y"):
            if name not in existing:
                sep = etree.SubElement(le, f"{{{NS_JDF}}}SeparationSpec")
                sep.set(f"{{{NS_HDM}}}IsMapRel", "true")
                sep.set(f"{{{NS_HDM}}}SubType", "Control")
                sep.set(f"{{{NS_HDM}}}Type", "Printing")
                sep.set("Name", name)

    # Set top-level NPage to reflect total marks pages consumed
    total_pages = len(side_contexts) * 2
    rl_top.set("NPage", str(total_pages))

# ------------------------------ ColorantControl ------------------------------

def ensure_colorants(root: etree._Element) -> None:
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    require(pool is not None and rlp is not None, "Missing ResourcePool/ResourceLinkPool in JDF")

    existing = pool.find(f".//jdf:ColorantControl[@ID='{COLORANTS_ID}']", namespaces=NS)
    if existing is None:
        cc = etree.SubElement(pool, f"{{{NS_JDF}}}ColorantControl")
        cc.set("ID", COLORANTS_ID)
        cc.set("Class", "Parameter")
        cc.set("Status", "Available")
        cc.set("ProcessColorModel", "DeviceCMYK")
        order = etree.SubElement(cc, f"{{{NS_JDF}}}ColorantOrder")
        sep = etree.SubElement(order, f"{{{NS_JDF}}}SeparationSpec")
        sep.set("Name", "Black")
    # Link
    link = None
    for l in rlp.xpath(".//jdf:ColorantControlLink", namespaces=NS):
        if l.get("rRef") == COLORANTS_ID:
            link = l
            break
    if link is None:
        link = etree.SubElement(rlp, f"{{{NS_JDF}}}ColorantControlLink")
        link.set("rRef", COLORANTS_ID)
    link.set("Usage", "Input")
    link.set("CombinedProcessIndex", "1")

# ------------------------------ Preview helpers (HDM:PaperRect) ------------------------------

def _parse_two_floats(s: Optional[str]) -> Optional[Tuple[float, float]]:
    if not s:
        return None
    try:
        a, b = [float(x) for x in s.strip().split()[:2]]
        return a, b
    except Exception:
        return None

def _parse_rect(s: Optional[str]) -> Optional[Tuple[float, float, float, float]]:
    if not s:
        return None
    try:
        a, b, c, d = [float(x) for x in s.strip().split()[:4]]
        return a, b, c, d
    except Exception:
        return None


def _collect_paper_dims_from_media(root: etree._Element) -> Dict[Tuple[str, str], Tuple[float, float]]:
    """Read paper W/H (points) from the partitioned Paper Media resource.
    Prefer our canonical ID; fall back to any MediaType="Paper" with the same partitioning.
    """
    dims: Dict[Tuple[str, str], Tuple[float, float]] = {}
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    if pool is None:
        return dims
    # Try canonical first
    media = pool.find(f".//jdf:Media[@ID='{PAPER_MEDIA_ID}']", namespaces=NS)
    if media is None:
        # Fallback: any partitioned Paper media
        media = pool.find(".//jdf:Media[@MediaType='Paper' and @PartIDKeys='SignatureName SheetName']", namespaces=NS)
    if media is None:
        return dims
    for sig_part in media.xpath("./jdf:Media[@SignatureName]", namespaces=NS):
        sig = sig_part.get("SignatureName")
        for leaf in sig_part.xpath("./jdf:Media[@SheetName]", namespaces=NS):
            sheet = leaf.get("SheetName")
            d = _parse_two_floats(leaf.get("Dimension"))
            if d:
                dims[(sig or "", sheet or "")] = d
    return dims


def ensure_paper_rects(root: etree._Element) -> int:
    """Add HDM:PaperRect to each Layout/Sheet (and its Surfaces) so Prinect preview centers the sheet on the plate.

    PaperRect = "llx lly urx ury" in plate coordinate system.
    We compute llx/lly from Surface/@SSi:MediaOrigin when available; else center the paper within SurfaceContentsBox.
    W/H prefer SSi:Dimension from the Surface; fallback to partitioned Paper Media. Returns number of sheets updated.
    """
    updated = 0
    rects: set = set()
    dims_map = _collect_paper_dims_from_media(root)

    pairs = enumerate_sig_sheet_pairs(root)
    for (sig, sheet, sheet_node) in pairs:
        # Prefer SSi:Dimension on the first Surface for paper size
        surface = sheet_node.find("jdf:Surface", namespaces=NS)
        wh = surface_plate_dimension(surface) if surface is not None else None
        if wh is None:
            wh = dims_map.get((sig, sheet))
        if wh is None:
            log("WARN", f"Skip PaperRect for {sig}/{sheet}: missing paper size (SSi:Dimension or Paper Media)")
            continue
        w_pt, h_pt = wh
        # Prefer MediaOrigin from the first Surface
        origin = _parse_two_floats(surface.get(f"{{{NS_SSI}}}MediaOrigin") if surface is not None else None)
        if origin is None:
            # Center within SurfaceContentsBox (from Surface or Sheet)
            scb = _parse_rect((surface.get("SurfaceContentsBox") if surface is not None else None) or sheet_node.get("SurfaceContentsBox"))
            if scb is None:
                log("WARN", f"Skip PaperRect for {sig}/{sheet}: no MediaOrigin or SurfaceContentsBox")
                continue
            llx0, lly0, urx0, ury0 = scb
            pw = urx0 - llx0
            ph = ury0 - lly0
            ox = llx0 + max(0.0, (pw - w_pt) / 2.0)
            oy = lly0 + max(0.0, (ph - h_pt) / 2.0)
        else:
            ox, oy = origin
        llx = ox
        lly = oy
        urx = ox + w_pt
        ury = oy + h_pt
        rect_str = f"{llx:.4f} {lly:.4f} {urx:.4f} {ury:.4f}"
        rects.add(rect_str)
        # Set on the Sheet node (works well in practice)
        sheet_node.set(f"{{{NS_HDM}}}PaperRect", rect_str)
        # Also mirror to each Surface leaf for safety
        for surf in sheet_node.xpath("./jdf:Surface", namespaces=NS):
            surf.set(f"{{{NS_HDM}}}PaperRect", rect_str)
        # And mirror to any Signa-style Layout nodes per Side present
        if surface is not None:
            side = surface.get("Side")
            if side:
                for lay in root.xpath(".//jdf:Layout[@Side=$side]", namespaces=NS, side=side):
                    has_sheet = lay.xpath(
                        ".//jdf:Signature[(@Name=$sig or @SignatureName=$sig)]"
                        "/jdf:Sheet[(@Name=$sheet or @SheetName=$sheet)]",
                        namespaces=NS,
                        sig=sig,
                        sheet=sheet,
                    )
                    if has_sheet:
                        lay.set(f"{{{NS_HDM}}}PaperRect", rect_str)
        updated += 1
    if len(rects) == 1:
        rect_str = next(iter(rects))
        for lay in root.xpath(".//jdf:ResourcePool/jdf:Layout", namespaces=NS):
            if lay.get(f"{{{NS_HDM}}}PaperRect") is None:
                lay.set(f"{{{NS_HDM}}}PaperRect", rect_str)
    return updated


def ensure_layout_partids(root: etree._Element) -> int:
    """Ensure Layout/Signature/Sheet have PartIDKeys-compatible attrs for preview tools."""
    updated = 0
    layout = root.find(".//jdf:ResourcePool/jdf:Layout", namespaces=NS)
    if layout is None:
        return 0
    if not layout.get("PartIDKeys"):
        layout.set("PartIDKeys", "SignatureName SheetName Side")
        updated += 1
    for sig in layout.xpath("./jdf:Signature", namespaces=NS):
        if sig.get("SignatureName") is None:
            name = sig.get("Name")
            if name:
                sig.set("SignatureName", name)
                updated += 1
        for sheet in sig.xpath("./jdf:Sheet", namespaces=NS):
            if sheet.get("SheetName") is None:
                name = sheet.get("Name")
                if name:
                    sheet.set("SheetName", name)
                    updated += 1
            # Mirror MediaOrigin onto the Sheet if present on first Surface
            if sheet.get(f"{{{NS_SSI}}}MediaOrigin") is None:
                surf0 = sheet.find("jdf:Surface", namespaces=NS)
                if surf0 is not None:
                    origin = surf0.get(f"{{{NS_SSI}}}MediaOrigin")
                    if origin:
                        sheet.set(f"{{{NS_SSI}}}MediaOrigin", origin)
                        updated += 1
            for surf in sheet.xpath("./jdf:Surface", namespaces=NS):
                if surf.get("Side") is None:
                    surf.set("Side", "Front")
                    updated += 1
    return updated


def _infer_page_orientation(ctm: Optional[str]) -> Optional[str]:
    if not ctm:
        return None
    try:
        a, b, c, d, _e, _f = [float(x) for x in ctm.strip().split()[:6]]
    except Exception:
        return None
    eps = 1e-6
    if abs(b) < eps and abs(c) < eps:
        if a >= 0.0 and d >= 0.0:
            return "0"
        if a <= 0.0 and d <= 0.0:
            return "180"
    if abs(a) < eps and abs(d) < eps:
        if b > 0.0 and c < 0.0:
            return "90"
        if b < 0.0 and c > 0.0:
            return "270"
    return None


def ensure_hdm_page_boxes(root: etree._Element) -> int:
    """Ensure HDM:FinalPageBox and HDM:PageOrientation are set on ContentObject."""
    updated = 0
    for co in root.xpath(".//jdf:ContentObject", namespaces=NS):
        if co.get(f"{{{NS_HDM}}}FinalPageBox") is None:
            trim = co.get(f"{{{NS_SSI}}}TrimBox1") or co.get("TrimBox")
            if trim:
                co.set(f"{{{NS_HDM}}}FinalPageBox", trim.strip())
                updated += 1
        if co.get(f"{{{NS_HDM}}}PageOrientation") is None:
            orient = _infer_page_orientation(co.get("CTM"))
            if orient is not None:
                co.set(f"{{{NS_HDM}}}PageOrientation", orient)
                updated += 1
    return updated


def ensure_plate_leading_edge(root: etree._Element) -> bool:
    """Set HDM:LeadingEdge on the top-level Plate Media if missing."""
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    if pool is None:
        return False
    media = None
    for m in pool.xpath(
        ".//jdf:Media[@MediaType='Plate' and @PartIDKeys='SignatureName SheetName']",
        namespaces=NS,
    ):
        media = m
        break
    if media is None:
        return False
    if media.get(f"{{{NS_HDM}}}LeadingEdge") is not None:
        return False
    dim = _parse_two_floats(media.get("Dimension"))
    if not dim:
        leaf = media.find(".//jdf:Media[@SheetName]", namespaces=NS)
        if leaf is not None:
            dim = _parse_two_floats(leaf.get("Dimension"))
    if not dim:
        return False
    _w, h = dim
    media.set(f"{{{NS_HDM}}}LeadingEdge", f"{h:.4f}")
    return True


def ensure_stripcellparams(root: etree._Element) -> bool:
    """Ensure StrippingParams has StripCellParams/TrimSize using first TrimSize or TrimBox."""
    sp = root.find(".//jdf:ResourcePool/jdf:StrippingParams", namespaces=NS)
    if sp is None:
        return False
    if sp.find("./jdf:StripCellParams", namespaces=NS) is not None:
        return False
    trim_size = None
    co = root.find(".//jdf:ContentObject[@TrimSize]", namespaces=NS)
    if co is not None:
        trim_size = co.get("TrimSize")
    if not trim_size:
        co = root.find(".//jdf:ContentObject", namespaces=NS)
        if co is not None:
            trim = co.get(f"{{{NS_SSI}}}TrimBox1") or co.get("TrimBox")
            rect = _parse_rect(trim)
            if rect:
                x0, y0, x1, y1 = rect
                trim_size = f"{(x1 - x0):.4f} {(y1 - y0):.4f}"
    if not trim_size:
        return False
    scp = etree.SubElement(sp, f"{{{NS_JDF}}}StripCellParams")
    scp.set("TrimSize", trim_size.strip())
    return True


def create_signa_layout_preview(root: etree._Element) -> Optional[str]:
    """Create a separate Signa-style Layout resource and return its ID."""
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    if pool is None:
        return None
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    layout_orig = None
    if rlp is not None:
        link = next(iter(rlp.xpath(".//jdf:LayoutLink", namespaces=NS)), None)
        if link is not None and link.get("rRef"):
            layout_orig = pool.find(f".//jdf:Layout[@ID='{link.get('rRef')}']", namespaces=NS)
    if layout_orig is None:
        layout_orig = pool.find(".//jdf:Layout", namespaces=NS)
    if layout_orig is None:
        return None

    existing_ids = {el.get("ID") for el in pool.xpath(".//*[@ID]", namespaces=NS) if el.get("ID")}
    new_id = _unique_id(existing_ids, prefix="r_LayoutPreview_")

    layout = etree.Element(f"{{{NS_JDF}}}Layout")
    layout.set("ID", new_id)
    layout.set("Class", layout_orig.get("Class") or "Parameter")
    layout.set("Status", layout_orig.get("Status") or "Available")
    layout.set("PartIDKeys", "SignatureName SheetName Side")
    desc = layout_orig.get("DescriptiveName") or "Layout"
    layout.set("DescriptiveName", f"{desc} Preview")
    if layout_orig.get("Name"):
        layout.set("Name", f"{layout_orig.get('Name')}_Preview")

    used_sheet_names: set = set()
    pairs = enumerate_sig_sheet_pairs(root)

    paper_id = None
    plate_id = None
    for m in pool.xpath(".//jdf:Media[@MediaType='Paper' and @PartIDKeys='SignatureName SheetName']", namespaces=NS):
        paper_id = m.get("ID")
        break
    for m in pool.xpath(".//jdf:Media[@MediaType='Plate' and @PartIDKeys='SignatureName SheetName']", namespaces=NS):
        plate_id = m.get("ID")
        break

    for sig_name, sheet_name, sheet_node in pairs:
        sig_layout = etree.SubElement(layout, f"{{{NS_JDF}}}Layout")
        sig_layout.set("SignatureName", sig_name)
        sig_layout.set("Name", sig_name)

        sheet_layout = etree.SubElement(sig_layout, f"{{{NS_JDF}}}Layout")
        sheet_layout.set("SheetName", sheet_name)
        unique_name = f"{sig_name}_{sheet_name}" if sheet_name in used_sheet_names else sheet_name
        sheet_layout.set("Name", unique_name)
        used_sheet_names.add(unique_name)
        scb = sheet_node.get("SurfaceContentsBox")
        if not scb:
            surf0 = sheet_node.find("jdf:Surface", namespaces=NS)
            scb = surf0.get("SurfaceContentsBox") if surf0 is not None else None
        if scb:
            sheet_layout.set("SurfaceContentsBox", scb)

        for surf in sheet_node.xpath("./jdf:Surface", namespaces=NS):
            side = surf.get("Side") or "Front"
            side_layout = etree.SubElement(sheet_layout, f"{{{NS_JDF}}}Layout")
            side_layout.set("Side", side)
            side_layout.set("DescriptiveName", side)
            rect = surf.get(f"{{{NS_HDM}}}PaperRect") or sheet_node.get(f"{{{NS_HDM}}}PaperRect")
            if rect:
                side_layout.set(f"{{{NS_HDM}}}PaperRect", rect)
            for child in surf:
                if child.tag in (f"{{{NS_JDF}}}MarkObject", f"{{{NS_JDF}}}ContentObject"):
                    side_layout.append(copy.deepcopy(child))

        if paper_id:
            etree.SubElement(sheet_layout, f"{{{NS_JDF}}}MediaRef").set("rRef", paper_id)
        if plate_id:
            etree.SubElement(sheet_layout, f"{{{NS_JDF}}}MediaRef").set("rRef", plate_id)

    pool.append(layout)
    return new_id

# ------------------------------ CPI normalization ------------------------------

def normalize_cpi_links(root: etree._Element) -> None:
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    if rlp is None:
        return
    # Document, Marks, PagePool → CPI 0
    for l in rlp.xpath(".//jdf:RunListLink", namespaces=NS):
        pu = l.get("ProcessUsage")
        if pu in ("Document", "Marks", "PagePool") or (pu is None and l.get("Usage") in ("Output",)):
            l.set("CombinedProcessIndex", "0")
    # ConventionalPrintingParamsLink → CPI 1
    for l in rlp.xpath(".//jdf:ConventionalPrintingParamsLink", namespaces=NS):
        l.set("CombinedProcessIndex", "1")
    # MediaLink handled in ensure_media_link (CPI "1 2")

# ------------------------------ Preview helpers (Cutting/CTM/Stripping) ------------------------------

def collect_sheet_positions_from_ssi(root: etree._Element) -> Dict[Tuple[str, str], Tuple[float, float, float, float]]:
    """Return {(SignatureName, SheetName): (x, y, w, h)} using:
    - w,h from Surface/@SSi:Dimension, fallback to Paper Media dims
    - origin (x,y) from Surface/@SSi:MediaOrigin, fallback to centering within SurfaceContentsBox
    Coordinates are in plate units (points).
    """
    positions: Dict[Tuple[str, str], Tuple[float, float, float, float]] = {}
    dims_map = _collect_paper_dims_from_media(root)
    pairs = enumerate_sig_sheet_pairs(root)
    for (sig, sheet, sheet_node) in pairs:
        surface = sheet_node.find("jdf:Surface", namespaces=NS)
        if surface is None:
            continue
        wh = surface_plate_dimension(surface)
        if wh is None:
            wh = dims_map.get((sig, sheet))
        if wh is None:
            continue
        w_pt, h_pt = wh
        origin = _parse_two_floats(surface.get(f"{{{NS_SSI}}}MediaOrigin"))
        if origin is None:
            scb = _parse_rect(surface.get("SurfaceContentsBox") or sheet_node.get("SurfaceContentsBox"))
            if scb is None:
                continue
            llx0, lly0, urx0, ury0 = scb
            pw = urx0 - llx0
            ph = ury0 - lly0
            ox = llx0 + max(0.0, (pw - w_pt) / 2.0)
            oy = lly0 + max(0.0, (ph - h_pt) / 2.0)
        else:
            ox, oy = origin
        positions[(sig, sheet)] = (ox, oy, w_pt, h_pt)
    return positions


def ensure_cuttingparams_from_positions(root: etree._Element,
                                        positions: Dict[Tuple[str, str], Tuple[float, float, float, float]],
                                        rid: str = "r_CutDummy") -> bool:
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    if pool is None or rlp is None:
        return False
    link = next(iter(rlp.xpath(".//jdf:CuttingParamsLink", namespaces=NS)), None)
    cpm = None
    if link is not None and link.get("rRef"):
        cpm = pool.find(f".//jdf:CuttingParams[@ID='{link.get('rRef')}']", namespaces=NS)
    if cpm is None:
        cpm = pool.find(f".//jdf:CuttingParams[@ID='{rid}']", namespaces=NS)

    created = False
    if cpm is None:
        cpm = etree.Element(f"{{{NS_JDF}}}CuttingParams")
        cpm.set("ID", rid)
        pool.append(cpm)
        created = True
    elif not cpm.get("ID"):
        cpm.set("ID", rid)

    cpm.set("Class", cpm.get("Class") or "Parameter")
    cpm.set("Status", "Available")
    cpm.set("PartIDKeys", "SignatureName SheetName")

    direct_blocks = cpm.xpath("./jdf:CutBlock", namespaces=NS)
    leaf_blocks = cpm.xpath(".//jdf:CuttingParams[@SheetName]/jdf:CutBlock", namespaces=NS)
    rebuild = created or direct_blocks or (len(leaf_blocks) != len(positions))
    if rebuild:
        for child in list(cpm):
            cpm.remove(child)
        # Build partition context leaves
        for (sig, sheet), (x, y, w, h) in sorted(positions.items()):
            p1 = etree.SubElement(cpm, f"{{{NS_JDF}}}CuttingParams")
            p1.set("SignatureName", sig)
            p2 = etree.SubElement(p1, f"{{{NS_JDF}}}CuttingParams")
            p2.set("SheetName", sheet)
            # Per-sheet block with placement (CIP3BlockTrf translation)
            blk = etree.SubElement(p2, f"{{{NS_JDF}}}CutBlock")
            blk.set("Class", "Parameter")
            blk.set("BlockElementType", "CutElement")
            blk.set("BlockType", "CutBlock")
            blk.set("BlockName", f"{sig}_{sheet}_B_1_1")
            blk.set("BlockSize", f"{w:.6f} {h:.6f}")
            blk.set("BlockTrf", "1 0 0 1 0 0")
            blk.set(f"{{{NS_HDM}}}CIP3BlockTrf", f"1 0 0 1 {x:.6f} {y:.6f}")

    if link is None:
        link = etree.SubElement(rlp, f"{{{NS_JDF}}}CuttingParamsLink")
    link.set("Usage", "Input")
    link.set("rRef", cpm.get("ID") or rid)
    return created or rebuild


def ensure_transfer_ctm_from_positions(root: etree._Element,
                                       positions: Dict[Tuple[str, str], Tuple[float, float, float, float]],
                                       rid: str = "r_TransferCTM") -> bool:
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    if pool is None or rlp is None:
        return False
    # Skip if already linked
    if root.xpath('.//jdf:TransferCurvePoolLink', namespaces=NS):
        return False
    tcp = etree.Element(f"{{{NS_JDF}}}TransferCurvePool")
    tcp.set("Class", "Parameter")
    tcp.set("Status", "Available")
    tcp.set("ID", rid)
    tcp.set("PartIDKeys", "SignatureName SheetName")

    for (sig, sheet), (x, y, _w, _h) in sorted(positions.items()):
        p1 = etree.SubElement(tcp, f"{{{NS_JDF}}}TransferCurvePool")
        p1.set("SignatureName", sig)
        p2 = etree.SubElement(p1, f"{{{NS_JDF}}}TransferCurvePool")
        p2.set("SheetName", sheet)
        # Paper CTM translates by -origin
        etree.SubElement(p2, f"{{{NS_JDF}}}TransferCurveSet").attrib.update({
            "Name": "Paper", "CTM": f"1 0 0 1 {-x:.6f} {-y:.6f}",
        })
        # Plate CTM identity
        etree.SubElement(p2, f"{{{NS_JDF}}}TransferCurveSet").attrib.update({
            "Name": "Plate", "CTM": "1 0 0 1 0 0",
        })

    pool.append(tcp)
    link = etree.SubElement(rlp, f"{{{NS_JDF}}}TransferCurvePoolLink")
    link.set("Usage", "Input")
    link.set("rRef", rid)
    return True


def ensure_stripping_positions(root: etree._Element,
                               positions: Dict[Tuple[str, str], Tuple[float, float, float, float]],
                               rid: str = "r_StripPos") -> bool:
    pool = root.find(".//jdf:ResourcePool", namespaces=NS)
    rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
    if pool is None or rlp is None:
        return False
    # Reuse existing resource if present; else create
    sp = pool.find(f".//jdf:StrippingParams[@ID='{rid}']", namespaces=NS)
    if sp is None:
        sp = etree.Element(f"{{{NS_JDF}}}StrippingParams")
        sp.set("Class", "Parameter")
        sp.set("Status", "Available")
        sp.set("ID", rid)
        sp.set("PartIDKeys", "SignatureName SheetName")
        pool.append(sp)
        l = etree.SubElement(rlp, f"{{{NS_JDF}}}StrippingParamsLink")
        l.set("Usage", "Input")
        l.set("rRef", rid)
    if sp.get("WorkStyle") is None:
        ws = None
        for sheet in root.xpath(".//jdf:Sheet", namespaces=NS):
            code = sheet.get(f"{{{NS_SSI}}}WorkStyle")
            if not code:
                continue
            ws = SSI_TO_JDF_WORKSTYLE.get(code)
            if ws:
                break
        if ws:
            sp.set("WorkStyle", ws)

    # Ensure MediaRef entries if missing
    existing_refs = {ref.get("rRef") for ref in sp.xpath("./jdf:MediaRef", namespaces=NS)}
    for rid_candidate in (PAPER_MEDIA_ID, PLATE_MEDIA_ID):
        if rid_candidate in existing_refs:
            continue
        if pool.find(f".//jdf:Media[@ID='{rid_candidate}']", namespaces=NS) is not None:
            etree.SubElement(sp, f"{{{NS_JDF}}}MediaRef").set("rRef", rid_candidate)

    # Remove any existing unqualified Position nodes to avoid duplicates
    for old in list(sp.xpath("./jdf:Position", namespaces=NS)):
        sp.remove(old)

    cleared_sigs: set = set()
    for (sig, sheet), (x, y, w, h) in sorted(positions.items()):
        # Ensure partition chain exists
        p_sig = None
        for c in sp.xpath(f"./jdf:StrippingParams[@SignatureName='{sig}']", namespaces=NS):
            p_sig = c
            break
        if p_sig is None:
            p_sig = etree.SubElement(sp, f"{{{NS_JDF}}}StrippingParams")
            p_sig.set("SignatureName", sig)
        p_sheet = None
        for c in p_sig.xpath(f"./jdf:StrippingParams[@SheetName='{sheet}']", namespaces=NS):
            p_sheet = c
            break
        if p_sheet is None:
            p_sheet = etree.SubElement(p_sig, f"{{{NS_JDF}}}StrippingParams")
            p_sheet.set("SheetName", sheet)
        # Clear any existing per-sheet positions to avoid duplicates
        for old in list(p_sheet.xpath("./jdf:Position", namespaces=NS)):
            p_sheet.remove(old)
        if sig not in cleared_sigs:
            for old in list(p_sig.xpath("./jdf:Position", namespaces=NS)):
                p_sig.remove(old)
            cleared_sigs.add(sig)
        # Compute RelativeBox from plate SCB
        sheet_nodes = root.xpath(
            ".//jdf:Signature[(@Name=$sig or @SignatureName=$sig)]"
            "/jdf:Sheet[(@Name=$sheet or @SheetName=$sheet)]",
            namespaces=NS,
            sig=sig,
            sheet=sheet,
        )
        sheet_node = sheet_nodes[0] if sheet_nodes else None
        scb = None
        if sheet_node is not None:
            scb = sheet_node.get("SurfaceContentsBox")
            if not scb:
                surf0 = sheet_node.find("jdf:Surface", namespaces=NS)
                scb = surf0.get("SurfaceContentsBox") if surf0 is not None else None
        if not scb:
            continue
        xs, ys, xe, ye = [float(v) for v in scb.split()[:4]]
        pw, ph = (xe - xs), (ye - ys)
        left = x / pw
        bottom = y / ph
        right = (x + w) / pw
        top = (y + h) / ph
        etree.SubElement(p_sig, f"{{{NS_JDF}}}Position").set(
            "RelativeBox",
            f"{left:.8f} {bottom:.8f} {right:.8f} {top:.8f}",
        )
    return True

# ------------------------------ Summary ------------------------------

def write_summary(summary_path: str, mode: str,
                  paper_rows: List[Tuple[str,str,Tuple[float,float],Optional[str],int,int,str]],
                  plate_rows: List[Tuple[str,str,Tuple[float,float]]]) -> None:
    with open(summary_path, "w", encoding="utf-8") as f:
        f.write(f"Label mode: {mode}\n")
        f.write("Paper:\n")
        for (sig, sheet, (w_pt,h_pt), grain, gsm, mic, human) in paper_rows:
            f.write(
                f"  {sig}/{sheet}: {w_pt:.4f} x {h_pt:.4f} pt  "
                f"({round4_in(points_to_inches(w_pt))} x {round4_in(points_to_inches(h_pt))} in); "
                f"Grain={grain or '-'}; Weight={gsm} gsm; Thick={mic} µm; Stock='{human}'\n"
            )
        f.write("Plate:\n")
        for (sig, sheet, (w_pt,h_pt)) in plate_rows:
            f.write(
                f"  {sig}/{sheet}: {w_pt:.4f} x {h_pt:.4f} pt  "
                f"({round4_in(points_to_inches(w_pt))} x {round4_in(points_to_inches(h_pt))} in)\n"
            )

# ------------------------------ Main transform ------------------------------

def transform(jdf_path: str, mxml_path: str, out_path: str, validate_only: bool,
              labels_mode_arg: str, do_paper: bool, do_plate: bool, do_marks: bool,
              verbosity: str, do_signa_layout: bool = False) -> None:
    tree = read_xml(jdf_path)
    root = jdf_root(tree)
    ensure_namespaces(root)

    mxml = read_xml(mxml_path)

    # ConventionalPrintingParams from SSi WorkStyle
    try:
        made_conv = inject_workstyle_from_ssi(root)
        if made_conv:
            log("OK", "ConventionalPrintingParams injected from SSi:WorkStyle")
        else:
            log("WARN", "No SSi:WorkStyle found; skipping ConventionalPrintingParams")
    except Exception as e:
        require(False, f"Failed to inject ConventionalPrintingParams: {e}")

    # Labels
    mode = derive_label_mode(labels_mode_arg, root, mxml)
    labels = build_labels(root, mxml, mode)
    if not validate_only:
        apply_labels(root, labels)
    log("OK", f"Labels applied (mode={mode})")

    # Media
    paper_summary: List[Tuple[str,str,Tuple[float,float],Optional[str],int,int,str]] = []
    plate_summary: List[Tuple[str,str,Tuple[float,float]]] = []

    if do_paper:
        stocks = mxml_read_layout_stock_sequence(mxml)
        paper_summary = set_paper_media(root, stocks)
        ensure_media_link(root, PAPER_MEDIA_ID)
        ensure_media_refs(root, PAPER_MEDIA_ID)
        log("OK", f"Paper media set for {len(paper_summary)} sheet(s)")

    if do_plate:
        plate_summary = set_plate_media(root)
        ensure_media_link(root, PLATE_MEDIA_ID)
        ensure_media_refs(root, PLATE_MEDIA_ID)
        log("OK", f"Plate media set for {len(plate_summary)} sheet(s)")

    if do_marks:
        ensure_marks_runlist(root)
        log("OK", "Marks RunList normalized (BCMY map-rel)")

    ensure_colorants(root)
    normalize_cpi_links(root)

    # Ensure PaperRect preview rects after media and CPI are in place
    pr_count = ensure_paper_rects(root)
    log("OK", f"HDM:PaperRect set on {pr_count} sheet(s)")
    part_count = ensure_layout_partids(root)
    if part_count:
        log("OK", f"Layout PartIDKeys and names normalized ({part_count} updates)")

    # Add HDM page boxes/orientations and Signa-style Layout/Side preview tree
    page_updates = ensure_hdm_page_boxes(root)
    if page_updates:
        log("OK", f"HDM:FinalPageBox/PageOrientation set on {page_updates} ContentObject(s)")
    lead = ensure_plate_leading_edge(root)
    if lead:
        log("OK", "HDM:LeadingEdge set on Plate Media")
    scp = ensure_stripcellparams(root)
    if scp:
        log("OK", "StripCellParams TrimSize set")
    if do_signa_layout:
        signa_id = create_signa_layout_preview(root)
        if signa_id:
            rlp = root.find(".//jdf:ResourceLinkPool", namespaces=NS)
            if rlp is not None:
                links = list(rlp.xpath(".//jdf:LayoutLink", namespaces=NS))
                if not links:
                    links = [etree.SubElement(rlp, f"{{{NS_JDF}}}LayoutLink")]
                for link in links:
                    link.set("Usage", "Input")
                    link.set("rRef", signa_id)
            log("OK", f"Signa preview LayoutLink set to {signa_id}")

    # Add preview helpers (CuttingParams with CIP3BlockTrf, TransferCurvePool CTMs, StrippingParams positions)
    try:
        positions = collect_sheet_positions_from_ssi(root)
        if positions:
            made_cut = ensure_cuttingparams_from_positions(root, positions)
            if made_cut:
                log("OK", "CuttingParams with HDM:CIP3BlockTrf added")
            made_tcp = ensure_transfer_ctm_from_positions(root, positions)
            if made_tcp:
                log("OK", "TransferCurvePool (Paper/Plate CTMs) added")
            made_strip = ensure_stripping_positions(root, positions)
            if made_strip:
                log("OK", "StrippingParams positions added")
        else:
            log("WARN", "No sheet positions from SSi; preview helpers skipped")
    except Exception as e:
        log("WARN", f"Preview helper injection failed: {e}")

    if validate_only:
        log("OK", "Validation-only: no output written")
        return

    write_xml(tree, out_path)
    log("OK", f"Wrote cleaned JDF: {out_path}")

    # Sidecar summary
    summary_path = os.path.splitext(out_path)[0] + ".summary.txt"
    write_summary(summary_path, mode, paper_summary, plate_summary)
    log("OK", f"Wrote summary: {summary_path}")

# ------------------------------ CLI ------------------------------

def main():
    ap = argparse.ArgumentParser(description="Metrix → Signa JDF transformer")
    ap.add_argument("job", help="Job name/number (used to locate JOB.jdf and JOB.mxml)")
    ap.add_argument("in_path", help="Input directory containing JOB.jdf and JOB.mxml")
    ap.add_argument("out_path", help="Output directory (writes Data.jdf)")
    ap.add_argument("--validate-only", action="store_true", help="Validate without writing")
    ap.add_argument("--labels", choices=["auto", "postcards", "book", "multiproduct"], default="auto",
                    help="Labeling mode (auto detects by JDF/MXML)")
    ap.add_argument("--no-paper", action="store_true", help="Skip paper media injection")
    ap.add_argument("--no-plate", action="store_true", help="Skip plate media injection")
    ap.add_argument("--no-marks", action="store_true", help="Skip marks RunList normalization")
    ap.add_argument("--signa-layout-preview", action="store_true",
                    help="Add Signa-style Layout/Side preview nodes (may duplicate sheets)")
    ap.add_argument("--verbosity", choices=["info", "debug"], default="info")

    args = ap.parse_args()

    job = args.job.strip()
    in_dir = Path(args.in_path).expanduser().resolve()
    out_dir = Path(args.out_path).expanduser().resolve()
    jdf_path = in_dir / f"{job}.jdf"
    mxml_path = in_dir / f"{job}.mxml"
    out_path = out_dir / "Data.jdf"

    require(jdf_path.exists(), f"Metrix JDF not found: {jdf_path}")
    require(mxml_path.exists(), f"MXML not found: {mxml_path}")
    out_dir.mkdir(parents=True, exist_ok=True)

    log("INFO", f"Job: {job}")
    log("INFO", f"Metrix JDF: {jdf_path}")
    log("INFO", f"MXML: {mxml_path}")
    log("INFO", f"Output: {out_path}")

    try:
        transform(
            jdf_path=str(jdf_path),
            mxml_path=str(mxml_path),
            out_path=str(out_path),
            validate_only=args.validate_only,
            labels_mode_arg=args.labels,
            do_paper=(not args.no_paper),
            do_plate=(not args.no_plate),
            do_marks=(not args.no_marks),
            verbosity=args.verbosity,
            do_signa_layout=args.signa_layout_preview,
        )
    except SystemExit:
        raise
    except Exception as e:
        log("ERROR", f"Unhandled exception: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()

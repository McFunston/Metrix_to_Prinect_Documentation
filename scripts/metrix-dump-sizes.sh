#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 2 ]; then
  echo "Usage: scripts/metrix-dump-sizes.sh <metrix-jdf> <signa-jdf>"
  exit 1
fi

metrix_jdf="$1"
signa_jdf="$2"

python3 - "$metrix_jdf" "$signa_jdf" <<'PY'
import sys
from xml.etree import ElementTree as ET

metrix_jdf = sys.argv[1]
signa_jdf = sys.argv[2]

NS_JDF = "http://www.CIP4.org/JDFSchema_1_1"
NS_SSI = "http://www.creo.com/SSI/JDFExtensions.xsd"
NS_HDM = "www.heidelberg.com/schema/HDM"


def parse_metrix(path):
    tree = ET.parse(path)
    root = tree.getroot()
    ns = {"jdf": NS_JDF, "SSi": NS_SSI}
    results = []
    for sig in root.findall(".//jdf:Signature", ns):
        sig_name = sig.get("Name") or sig.get("SignatureName")
        for sheet in sig.findall("./jdf:Sheet", ns):
            sheet_name = sheet.get("Name") or sheet.get("SheetName")
            scb = sheet.get("SurfaceContentsBox")
            surf = sheet.find("./jdf:Surface", ns)
            if surf is not None and not scb:
                scb = surf.get("SurfaceContentsBox")
            dim = None
            origin = None
            if surf is not None:
                dim = surf.get(f"{{{NS_SSI}}}Dimension")
                origin = surf.get(f"{{{NS_SSI}}}MediaOrigin")
            results.append((sig_name, sheet_name, scb, dim, origin))
    return results


def parse_signa(path):
    tree = ET.parse(path)
    root = tree.getroot()
    ns = {"jdf": NS_JDF, "HDM": NS_HDM}
    results = []
    for sig in root.findall(".//jdf:Layout[@SignatureName]", ns):
        sig_name = sig.get("SignatureName")
        for sheet in sig.findall("./jdf:Layout[@SheetName]", ns):
            sheet_name = sheet.get("SheetName")
            scb = sheet.get("SurfaceContentsBox")
            paper_rect = None
            side = sheet.find("./jdf:Layout[@Side]", ns)
            if side is not None:
                paper_rect = side.get(f"{{{NS_HDM}}}PaperRect")
            results.append((sig_name, sheet_name, scb, paper_rect))
    media_dims = {}
    for media in root.findall(".//jdf:Media[@MediaType='Paper']", ns):
        dim = media.get("Dimension")
        if dim:
            media_dims.setdefault("Paper", []).append(dim)
    for media in root.findall(".//jdf:Media[@MediaType='Plate']", ns):
        dim = media.get("Dimension")
        if dim:
            media_dims.setdefault("Plate", []).append(dim)
    return results, media_dims


metrix = parse_metrix(metrix_jdf)
signa, media_dims = parse_signa(signa_jdf)

print("Metrix JDF (Signature, Sheet, SurfaceContentsBox, SSi:Dimension, SSi:MediaOrigin)")
for row in metrix:
    print("-", row)

print("\nSigna JDF (Signature, Sheet, SurfaceContentsBox, HDM:PaperRect)")
for row in signa:
    print("-", row)

print("\nSigna Media Dimensions")
for key, vals in media_dims.items():
    print(f"- {key}: {sorted(set(vals))}")
PY

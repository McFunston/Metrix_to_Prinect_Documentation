# Metrix to Prinect Documentation
![License](https://img.shields.io/badge/license-MIT-blue)

This project documents and normalizes Metrix JDF output for Heidelberg Prinect Cockpit import.

This project documents observed behavior of JDFs produced by specific versions of Metrix and Signa, based on empirical testing.
It is not vendor documentation and makes no claims about internal algorithms or future behavior.

## Scope
- Document and normalize Metrix JDF output for Prinect Cockpit import.
- Focus on imposition-related resources, geometry, and page assignment behavior.
- Use empirical testing against specific Metrix/Signa builds.

## Non-goals
- Full CIP4 JDF coverage or schema implementation.
- JMF workflows, device automation, or MIS/ERP integration.
- Vendor-internal algorithm explanations or future guarantees.

## Unsupported areas
- Bindery-executable JDF (folding machines, cutters, stitchers).
- Bindery components that require licensed Prinect modules or physical device integration.

## Included reference data

- `JDF_Schema/JDFFoldingSchemes/Folds.json` contains standard JDF fold scheme metadata used in documentation and validation notes.

## Private sample data

Sample files are not included in this repository. They contain proprietary job names and customer data.

Tests that exercise sample parsing are skipped automatically when private samples are not present.

## Data policy

This repository intentionally excludes any customer data, proprietary job identifiers, and production assets. All sample references are anonymized; private samples and mappings must remain outside the repo.

See `CONTRIBUTING.md` for contribution guidelines.

Expected local layout (outside this repo):

```
~/Metrix_to_Cockpit_PrivateSamples/
  Metrix_Samples/
  Signa_Samples/
  GeneratedJDFs/
```

Use absolute/relative paths to those folders when running tools. See `docs/samples/README.md` for more detail.

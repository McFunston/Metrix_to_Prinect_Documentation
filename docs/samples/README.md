# Private sample data

This repository does not include Metrix/Signa sample files. The sample data contains proprietary job names and customer information and must remain private.

## Recommended local layout

Create a private folder outside this repo, for example:

```
~/Metrix_to_Cockpit_PrivateSamples/
  Metrix_Samples/
  Signa_Samples/
  GeneratedJDFs/
```

## Usage

When running the CLI tools, pass absolute or relative paths to the private sample folders (e.g., `~/Metrix_to_Cockpit_PrivateSamples/Metrix_Samples/...`).

If desired, set an environment variable such as `METRIX_SAMPLES_DIR` and use it in your local scripts to avoid hard‑coding paths.

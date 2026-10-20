# Changelog — final quality / art / balance pass
- Art Quality Validator + self-tests + waivers; ArtPipeline CLI with staging, transactional import, rollback, history.
- Art provenance: manifest entries now record the generator module (`src`) and staged output hash (`sha256`).
- NUnit `ArtReferenceTests`: every runtime sprite key exists in the manifest.

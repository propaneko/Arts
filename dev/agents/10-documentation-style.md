# Documentation Style

Apply these rules when writing or editing repository documentation.

1. Write for collaborators first.
   - Explain how contributors actually build, launch, test, and maintain the repository.
   - Keep player instructions and colorful mod descriptions on the Vintage Story ModDB; link there instead of duplicating them.

2. Use a human, candid voice.
   - Prefer direct and conversational wording over corporate or policy-like prose.
   - Mild humor and personality are welcome when they do not obscure the instructions.
   - Avoid making rough edges sound more formal or deliberate than they are.

3. Prefer practical guidance over exhaustive description.
   - Document real workflows, useful defaults, and known exceptions.
   - Include implementation detail only when it helps someone make a change safely.
   - Explain why a workflow exists when that changes how contributors should use it.

4. Present the common case first.
   - Describe the default Windows setup before custom paths, launchers, profiles, or other operating systems.
   - Put optional overrides and exceptional configurations in clearly named subsections.

5. Explain distinctions that affect the workflow.
   - State that Vintage Story loads unpacked mod directories directly.
   - Treat building as the normal development loop.
   - Treat packaging as optional release preparation rather than a development requirement.

6. Be honest about tools and conventions.
   - Describe CakeBuild as convenient inherited release tooling, not the only valid way to package mods.
   - Call out exceptional projects plainly. In particular, AXS requires XSkills/xLib for development and the installed XSkills mod for runtime testing.
   - It is acceptable to mention that contributors can unload irrelevant optional projects in supporting IDEs.

7. Avoid duplicating or hardcoding volatile information.
   - Do not manually maintain release status or current-version statements when they should eventually be generated.
   - Link to the authoritative publishing page or source when appropriate.
   - Do not repeat the same guidance across README and agent documents unless each audience needs it.

8. Keep future plans modest and explicit.
   - Label automation, release checklists, and improved validation as future goals when they do not exist yet.
   - Do not present aspirational workflows as current capabilities.

9. Separate documentation by audience.
   - `README.md` is human-facing and collaborator-oriented.
   - Root `AGENTS.md` is a small discovery index.
   - Detailed coding-agent context belongs in ordered Markdown files under `dev/agents/`.

10. Keep attribution factual.
    - Name known authors, upstream repositories, platforms, and third-party tools.
    - Do not invent or infer licensing terms.
    - Defer formal licensing claims until the repository and bundled dependencies have been audited.

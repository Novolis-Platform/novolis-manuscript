# Novolis Manuscript Protocol 1 

## Purpose

NMP/1 defines a deterministic filesystem and YAML contract for locating, cataloging, validating, and processing manuscript projects.

It deliberately covers:

* Workspace discovery
* Fiction and non-fiction organization
* Universes, subjects, series, and books
* Chapter and appendix ordering
* Hierarchical reference material
* Content metadata

It deliberately does **not** define:

* PDF or HTML rendering
* CSS
* Debug output
* AI/editorial policy
* Build commands
* Generated output locations
* Application-specific UI state

Those belong to consumers of the protocol, not the manuscript itself.

---

## 1. Design principles

### 1.1 Structure carries meaning

A book's kind, universe, subject, and series are inferred from its location.

Do not repeat structural information in YAML.

For example, a book inside:

```text
src/Fiction/galactic-confederation/the-calypso-cycle/calypso/
```

does not need:

```yaml
id: calypso
series: The Calypso Cycle
universe: Galactic Confederation
kind: fiction
```

Those values are already represented by the path.

### 1.2 Marker files identify node types

Optional series must not be inferred merely from directory depth.

A directory containing:

* `universe.yaml` is a universe
* `subject.yaml` is a non-fiction subject
* `series.yaml` is a series
* `book.yaml` is a book

This makes the optional series layer deterministic without introducing extra `Series/` and `Books/` container directories.

### 1.3 One canonical convention

NMP/1 has:

* One workspace marker
* One spelling for every reserved directory
* One chapter-ordering rule
* One metadata format
* No legacy aliases

In particular:

* `References`, never `Reference`
* `Chapters`, never `chapters`
* Numeric filename ordering, never an optional book setting
* YAML front matter, not a mixture of front matter, comments, and heading-derived order

Legacy compatibility should be implemented as an adapter, not allowed to seep into the protocol.

### 1.4 Metadata describes content

`book.yaml` describes the book.

It must not contain renderer switches such as:

```yaml
debug_mode: true
chapter_order_from_heading: true
```

The existing books repository uses these effectively, but they are build configuration rather than manuscript metadata.

### 1.5 IDs come from directory names

Directory names are canonical identifiers.

They use lowercase kebab-case:

```text
galactic-confederation
the-calypso-cycle
calypso
software-engineering
programming-fundamentals
```

There is no `id` field to drift out of sync with the filesystem.

---

## 2. Canonical workspace structure

```text
manuscript.yaml

src/
├── Fiction/
│   └── <universe>/
│       ├── universe.yaml
│       ├── References/
│       │   └── **/*.md
│       │
│       ├── <standalone-book>/
│       │   ├── book.yaml
│       │   ├── Chapters/
│       │   ├── Appendices/
│       │   ├── References/
│       │   └── Assets/
│       │
│       └── <series>/
│           ├── series.yaml
│           ├── References/
│           │   └── **/*.md
│           │
│           └── <book>/
│               ├── book.yaml
│               ├── Chapters/
│               ├── Appendices/
│               ├── References/
│               └── Assets/
│
└── NonFiction/
    └── <subject>/
        ├── subject.yaml
        ├── References/
        │   └── **/*.md
        │
        └── <book>/
            ├── book.yaml
            ├── Chapters/
            ├── Appendices/
            ├── References/
            └── Assets/
```

Only `Chapters` is required inside a book.

The following are optional:

* `Appendices`
* `References`
* `Assets`

A fiction universe can contain:

* Standalone books
* Series
* Both simultaneously

A non-fiction subject contains books directly. NMP/1 does not introduce non-fiction series. That can be added in a future protocol version if a concrete need appears.

---

## 3. Example structure

```text
manuscript.yaml

src/
├── Fiction/
│   └── galactic-confederation/
│       ├── universe.yaml
│       ├── References/
│       │   ├── history/
│       │   │   └── timeline.md
│       │   ├── politics/
│       │   │   └── earth-union.md
│       │   └── species/
│       │       └── humans.md
│       │
│       └── the-calypso-cycle/
│           ├── series.yaml
│           ├── References/
│           │   └── continuity/
│           │       └── series-timeline.md
│           │
│           ├── calypso/
│           │   ├── book.yaml
│           │   ├── Chapters/
│           │   │   ├── 10-the-rescue.md
│           │   │   ├── 20-first-contact.md
│           │   │   └── 30-oh-hell-no.md
│           │   ├── Appendices/
│           │   ├── References/
│           │   └── Assets/
│           │
│           └── personage/
│               ├── book.yaml
│               └── Chapters/
│
└── NonFiction/
    └── software-engineering/
        ├── subject.yaml
        ├── References/
        │   └── terminology.md
        │
        └── programming-fundamentals/
            ├── book.yaml
            ├── Chapters/
            ├── Appendices/
            └── Assets/
```

The hierarchical reference organization is intentionally inspired by the current books repository, where universe material is already divided semantically into areas such as politics, species, ships, history, and places.

---

## 4. Reserved names

Reserved names are case-sensitive:

| Name         | Meaning                                |
| ------------ | -------------------------------------- |
| `src`        | Protocol source root                   |
| `Fiction`    | Fiction branch                         |
| `NonFiction` | Non-fiction branch                     |
| `Chapters`   | Ordered primary manuscript documents   |
| `Appendices` | Ordered publishable appendix documents |
| `References` | Non-publishable supporting material    |
| `Assets`     | Book-local images and other resources  |

Authored identifiers must not use reserved names.

Fixed metadata filenames are lowercase:

```text
manuscript.yaml
universe.yaml
subject.yaml
series.yaml
book.yaml
```

This deliberate casing distinguishes protocol directories from authored IDs.

---

## 5. Workspace metadata

The workspace root is identified by `manuscript.yaml`.

```yaml
protocol: novolis.manuscript
version: 1

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

### Required fields

| Field      | Type    | Meaning                      |
| ---------- | ------- | ---------------------------- |
| `protocol` | string  | Must be `novolis.manuscript` |
| `version`  | integer | Protocol major version       |

### Optional fields

| Field      | Type   | Meaning               |
| ---------- | ------ | --------------------- |
| `defaults` | object | Default book metadata |

Workspace discovery walks upward until it finds `manuscript.yaml`.

It does not guess based on the presence of arbitrary directories. This replaces the current approach of searching for `content/series` or `content/books`.

Unsupported major versions are rejected.

---

## 6. Defaults and inheritance

The following fields may be supplied through `defaults`:

```yaml
defaults:
  authors:
    - Frank R. Haugen
  language: en-US
  rights: Copyright © Frank R. Haugen
```

Defaults may appear in:

* `manuscript.yaml`
* `universe.yaml`
* `subject.yaml`
* `series.yaml`

Effective book metadata is resolved nearest-first:

```text
book
→ series
→ universe or subject
→ workspace
```

Only values inside `defaults` are inherited.

Inheritance is replacement-based, not deep merging. For example, a book-level `authors` list replaces the inherited list completely.

Titles, descriptions, ordering, publication data, and extension data are never inherited.

---

## 7. Universe metadata

`universe.yaml` is required for every fiction universe.

```yaml
title: Galactic Confederation

description: >
  Fiction set within the Galactic Confederation continuity.

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

### Schema

| Field         | Required | Type            |
| ------------- | -------: | --------------- |
| `title`       |      yes | string          |
| `description` |       no | string          |
| `defaults`    |       no | defaults object |
| `extensions`  |       no | free-form map   |

The universe ID is the directory name.

---

## 8. Subject metadata

`subject.yaml` is required for every non-fiction subject.

```yaml
title: Software Engineering

description: >
  Books about programming, software architecture, and engineering practice.

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

Its schema is identical to `universe.yaml`.

Universe and subject remain separate domain types even though their metadata shapes happen to match.

---

## 9. Series metadata

`series.yaml` identifies a fiction series.

```yaml
title: The Calypso Cycle

description: >
  A sequence of novels following Calypso and her crew.

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

### Schema

| Field         | Required | Type            |
| ------------- | -------: | --------------- |
| `title`       |      yes | string          |
| `description` |       no | string          |
| `defaults`    |       no | defaults object |
| `extensions`  |       no | free-form map   |

The current repository's `series.yaml` contains both `id` and `name`. NMP/1 removes that duplication: the directory supplies the ID and `title` supplies the display name.

---

## 10. Book metadata

`book.yaml` is required.

### Fiction example

```yaml
title: Calypso
subtitle: Book One
order: 1

authors:
  - Frank R. Haugen

language: en-US
description: >
  The first novel in The Calypso Cycle.

rights: Copyright © Frank R. Haugen 2025

targets:
  words: 130000
```

### Non-fiction example

```yaml
title: Programming Fundamentals with C# and .NET
subtitle: A University Introduction to Software Development

authors:
  - Frank R. Haugen

language: en-US

description: >
  A university-level introduction to programming with modern C# and .NET.

publication:
  version: 1.0.0
  isbn: null
  date: null
```

### Schema

| Field                 |         Required | Type             | Notes                        |
| --------------------- | ---------------: | ---------------- | ---------------------------- |
| `title`               |              yes | string           | Canonical title              |
| `subtitle`            |               no | string           |                              |
| `order`               | for series books | integer          | Position within the series   |
| `authors`             |               no | string array     | May be inherited             |
| `language`            |               no | string           | BCP 47 natural-language code |
| `description`         |               no | string           |                              |
| `rights`              |               no | string           | May be inherited             |
| `targets.words`       |               no | positive integer | Authoring metric             |
| `publication.version` |               no | string           | Edition or release version   |
| `publication.isbn`    |               no | string or null   |                              |
| `publication.date`    |               no | ISO date or null |                              |
| `extensions`          |               no | free-form map    | Tool-specific metadata       |

The following fields are intentionally absent:

```yaml
id:
kind:
series:
universe:
subject:
chapter_order_from_heading:
chapter_sort_numeric_prefix:
debug_mode:
```

They are either inferred structurally or belong to tools.

The current Calypso metadata repeats its series name and carries chapter-order and debug renderer switches in the same document. NMP/1 separates these concerns.

---

## 11. Chapter and appendix filenames

Documents use:

```text
<number>-<slug>.md
```

Examples:

```text
1-arrival.md
10-the-rescue.md
20-first-contact.md
113-oh-hell-no.md
```

Rules:

* Number is a positive integer.
* Slug is lowercase kebab-case.
* Order is always numeric by filename prefix.
* Order numbers must be unique within their directory.
* Chapter and appendix numbering are independent.
* Nested chapter directories are not supported in NMP/1.

Authors may leave gaps:

```text
100-opening.md
200-first-contact.md
300-aftermath.md
```

This permits later insertion without introducing decimal order numbers or additional ordering metadata.

The filename is the only ordering authority. There is no fallback to HTML comments, YAML chapter numbers, heading numbers, or lexical sort.

---

## 12. Chapter document format

The canonical chapter format is:

```markdown
---
date: "2496.349"
system: Centralis Omnis System
locations:
  - The Hub, Maintenance Corridor E-17
  - Calypso, Docking Ring 3
pov: Marsh
characters:
  - Marsh
  - Ryn
status: draft
tags:
  - maintenance-grid
---

# Oh Hell No

The message from station administration had been brief.
```

### Required content

* Exactly one initial level-one heading
* Non-empty title
* Markdown body may otherwise use the supported markup dialect

### Derived information

| Property            | Source                               |
| ------------------- | ------------------------------------ |
| Order               | Filename numeric prefix              |
| Slug                | Filename suffix                      |
| Title               | First H1                             |
| Kind                | `Chapters` or `Appendices` directory |
| Book                | Structural path                      |
| Fiction/non-fiction | Structural path                      |

The title and order are not duplicated in YAML.

### Common front-matter fields

| Field        | Type          |
| ------------ | ------------- |
| `status`     | string        |
| `tags`       | string array  |
| `extensions` | free-form map |

### Fiction fields

| Field        | Type         |
| ------------ | ------------ |
| `date`       | string       |
| `time`       | string       |
| `system`     | string       |
| `locations`  | string array |
| `pov`        | string       |
| `characters` | string array |

Dates such as `2496.349` must be quoted so YAML does not interpret them as numeric values.

The current callout convention distinguishes public location/date fields from hidden editorial fields. That remains useful as a rendering policy, but the visibility decision belongs to the renderer rather than NMP/1.

A compatibility reader may continue accepting existing callouts, but canonical NMP/1 writers should produce YAML front matter.

---

## 13. Reference protocol

`References` may exist at:

* Universe level
* Subject level
* Series level
* Book level

Reference discovery is recursive.

For a fiction book in a series, its available scopes are:

```text
Universe References
Series References
Book References
```

For standalone fiction:

```text
Universe References
Book References
```

For non-fiction:

```text
Subject References
Book References
```

References are not implicitly merged or overridden.

Every reference retains its scope as part of its catalog identity:

```text
fiction/galactic-confederation/reference/history/timeline
fiction/galactic-confederation/the-calypso-cycle/reference/continuity/series-timeline
fiction/galactic-confederation/the-calypso-cycle/calypso/reference/editorial/open-questions
```

This means two scopes may contain the same relative path without becoming the same document.

### Reference file format

```markdown
---
aliases:
  - EU Fleet
tags:
  - faction
  - military
---

# Earth Fleet

Earth Fleet is...
```

Reference front matter is optional.

The first H1 supplies the title. The relative path supplies the reference ID.

Files or directories whose names begin with `_` or `.` are excluded from catalog discovery:

```text
_archive/
_drafts/
.private/
```

This gives authors a simple way to retain inactive material without teaching every consumer about special archive folder names.

References are never included in book output unless an exporter is explicitly asked to render them.

---

## 14. YAML rules

All protocol YAML follows these rules:

* UTF-8
* Lowercase snake_case keys
* Case-sensitive keys
* Duplicate keys are invalid
* Unknown top-level protocol keys are invalid
* Custom data belongs under `extensions`
* Empty strings should normally be represented as `null` or omitted
* Paths are never stored in YAML
* IDs are never stored in YAML

Example extension:

```yaml
extensions:
  novolis.audio:
    narrator: en-US-AriaNeural
  novolis.metrics:
    count_dialogue: true
```

Reverse-domain or product-prefixed extension keys prevent accidental collisions.

The current `BookYaml` implementation uses an untyped dictionary and ignores unmatched properties. NMP/1 should instead deserialize typed protocol DTOs and report unknown fields, preventing misspellings from quietly becoming dead metadata.

The existing chapter YAML parser is also effectively a line-based scalar parser, so it cannot correctly represent lists such as multiple locations or characters. NMP/1 should parse chapter front matter with the same YAML parser used for entity files.

---

## 15. Catalog model

The public domain model should be immutable and typed.

```csharp
public sealed record ManuscriptCatalog(
    IReadOnlyList<FictionUniverse> Fiction,
    IReadOnlyList<NonFictionSubject> NonFiction);

public sealed record FictionUniverse(
    string Id,
    UniverseMetadata Metadata,
    IReadOnlyList<ManuscriptSeries> Series,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

public sealed record NonFictionSubject(
    string Id,
    SubjectMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

public sealed record ManuscriptSeries(
    string Id,
    SeriesMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

public sealed record ManuscriptBook(
    ManuscriptAddress Address,
    BookMetadata Metadata,
    IReadOnlyList<ManuscriptDocument> Chapters,
    IReadOnlyList<ManuscriptDocument> Appendices,
    IReadOnlyList<ReferenceDocument> References);

public sealed record ManuscriptDocument(
    string Slug,
    int Order,
    string Title,
    ManuscriptDocumentKind Kind,
    string FilePath,
    ChapterMetadata Metadata);
```

A book address contains its structural identity:

```csharp
public sealed record ManuscriptAddress(
    ManuscriptKind Kind,
    string ScopeId,
    string? SeriesId,
    string BookId);
```

Where:

* `ScopeId` is the universe ID for fiction
* `ScopeId` is the subject ID for non-fiction
* `SeriesId` is null for standalone books

This replaces a model centered primarily around `SeriesInfo` and standalone books with one that represents both top-level branches explicitly. The current records are a useful foundation but only model series, books, chapters, and reference sets.

---

## 16. Loading API

Recommended public API:

```csharp
var workspace = ManuscriptWorkspace.Open(startPath);
var snapshot = workspace.Read();

var catalog = snapshot.Catalog;
var diagnostics = snapshot.Diagnostics;
```

```csharp
public sealed record ManuscriptSnapshot(
    ManuscriptCatalog Catalog,
    IReadOnlyList<ManuscriptDiagnostic> Diagnostics);
```

`Open`:

1. Normalizes the starting path.
2. Walks upward for `manuscript.yaml`.
3. Parses and validates the workspace marker.
4. Rejects unsupported protocol versions.

`Read`:

1. Enumerates the canonical tree.
2. Parses typed YAML metadata.
3. Reads document headings and front matter.
4. Resolves effective defaults.
5. Produces an immutable catalog and diagnostics.

The implementation should remain synchronous. Local directory enumeration has no meaningful asynchronous API, and wrapping it in `Task.Run` would merely paint racing stripes on a wheelbarrow.

---

## 17. Validation

Diagnostics should have stable codes.

```csharp
public sealed record ManuscriptDiagnostic(
    ManuscriptDiagnosticSeverity Severity,
    string Code,
    string Message,
    string Path);
```

### Errors

```text
NMP001 unsupported-protocol-version
NMP002 invalid-workspace-metadata
NMP003 invalid-identifier
NMP004 missing-universe-metadata
NMP005 missing-subject-metadata
NMP006 missing-series-metadata
NMP007 missing-book-metadata
NMP008 missing-book-title
NMP009 missing-chapters-directory
NMP010 duplicate-document-order
NMP011 invalid-document-filename
NMP012 missing-document-title
NMP013 invalid-yaml
NMP014 unknown-metadata-field
NMP015 path-escapes-workspace
```

### Warnings

```text
NMP101 empty-book
NMP102 empty-reference-folder
NMP103 series-book-missing-order
NMP104 duplicate-series-order
NMP105 unused-assets
NMP106 metadata-title-differs-from-heading
```

Structural authoring problems should normally produce diagnostics rather than immediate exceptions.

Exceptions remain appropriate for:

* Invalid API arguments
* Inaccessible workspace root
* Unexpected I/O failure
* Unsupported protocol version when strict loading was requested

The current doctor already establishes the useful pattern of stable diagnostic codes and severities.

---

## 18. Implementation boundaries

Suggested internal components:

```text
WorkspaceLocator
ProtocolMetadataReader
CatalogReader
DocumentReader
ReferenceReader
MetadataResolver
ProtocolValidator
```

Do not introduce repositories or service interfaces merely to wrap `Directory` and `File`.

Use real filesystem trees in tests.

An abstraction becomes justified only if a second storage mechanism actually exists, such as:

* ZIP manuscript packages
* Git object trees
* Remote document stores
* Browser-backed virtual filesystems

Until then, physical files are the protocol.

---

## 19. Legacy adapter

The existing layout should not be accepted directly by the NMP/1 reader.

Instead, provide an isolated adapter:

```text
Novolis.Markup.Manuscript.LegacyBooks
```

Conceptually:

```csharp
public sealed class LegacyBooksCatalogReader
{
    public ManuscriptSnapshot Read(string root);
}
```

It may support:

```text
content/series/<series>/books/<book>
content/books/<book>
references/
reference/
chapter_order_from_heading
chapter_sort_numeric_prefix
callout metadata
```

The adapter returns the same domain catalog as NMP/1 but does not change the NMP/1 rules.

This prevents permanent compatibility barnacles from attaching themselves to the clean protocol.

---

## 20. Migration mapping

### Existing fiction

```text
content/series/the-calypso-cycle/
```

becomes:

```text
src/Fiction/galactic-confederation/the-calypso-cycle/
```

Add:

```text
src/Fiction/galactic-confederation/universe.yaml
```

Convert:

```yaml
id: the-calypso-cycle
name: The Calypso Cycle
```

to:

```yaml
title: The Calypso Cycle
```

Move:

```text
content/series/the-calypso-cycle/references/
```

to either:

```text
src/Fiction/galactic-confederation/References/
```

for universe-wide canon, or:

```text
src/Fiction/galactic-confederation/the-calypso-cycle/References/
```

for material specific to that series.

### Existing non-fiction

```text
content/books/intro-to-programming/
```

becomes:

```text
src/NonFiction/software-engineering/intro-to-programming/
```

Add:

```text
src/NonFiction/software-engineering/subject.yaml
```

Natural-language metadata should use:

```yaml
language: en-US
```

rather than storing a programming-language version in `language`, as the current example does with `C# 13`.

Technology-specific information can live under extensions:

```yaml
extensions:
  novolis.technical:
    language: C#
    language_version: 13
    target_framework: net10.0
```

---

## 21. Protocol invariants

A conforming NMP/1 workspace guarantees:

1. `manuscript.yaml` uniquely identifies the workspace.
2. Every structural node has exactly one identifying YAML file.
3. Directory names are canonical IDs.
4. Structural relationships are never duplicated in YAML.
5. Every book contains `book.yaml` and `Chapters`.
6. Chapter and appendix order comes only from numeric filename prefixes.
7. Titles come only from the first H1.
8. References are recursively discoverable and retain their scope.
9. Renderer settings are not manuscript metadata.
10. Unknown protocol metadata cannot silently disappear.
11. Consumers can build the complete catalog without application-specific knowledge.
12. Legacy formats are handled outside the protocol reader.

---

## Reference Proposal

```markdown
# Novolis Manuscript Protocol 1

## Purpose

NMP/1 defines a deterministic filesystem and YAML contract for locating, cataloging, validating, and processing manuscript projects.

It deliberately covers:

* Workspace discovery
* Fiction and non-fiction organization
* Universes, subjects, series, and books
* Chapter and appendix ordering
* Hierarchical reference material
* Content metadata

It deliberately does **not** define:

* PDF or HTML rendering
* CSS
* Debug output
* AI/editorial policy
* Build commands
* Generated output locations
* Application-specific UI state

Those belong to consumers of the protocol, not the manuscript itself.

---

## 1. Design principles

### 1.1 Structure carries meaning

A book's kind, universe, subject, and series are inferred from its location.

Do not repeat structural information in YAML.

For example, a book inside:

```text
src/Fiction/galactic-confederation/the-calypso-cycle/calypso/
```

does not need:

```yaml
id: calypso
series: The Calypso Cycle
universe: Galactic Confederation
kind: fiction
```

Those values are already represented by the path.

### 1.2 Marker files identify node types

Optional series must not be inferred merely from directory depth.

A directory containing:

* `universe.yaml` is a universe
* `subject.yaml` is a non-fiction subject
* `series.yaml` is a series
* `book.yaml` is a book

This makes the optional series layer deterministic without introducing extra `Series/` and `Books/` container directories.

### 1.3 One canonical convention

NMP/1 has:

* One workspace marker
* One spelling for every reserved directory
* One chapter-ordering rule
* One metadata format
* No legacy aliases

In particular:

* `References`, never `Reference`
* `Chapters`, never `chapters`
* Numeric filename ordering, never an optional book setting
* YAML front matter, not a mixture of front matter, comments, and heading-derived order

Legacy compatibility should be implemented as an adapter, not allowed to seep into the protocol.

### 1.4 Metadata describes content

`book.yaml` describes the book.

It must not contain renderer switches such as:

```yaml
debug_mode: true
chapter_order_from_heading: true
```

The existing books repository uses these effectively, but they are build configuration rather than manuscript metadata.

### 1.5 IDs come from directory names

Directory names are canonical identifiers.

They use lowercase kebab-case:

```text
galactic-confederation
the-calypso-cycle
calypso
software-engineering
programming-fundamentals
```

There is no `id` field to drift out of sync with the filesystem.

---

## 2. Canonical workspace structure

```text
manuscript.yaml

src/
├── Fiction/
│   └── <universe>/
│       ├── universe.yaml
│       ├── References/
│       │   └── **/*.md
│       │
│       ├── <standalone-book>/
│       │   ├── book.yaml
│       │   ├── Chapters/
│       │   ├── Appendices/
│       │   ├── References/
│       │   └── Assets/
│       │
│       └── <series>/
│           ├── series.yaml
│           ├── References/
│           │   └── **/*.md
│           │
│           └── <book>/
│               ├── book.yaml
│               ├── Chapters/
│               ├── Appendices/
│               ├── References/
│               └── Assets/
│
└── NonFiction/
    └── <subject>/
        ├── subject.yaml
        ├── References/
        │   └── **/*.md
        │
        └── <book>/
            ├── book.yaml
            ├── Chapters/
            ├── Appendices/
            ├── References/
            └── Assets/
```

Only `Chapters` is required inside a book.

The following are optional:

* `Appendices`
* `References`
* `Assets`

A fiction universe can contain:

* Standalone books
* Series
* Both simultaneously

A non-fiction subject contains books directly. NMP/1 does not introduce non-fiction series. That can be added in a future protocol version if a concrete need appears.

---

## 3. Example structure

```text
manuscript.yaml

src/
├── Fiction/
│   └── galactic-confederation/
│       ├── universe.yaml
│       ├── References/
│       │   ├── history/
│       │   │   └── timeline.md
│       │   ├── politics/
│       │   │   └── earth-union.md
│       │   └── species/
│       │       └── humans.md
│       │
│       └── the-calypso-cycle/
│           ├── series.yaml
│           ├── References/
│           │   └── continuity/
│           │       └── series-timeline.md
│           │
│           ├── calypso/
│           │   ├── book.yaml
│           │   ├── Chapters/
│           │   │   ├── 10-the-rescue.md
│           │   │   ├── 20-first-contact.md
│           │   │   └── 30-oh-hell-no.md
│           │   ├── Appendices/
│           │   ├── References/
│           │   └── Assets/
│           │
│           └── personage/
│               ├── book.yaml
│               └── Chapters/
│
└── NonFiction/
    └── software-engineering/
        ├── subject.yaml
        ├── References/
        │   └── terminology.md
        │
        └── programming-fundamentals/
            ├── book.yaml
            ├── Chapters/
            ├── Appendices/
            └── Assets/
```

The hierarchical reference organization is intentionally inspired by the current books repository, where universe material is already divided semantically into areas such as politics, species, ships, history, and places.

---

## 4. Reserved names

Reserved names are case-sensitive:

| Name         | Meaning                                |
| ------------ | -------------------------------------- |
| `src`        | Protocol source root                   |
| `Fiction`    | Fiction branch                         |
| `NonFiction` | Non-fiction branch                     |
| `Chapters`   | Ordered primary manuscript documents   |
| `Appendices` | Ordered publishable appendix documents |
| `References` | Non-publishable supporting material    |
| `Assets`     | Book-local images and other resources  |

Authored identifiers must not use reserved names.

Fixed metadata filenames are lowercase:

```text
manuscript.yaml
universe.yaml
subject.yaml
series.yaml
book.yaml
```

This deliberate casing distinguishes protocol directories from authored IDs.

---

## 5. Workspace metadata

The workspace root is identified by `manuscript.yaml`.

```yaml
protocol: novolis.manuscript
version: 1

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

### Required fields

| Field      | Type    | Meaning                      |
| ---------- | ------- | ---------------------------- |
| `protocol` | string  | Must be `novolis.manuscript` |
| `version`  | integer | Protocol major version       |

### Optional fields

| Field      | Type   | Meaning               |
| ---------- | ------ | --------------------- |
| `defaults` | object | Default book metadata |

Workspace discovery walks upward until it finds `manuscript.yaml`.

It does not guess based on the presence of arbitrary directories. This replaces the current approach of searching for `content/series` or `content/books`.

Unsupported major versions are rejected.

---

## 6. Defaults and inheritance

The following fields may be supplied through `defaults`:

```yaml
defaults:
  authors:
    - Frank R. Haugen
  language: en-US
  rights: Copyright © Frank R. Haugen
```

Defaults may appear in:

* `manuscript.yaml`
* `universe.yaml`
* `subject.yaml`
* `series.yaml`

Effective book metadata is resolved nearest-first:

```text
book
→ series
→ universe or subject
→ workspace
```

Only values inside `defaults` are inherited.

Inheritance is replacement-based, not deep merging. For example, a book-level `authors` list replaces the inherited list completely.

Titles, descriptions, ordering, publication data, and extension data are never inherited.

---

## 7. Universe metadata

`universe.yaml` is required for every fiction universe.

```yaml
title: Galactic Confederation

description: >
  Fiction set within the Galactic Confederation continuity.

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

### Schema

| Field         | Required | Type            |
| ------------- | -------: | --------------- |
| `title`       |      yes | string          |
| `description` |       no | string          |
| `defaults`    |       no | defaults object |
| `extensions`  |       no | free-form map   |

The universe ID is the directory name.

---

## 8. Subject metadata

`subject.yaml` is required for every non-fiction subject.

```yaml
title: Software Engineering

description: >
  Books about programming, software architecture, and engineering practice.

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

Its schema is identical to `universe.yaml`.

Universe and subject remain separate domain types even though their metadata shapes happen to match.

---

## 9. Series metadata

`series.yaml` identifies a fiction series.

```yaml
title: The Calypso Cycle

description: >
  A sequence of novels following Calypso and her crew.

defaults:
  authors:
    - Frank R. Haugen
  language: en-US
```

### Schema

| Field         | Required | Type            |
| ------------- | -------: | --------------- |
| `title`       |      yes | string          |
| `description` |       no | string          |
| `defaults`    |       no | defaults object |
| `extensions`  |       no | free-form map   |

The current repository's `series.yaml` contains both `id` and `name`. NMP/1 removes that duplication: the directory supplies the ID and `title` supplies the display name.

---

## 10. Book metadata

`book.yaml` is required.

### Fiction example

```yaml
title: Calypso
subtitle: Book One
order: 1

authors:
  - Frank R. Haugen

language: en-US
description: >
  The first novel in The Calypso Cycle.

rights: Copyright © Frank R. Haugen 2025

targets:
  words: 130000
```

### Non-fiction example

```yaml
title: Programming Fundamentals with C# and .NET
subtitle: A University Introduction to Software Development

authors:
  - Frank R. Haugen

language: en-US

description: >
  A university-level introduction to programming with modern C# and .NET.

publication:
  version: 1.0.0
  isbn: null
  date: null
```

### Schema

| Field                 |         Required | Type             | Notes                        |
| --------------------- | ---------------: | ---------------- | ---------------------------- |
| `title`               |              yes | string           | Canonical title              |
| `subtitle`            |               no | string           |                              |
| `order`               | for series books | integer          | Position within the series   |
| `authors`             |               no | string array     | May be inherited             |
| `language`            |               no | string           | BCP 47 natural-language code |
| `description`         |               no | string           |                              |
| `rights`              |               no | string           | May be inherited             |
| `targets.words`       |               no | positive integer | Authoring metric             |
| `publication.version` |               no | string           | Edition or release version   |
| `publication.isbn`    |               no | string or null   |                              |
| `publication.date`    |               no | ISO date or null |                              |
| `extensions`          |               no | free-form map    | Tool-specific metadata       |

The following fields are intentionally absent:

```yaml
id:
kind:
series:
universe:
subject:
chapter_order_from_heading:
chapter_sort_numeric_prefix:
debug_mode:
```

They are either inferred structurally or belong to tools.

The current Calypso metadata repeats its series name and carries chapter-order and debug renderer switches in the same document. NMP/1 separates these concerns.

---

## 11. Chapter and appendix filenames

Documents use:

```text
<number>-<slug>.md
```

Examples:

```text
1-arrival.md
10-the-rescue.md
20-first-contact.md
113-oh-hell-no.md
```

Rules:

* Number is a positive integer.
* Slug is lowercase kebab-case.
* Order is always numeric by filename prefix.
* Order numbers must be unique within their directory.
* Chapter and appendix numbering are independent.
* Nested chapter directories are not supported in NMP/1.

Authors may leave gaps:

```text
100-opening.md
200-first-contact.md
300-aftermath.md
```

This permits later insertion without introducing decimal order numbers or additional ordering metadata.

The filename is the only ordering authority. There is no fallback to HTML comments, YAML chapter numbers, heading numbers, or lexical sort.

---

## 12. Chapter document format

The canonical chapter format is:

```markdown
---
date: "2496.349"
system: Centralis Omnis System
locations:
  - The Hub, Maintenance Corridor E-17
  - Calypso, Docking Ring 3
pov: Marsh
characters:
  - Marsh
  - Ryn
status: draft
tags:
  - maintenance-grid
---

# Oh Hell No

The message from station administration had been brief.
```

### Required content

* Exactly one initial level-one heading
* Non-empty title
* Markdown body may otherwise use the supported markup dialect

### Derived information

| Property            | Source                               |
| ------------------- | ------------------------------------ |
| Order               | Filename numeric prefix              |
| Slug                | Filename suffix                      |
| Title               | First H1                             |
| Kind                | `Chapters` or `Appendices` directory |
| Book                | Structural path                      |
| Fiction/non-fiction | Structural path                      |

The title and order are not duplicated in YAML.

### Common front-matter fields

| Field        | Type          |
| ------------ | ------------- |
| `status`     | string        |
| `tags`       | string array  |
| `extensions` | free-form map |

### Fiction fields

| Field        | Type         |
| ------------ | ------------ |
| `date`       | string       |
| `time`       | string       |
| `system`     | string       |
| `locations`  | string array |
| `pov`        | string       |
| `characters` | string array |

Dates such as `2496.349` must be quoted so YAML does not interpret them as numeric values.

The current callout convention distinguishes public location/date fields from hidden editorial fields. That remains useful as a rendering policy, but the visibility decision belongs to the renderer rather than NMP/1.

A compatibility reader may continue accepting existing callouts, but canonical NMP/1 writers should produce YAML front matter.

---

## 13. Reference protocol

`References` may exist at:

* Universe level
* Subject level
* Series level
* Book level

Reference discovery is recursive.

For a fiction book in a series, its available scopes are:

```text
Universe References
Series References
Book References
```

For standalone fiction:

```text
Universe References
Book References
```

For non-fiction:

```text
Subject References
Book References
```

References are not implicitly merged or overridden.

Every reference retains its scope as part of its catalog identity:

```text
fiction/galactic-confederation/reference/history/timeline
fiction/galactic-confederation/the-calypso-cycle/reference/continuity/series-timeline
fiction/galactic-confederation/the-calypso-cycle/calypso/reference/editorial/open-questions
```

This means two scopes may contain the same relative path without becoming the same document.

### Reference file format

```markdown
---
aliases:
  - EU Fleet
tags:
  - faction
  - military
---

# Earth Fleet

Earth Fleet is...
```

Reference front matter is optional.

The first H1 supplies the title. The relative path supplies the reference ID.

Files or directories whose names begin with `_` or `.` are excluded from catalog discovery:

```text
_archive/
_drafts/
.private/
```

This gives authors a simple way to retain inactive material without teaching every consumer about special archive folder names.

References are never included in book output unless an exporter is explicitly asked to render them.

---

## 14. YAML rules

All protocol YAML follows these rules:

* UTF-8
* Lowercase snake_case keys
* Case-sensitive keys
* Duplicate keys are invalid
* Unknown top-level protocol keys are invalid
* Custom data belongs under `extensions`
* Empty strings should normally be represented as `null` or omitted
* Paths are never stored in YAML
* IDs are never stored in YAML

Example extension:

```yaml
extensions:
  novolis.audio:
    narrator: en-US-AriaNeural
  novolis.metrics:
    count_dialogue: true
```

Reverse-domain or product-prefixed extension keys prevent accidental collisions.

The current `BookYaml` implementation uses an untyped dictionary and ignores unmatched properties. NMP/1 should instead deserialize typed protocol DTOs and report unknown fields, preventing misspellings from quietly becoming dead metadata.

The existing chapter YAML parser is also effectively a line-based scalar parser, so it cannot correctly represent lists such as multiple locations or characters. NMP/1 should parse chapter front matter with the same YAML parser used for entity files.

---

## 15. Catalog model

The public domain model should be immutable and typed.

```csharp
public sealed record ManuscriptCatalog(
    IReadOnlyList<FictionUniverse> Fiction,
    IReadOnlyList<NonFictionSubject> NonFiction);

public sealed record FictionUniverse(
    string Id,
    UniverseMetadata Metadata,
    IReadOnlyList<ManuscriptSeries> Series,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

public sealed record NonFictionSubject(
    string Id,
    SubjectMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

public sealed record ManuscriptSeries(
    string Id,
    SeriesMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

public sealed record ManuscriptBook(
    ManuscriptAddress Address,
    BookMetadata Metadata,
    IReadOnlyList<ManuscriptDocument> Chapters,
    IReadOnlyList<ManuscriptDocument> Appendices,
    IReadOnlyList<ReferenceDocument> References);

public sealed record ManuscriptDocument(
    string Slug,
    int Order,
    string Title,
    ManuscriptDocumentKind Kind,
    string FilePath,
    ChapterMetadata Metadata);
```

A book address contains its structural identity:

```csharp
public sealed record ManuscriptAddress(
    ManuscriptKind Kind,
    string ScopeId,
    string? SeriesId,
    string BookId);
```

Where:

* `ScopeId` is the universe ID for fiction
* `ScopeId` is the subject ID for non-fiction
* `SeriesId` is null for standalone books

This replaces a model centered primarily around `SeriesInfo` and standalone books with one that represents both top-level branches explicitly. The current records are a useful foundation but only model series, books, chapters, and reference sets.

---

## 16. Loading API

Recommended public API:

```csharp
var workspace = ManuscriptWorkspace.Open(startPath);
var snapshot = workspace.Read();

var catalog = snapshot.Catalog;
var diagnostics = snapshot.Diagnostics;
```

```csharp
public sealed record ManuscriptSnapshot(
    ManuscriptCatalog Catalog,
    IReadOnlyList<ManuscriptDiagnostic> Diagnostics);
```

`Open`:

1. Normalizes the starting path.
2. Walks upward for `manuscript.yaml`.
3. Parses and validates the workspace marker.
4. Rejects unsupported protocol versions.

`Read`:

1. Enumerates the canonical tree.
2. Parses typed YAML metadata.
3. Reads document headings and front matter.
4. Resolves effective defaults.
5. Produces an immutable catalog and diagnostics.

The implementation should remain synchronous. Local directory enumeration has no meaningful asynchronous API, and wrapping it in `Task.Run` would merely paint racing stripes on a wheelbarrow.

---

## 17. Validation

Diagnostics should have stable codes.

```csharp
public sealed record ManuscriptDiagnostic(
    ManuscriptDiagnosticSeverity Severity,
    string Code,
    string Message,
    string Path);
```

### Errors

```text
NMP001 unsupported-protocol-version
NMP002 invalid-workspace-metadata
NMP003 invalid-identifier
NMP004 missing-universe-metadata
NMP005 missing-subject-metadata
NMP006 missing-series-metadata
NMP007 missing-book-metadata
NMP008 missing-book-title
NMP009 missing-chapters-directory
NMP010 duplicate-document-order
NMP011 invalid-document-filename
NMP012 missing-document-title
NMP013 invalid-yaml
NMP014 unknown-metadata-field
NMP015 path-escapes-workspace
```

### Warnings

```text
NMP101 empty-book
NMP102 empty-reference-folder
NMP103 series-book-missing-order
NMP104 duplicate-series-order
NMP105 unused-assets
NMP106 metadata-title-differs-from-heading
```

Structural authoring problems should normally produce diagnostics rather than immediate exceptions.

Exceptions remain appropriate for:

* Invalid API arguments
* Inaccessible workspace root
* Unexpected I/O failure
* Unsupported protocol version when strict loading was requested

The current doctor already establishes the useful pattern of stable diagnostic codes and severities.

---

## 18. Implementation boundaries

Suggested internal components:

```text
WorkspaceLocator
ProtocolMetadataReader
CatalogReader
DocumentReader
ReferenceReader
MetadataResolver
ProtocolValidator
```

Do not introduce repositories or service interfaces merely to wrap `Directory` and `File`.

Use real filesystem trees in tests.

An abstraction becomes justified only if a second storage mechanism actually exists, such as:

* ZIP manuscript packages
* Git object trees
* Remote document stores
* Browser-backed virtual filesystems

Until then, physical files are the protocol.

---

## 19. Legacy adapter

The existing layout should not be accepted directly by the NMP/1 reader.

Instead, provide an isolated adapter:

```text
Novolis.Markup.Manuscript.LegacyBooks
```

Conceptually:

```csharp
public sealed class LegacyBooksCatalogReader
{
    public ManuscriptSnapshot Read(string root);
}
```

It may support:

```text
content/series/<series>/books/<book>
content/books/<book>
references/
reference/
chapter_order_from_heading
chapter_sort_numeric_prefix
callout metadata
```

The adapter returns the same domain catalog as NMP/1 but does not change the NMP/1 rules.

This prevents permanent compatibility barnacles from attaching themselves to the clean protocol.

---

## 20. Migration mapping

### Existing fiction

```text
content/series/the-calypso-cycle/
```

becomes:

```text
src/Fiction/galactic-confederation/the-calypso-cycle/
```

Add:

```text
src/Fiction/galactic-confederation/universe.yaml
```

Convert:

```yaml
id: the-calypso-cycle
name: The Calypso Cycle
```

to:

```yaml
title: The Calypso Cycle
```

Move:

```text
content/series/the-calypso-cycle/references/
```

to either:

```text
src/Fiction/galactic-confederation/References/
```

for universe-wide canon, or:

```text
src/Fiction/galactic-confederation/the-calypso-cycle/References/
```

for material specific to that series.

### Existing non-fiction

```text
content/books/intro-to-programming/
```

becomes:

```text
src/NonFiction/software-engineering/intro-to-programming/
```

Add:

```text
src/NonFiction/software-engineering/subject.yaml
```

Natural-language metadata should use:

```yaml
language: en-US
```

rather than storing a programming-language version in `language`, as the current example does with `C# 13`.

Technology-specific information can live under extensions:

```yaml
extensions:
  novolis.technical:
    language: C#
    language_version: 13
    target_framework: net10.0
```

---

## 21. Protocol invariants

A conforming NMP/1 workspace guarantees:

1. `manuscript.yaml` uniquely identifies the workspace.
2. Every structural node has exactly one identifying YAML file.
3. Directory names are canonical IDs.
4. Structural relationships are never duplicated in YAML.
5. Every book contains `book.yaml` and `Chapters`.
6. Chapter and appendix order comes only from numeric filename prefixes.
7. Titles come only from the first H1.
8. References are recursively discoverable and retain their scope.
9. Renderer settings are not manuscript metadata.
10. Unknown protocol metadata cannot silently disappear.
11. Consumers can build the complete catalog without application-specific knowledge.
12. Legacy formats are handled outside the protocol reader.

---

## Recommendation

Adopt the structure above as **NMP/1** and implement it as a new typed reader rather than incrementally expanding the current `content/series` catalog.

Keep these concepts from the current implementation:

* Markdown-per-chapter
* `book.yaml` and container metadata
* Appendices
* Hierarchical references
* Catalog records
* Structural diagnostics

Replace these concepts:

* Folder-presence workspace detection
* Untyped YAML dictionaries
* Singular/plural folder aliases
* Multiple chapter-order mechanisms
* Renderer flags in `book.yaml`
* String-only chapter metadata
* Series as the highest fiction scope

The protocol should be boring in the best sense: one marker, one tree, one order rule, one metadata system. Everything else can bloom above it without turning the roots into spaghetti.

```

## Recommendation

Adopt the structure above as **NMP/1** and implement it as a new typed reader rather than incrementally expanding the current `content/series` catalog.

Keep these concepts from the current implementation:

* Markdown-per-chapter
* `book.yaml` and container metadata
* Appendices
* Hierarchical references
* Catalog records
* Structural diagnostics

Replace these concepts:

* Folder-presence workspace detection
* Untyped YAML dictionaries
* Singular/plural folder aliases
* Multiple chapter-order mechanisms
* Renderer flags in `book.yaml`
* String-only chapter metadata
* Series as the highest fiction scope

The protocol should be boring in the best sense: one marker, one tree, one order rule, one metadata system. Everything else can bloom above it without turning the roots into spaghetti.


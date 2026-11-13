# RDF (Report Definition Format) Schema Reference

> **Companion docs:** [`Dabbler.Reports.Core/README.md`](../../src/Libs/Dabbler.Reports.Core/README.md),
> [`platform-overview.md`](../platform-overview.md) § Reports.
> **Purpose:** Document the JSON-based report-definition schema used
> by `Dabbler.Reports.Core`'s `RdfParser`, `ReportDataEngine`, and
> `BandLayoutEngine`. One canonical worked example follows in §6.

This doc is the **schema reference** — what fields exist on a report
definition and how they compose. It is not an authoring tutorial; a
designer + sample library remains deferred per ledger §11 / Session 4
G9.

Where a property type is naming a discriminated union (e.g., `Element`
has `text`, `image`, `line`, `rectangle`, `aggregate`, `subreport`
variants), each variant's distinct properties are listed under that
variant's subsection.

---

## 1. Top-level structure

An RDF document is a JSON object with the following top-level keys:

```jsonc
{
  "$schema": "https://…/rdf.schema.json",   // optional; informational only
  "schemaVersion": "1.0",                   // string; current value "1.0"
  "report": {                                // required; the report definition
    "name": "sales-summary",                // required; stable identifier
    "title": "Sales Summary",               // required; human-readable title
    "description": "…",                     // optional
    "pageSize": "Letter",                   // "Letter" | "Legal" | "A4" | …
    "orientation": "Portrait",              // "Portrait" | "Landscape"
    "margins":      { … },                  // see §1.1
    "dataSource":   { … },                  // see §2
    "parameters":   [ … ],                  // see §3
    "styles":       [ … ],                  // see §4
    "grouping":     [ … ],                  // see §5
    "bands": {                              // see §5
      "reportHeader":  { … },
      "reportFooter":  { … },
      "pageHeader":    { … },
      "pageFooter":    { … },
      "detail":        { … },
      "groupHeaders":  [ { … } ],
      "groupFooters":  [ { … } ]
    },
    "subreports":   [ … ]                   // optional; see §5.5
  }
}
```

### 1.1 Margins

```jsonc
"margins": {
  "topInches":    0.5,
  "bottomInches": 0.5,
  "leftInches":   0.75,
  "rightInches":  0.75
}
```

All four margins are required when `margins` is present; the parser
applies platform defaults when `margins` is omitted.

---

## 2. Data source

The `dataSource` object names the data provider and the query it runs.

```jsonc
"dataSource": {
  "providerKey": "OrdersReportProvider",    // resolves to an IReportDataProvider implementation
  "query":       "GetSalesByMonth",         // optional; provider-defined query identifier
  "parameters":  ["Year", "Region"]         // optional; names of parameters to forward
}
```

The `ReportDataEngine` resolves `providerKey` against the DI-registered
`IReportDataProvider` whose `Key` property matches, then invokes
`ExecuteAsync(report, parameterValues)`. The provider is responsible
for shaping the returned rows.

---

## 3. Parameters

User-supplied values prompted before the report runs.

```jsonc
"parameters": [
  {
    "name":          "Year",
    "displayName":   "Year",
    "type":          "Int32",                 // see §3.1
    "isRequired":    true,
    "defaultValue":  2026,
    "allowedValues": [2024, 2025, 2026]       // optional; renders as a dropdown when present
  }
]
```

### 3.1 Parameter types

| `type` value | Editor surface (`ParameterPromptForm`) |
|--------------|-----------------------------------------|
| `String`     | `MudTextField` |
| `Int32`      | `MudNumericField<int>` |
| `Decimal`    | `MudNumericField<decimal>` |
| `DateTime`   | `MudDatePicker` |
| `Boolean`    | `MudSwitch` |

Custom parameter types are not supported in the current schema; this
list is closed.

---

## 4. Styles

Named style definitions referenced by elements.

```jsonc
"styles": [
  {
    "name":        "header-style",
    "fontFamily":  "Arial",
    "fontSize":    12,
    "fontBold":    true,
    "fontItalic":  false,
    "color":       "#000000",
    "background":  "#FFFFFF",
    "textAlign":   "Left",                      // "Left" | "Center" | "Right"
    "padding":     { "top": 2, "left": 2, "bottom": 2, "right": 2 },
    "border":      { "color": "#000000", "thickness": 1, "sides": ["Bottom"] }
  }
]
```

Element references a style by name: `"styleName": "header-style"`.
Styles compose with element-level overrides (any property set on the
element wins over the style).

---

## 5. Bands and elements

A report renders by band. Bands are vertical sections of the page;
elements (text, images, aggregates, etc.) live inside bands at
positioned coordinates.

### 5.1 Band kinds

| Band | When it renders |
|------|------------------|
| `reportHeader` | Once at the start of the report. |
| `reportFooter` | Once at the end of the report. |
| `pageHeader`   | At the top of every page. |
| `pageFooter`   | At the bottom of every page. |
| `detail`       | Once per data row. |
| `groupHeaders[]` | Once at the start of each group at the matching level. |
| `groupFooters[]` | Once at the end of each group at the matching level. |

### 5.2 Band structure

```jsonc
{
  "heightInches": 0.5,
  "elements": [ … ],                          // see §5.3
  "canGrow":      true,                       // optional; allows the band to expand for taller content
  "canShrink":    false,                      // optional; allows the band to contract when content is shorter
  "printWhen":    "@Region == 'East'"         // optional; expression gating render
}
```

### 5.3 Elements

Every element shares positional properties:

```jsonc
{
  "type":      "text",                        // see §5.3.1 for the type discriminator
  "left":      0.5,                           // inches from band's left margin
  "top":       0.0,                           // inches from band's top edge
  "width":     2.0,                           // inches
  "height":    0.25,                          // inches
  "styleName": "header-style",                // optional
  "printWhen": "@Region != null"              // optional; expression gating render
  // … type-specific properties
}
```

#### 5.3.1 Type discriminator

| `type` | Type-specific properties |
|--------|---------------------------|
| `text` | `text`: literal or `=@FieldName`; `format`: optional format string (`"C2"`, `"d"`, `"N0"`); `canGrow`/`canShrink` for cell-level reflow. |
| `image` | `imagePath` (relative to RDF location) or `imageData` (base64 inline); `scaling`: `"Stretch"` \| `"Uniform"` \| `"None"`. |
| `line` | `lineColor`, `lineThickness`, `lineStyle` (`"Solid"` \| `"Dashed"` \| `"Dotted"`); positional `left`+`top`+`width`+`height` define endpoints. |
| `rectangle` | `fillColor`, `borderColor`, `borderThickness`, `cornerRadius`. |
| `aggregate` | `aggregateType` (`"Sum"` \| `"Average"` \| `"Count"` \| `"Min"` \| `"Max"`); `aggregateField` (field name); `scope` (`"Detail"` \| `"Group:<groupName>"` \| `"Report"`); `runningTotal` (boolean — accumulate across detail rows or reset per group). |
| `subreport` | `subreportName` (matches `subreports[].name`); `parameterBindings`: object mapping subreport parameter names to expressions (`{"Year": "@Year"}`). |

#### 5.3.2 Hide duplicates

Detail-band text elements support a `hideDuplicates` boolean that
suppresses the value when the same value rendered in the previous
detail row. Useful for columns where consecutive rows share a value
(e.g., "Region" when sorted by region).

### 5.4 Grouping

```jsonc
"grouping": [
  {
    "name":     "ByRegion",
    "field":    "Region",
    "sort":     "Ascending",                    // "Ascending" | "Descending"
    "keepTogether": true                        // optional; tries to keep the group on one page
  },
  {
    "name":     "ByMonth",
    "field":    "OrderMonth",
    "sort":     "Ascending"
  }
]
```

Groups nest in declaration order — the first group is the outermost.
Group headers and footers reference groups by their `name`:

```jsonc
"groupHeaders": [
  { "groupName": "ByRegion", "heightInches": 0.4, "elements": [ … ] },
  { "groupName": "ByMonth",  "heightInches": 0.3, "elements": [ … ] }
]
```

### 5.5 Subreports

```jsonc
"subreports": [
  {
    "name":       "OrderDetails",
    "rdfPath":    "subreports/order-details.rdf.json",   // sibling RDF, resolved relative to parent
    "dataSource": { "providerKey": "OrderDetailsProvider", "query": "ForOrder" }
  }
]
```

A subreport element (`type: "subreport"`) in the parent's `detail` band
runs the named subreport once per parent row, passing parameter
bindings as input.

---

## 6. Worked example: one-page sales-summary

A single-page RDF that demonstrates banded layout (report header / page
footer / detail / group header / group footer), a grouped column with
running totals, and a footer aggregate.

```jsonc
{
  "schemaVersion": "1.0",
  "report": {
    "name":  "sales-by-region",
    "title": "Sales By Region",
    "pageSize":    "Letter",
    "orientation": "Portrait",
    "margins":     { "topInches": 0.5, "bottomInches": 0.5, "leftInches": 0.75, "rightInches": 0.75 },
    "dataSource":  { "providerKey": "SalesReportProvider", "query": "ByRegion" },
    "parameters":  [
      { "name": "Year", "displayName": "Year", "type": "Int32", "isRequired": true, "defaultValue": 2026 }
    ],
    "styles": [
      { "name": "title",  "fontFamily": "Arial", "fontSize": 16, "fontBold": true, "textAlign": "Center" },
      { "name": "header", "fontFamily": "Arial", "fontSize": 11, "fontBold": true,
        "border": { "color": "#000000", "thickness": 1, "sides": ["Bottom"] } },
      { "name": "cell",   "fontFamily": "Arial", "fontSize": 10 },
      { "name": "total",  "fontFamily": "Arial", "fontSize": 10, "fontBold": true,
        "textAlign": "Right" }
    ],
    "grouping": [
      { "name": "ByRegion", "field": "Region", "sort": "Ascending" }
    ],
    "bands": {
      "reportHeader": {
        "heightInches": 0.6,
        "elements": [
          { "type": "text", "left": 0, "top": 0.1, "width": 7, "height": 0.3,
            "styleName": "title", "text": "Sales By Region — @Year" }
        ]
      },
      "groupHeaders": [
        {
          "groupName": "ByRegion",
          "heightInches": 0.35,
          "elements": [
            { "type": "text", "left": 0,   "top": 0.05, "width": 2, "height": 0.25,
              "styleName": "header", "text": "Region: =@Region" },
            { "type": "text", "left": 2.5, "top": 0.05, "width": 1, "height": 0.25,
              "styleName": "header", "text": "Order #" },
            { "type": "text", "left": 4,   "top": 0.05, "width": 1.5, "height": 0.25,
              "styleName": "header", "text": "Customer" },
            { "type": "text", "left": 6,   "top": 0.05, "width": 1, "height": 0.25,
              "styleName": "header", "text": "Amount" }
          ]
        }
      ],
      "detail": {
        "heightInches": 0.22,
        "elements": [
          { "type": "text", "left": 2.5, "top": 0,    "width": 1, "height": 0.2,
            "styleName": "cell", "text": "=@OrderNumber" },
          { "type": "text", "left": 4,   "top": 0,    "width": 1.5, "height": 0.2,
            "styleName": "cell", "text": "=@CustomerName" },
          { "type": "text", "left": 6,   "top": 0,    "width": 1, "height": 0.2,
            "styleName": "cell", "text": "=@Amount", "format": "C2" }
        ]
      },
      "groupFooters": [
        {
          "groupName": "ByRegion",
          "heightInches": 0.3,
          "elements": [
            { "type": "text", "left": 4, "top": 0.05, "width": 1.5, "height": 0.2,
              "styleName": "total", "text": "Subtotal:" },
            { "type": "aggregate", "left": 6, "top": 0.05, "width": 1, "height": 0.2,
              "styleName": "total", "aggregateType": "Sum",
              "aggregateField": "Amount", "scope": "Group:ByRegion", "format": "C2" }
          ]
        }
      ],
      "reportFooter": {
        "heightInches": 0.4,
        "elements": [
          { "type": "text", "left": 4, "top": 0.1, "width": 1.5, "height": 0.2,
            "styleName": "total", "text": "Grand Total:" },
          { "type": "aggregate", "left": 6, "top": 0.1, "width": 1, "height": 0.2,
            "styleName": "total", "aggregateType": "Sum",
            "aggregateField": "Amount", "scope": "Report", "format": "C2" }
        ]
      },
      "pageFooter": {
        "heightInches": 0.3,
        "elements": [
          { "type": "text", "left": 6, "top": 0.05, "width": 1, "height": 0.2,
            "styleName": "cell", "text": "Page =@PageNumber of =@PageCount", "textAlign": "Right" }
        ]
      }
    }
  }
}
```

### What this RDF demonstrates

- **Bands** — report header, page footer, detail, group header,
  group footer.
- **Grouping** — one-level grouping by `Region`.
- **Aggregates** — group-scoped subtotal (`scope: "Group:ByRegion"`),
  report-scoped grand total (`scope: "Report"`).
- **Parameter binding** — `@Year` in the report header text resolves
  to the parameter value the user supplied.
- **Field binding** — `=@Region`, `=@OrderNumber`, `=@CustomerName`,
  `=@Amount` resolve per detail row from the data source.
- **Format strings** — `"C2"` on `Amount` renders currency with two
  decimals respecting the active culture.
- **Built-in expressions** — `=@PageNumber`, `=@PageCount` are
  evaluator-provided expressions resolved by the layout engine.

### Footer-aggregate alignment rule

Per
[`docs/planning/lessons-learned.md`](../planning/lessons-learned.md) →
*Report Footer Totals Must Align Under Their Detail Column*, the
`Amount` aggregate in both the group footer and the report footer is
positioned at `left: 6, width: 1` — identical to the detail-band
`Amount` element. The label ("Subtotal:", "Grand Total:") goes in a
separate text element to the left. **Do not** position the aggregate
mid-column and add a descriptor label next to it; column alignment
matters for readability.

---

## 7. What is not in the schema

- **Conditional formatting per row.** Use `printWhen` on a duplicate
  element to render only when a condition is met. Cell-level
  conditional formatting (e.g., "color the cell red if value < 0")
  is not currently supported.
- **Charts.** No native chart elements. Render a chart server-side and
  embed as an `image` element.
- **Designer / authoring tool.** RDFs are authored by hand or by code
  generators outside the platform. The designer + sample library
  remain deferred per ledger §11 / Session 4 G9.
- **External data binding (e.g., REST URLs).** Data sources resolve to
  `IReportDataProvider` registrations — bring REST data through your
  own provider implementation.

---

## 8. Sample RDFs in this repo

For working examples beyond the worked one above, see the
demonstration reports under
`src/Apps/AcmeCoffeeOnline.Web/Reports/` in the demo app:

- `orders-by-customer.rdf.json` — two-level grouping, currency
  aggregates, page footer with running totals.
- `sales-by-product.rdf.json` — subreport composition, product-image
  embedding.
- `inventory-status.rdf.json` — `printWhen` expressions for
  conditional row rendering.

These ship with the demo app and are not part of the platform package
surface.

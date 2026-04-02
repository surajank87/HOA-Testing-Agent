# Test Data Generator Skill

## Purpose
Reads the HOA requirement document and generates a state-specific test data Excel workbook.

## Input
- HOA requirement document: `carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx`
- Target state code (e.g., AZ, IL, FL)

## Output
- `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`

---

## CRITICAL RULES

### Rule 1: Only include fields with valid XPath
A field is carrier-relevant ONLY if its XPath column (Column N) has a real XPath value (e.g., `/root/...`).
- **Blank XPath** → NOT carrier-specific → EXCLUDE from test data
- **"N/A"** → NOT carrier-specific → EXCLUDE from test data
- **"Do not send" / "don't map"** → NOT carrier-specific → EXCLUDE from test data
- **"None" as XPath** → EXCLUDE (this means no mapping, not the string "None" as a value)
- **Valid XPath starting with `/root/...`** → INCLUDE

This means out of ~170 Interview fields, only ~52 with real XPaths go into the test data.

### Rule 2: Keep the SAME sheet structure as the requirement document
Do NOT split Interview rows into per-blind sheets. The output Excel must mirror the requirement document's sheet structure:

| Requirement Doc Sheet | Test Data Sheet | What to do |
|---|---|---|
| Interview | Interview | Filter: only rows with valid XPath + relevant for target state |
| Lists | Lists | Filter: only values for relevant fields + Carrier States filter |
| Defaults and Extras | Defaults and Extras | Filter: only state-applicable defaults |
| UUD | UUD | Copy as-is (always relevant) |
| Result Page Coverages | Result Page Coverages | Filter: by Bolt States for target state |
| Result Page Details | Result Page Details | Copy as-is (no state filter) |
| Scope | (omit) | Already verified, not needed in output |
| Updates Log | (omit) | Not needed in output |
| API Setup | (omit) | Not needed in output |

### Rule 3: Preserve formatting and add hyperlinks
- **Copy the formatting style from the requirement document** — colors, background-color, fonts, borders, column widths, row heights
- **Hyperlinks from List column:** In the Interview sheet, if a field has a `List` column value (e.g., "ArchitectureStyleList"), create a hyperlink in that cell that navigates to the corresponding group in the Lists sheet. This lets the user click a list name and jump directly to its values.
- **Freeze panes** as in the original document
- **Auto-filter** on header rows

---

## Process

### Step 1: Verify State Scope
Read the `Scope` sheet. Confirm the target state code exists in Column B. If not found, STOP and inform the user.

### Step 2: Read and Filter Interview Sheet

Read all rows from the Interview sheet. A row is included in the output ONLY if it passes BOTH checks:

**Check 1 — Valid XPath (Column N):**
```python
def has_valid_xpath(xpath_value):
    if xpath_value is None:
        return False
    xp = str(xpath_value).strip()
    if xp == '' or xp == 'None' or xp.lower() in ['n/a', 'na', 'do not send', "don't map", 'do not map']:
        return False
    # Must look like an actual XPath
    return xp.startswith('/root/')
```

**Check 2 — State relevance (Column G):**
```python
def is_relevant_for_state(state_value, target_state):
    if state_value is None or str(state_value).strip() == '' or str(state_value).strip() == 'None':
        return True  # Blank = all states
    
    sv = str(state_value).strip()
    
    # "NOT USED FOR VA" pattern
    if sv.upper().startswith('NOT USED FOR '):
        excluded = sv.upper().replace('NOT USED FOR ', '').strip()
        return target_state.upper() not in [s.strip() for s in excluded.split(',')]
    
    # "Not in MO" / "not in FL" pattern
    if sv.lower().startswith('not in '):
        excluded = sv[7:].strip()
        return target_state.upper() not in [s.strip().upper() for s in excluded.split(',')]
    
    # "state AZ, VA" / "state AZ & IL" pattern
    if sv.lower().startswith('state '):
        states_str = sv[6:].replace('&', ',').strip()
        included = [s.strip().upper() for s in states_str.split(',')]
        return target_state.upper() in included
    
    # Plain state codes: "FL", "CA"
    included = [s.strip().upper() for s in sv.split(',')]
    return target_state.upper() in included
```

**Include row if:** `has_valid_xpath(row.XPath) AND is_relevant_for_state(row.State, target)`

Track which `List` names are referenced by included fields — this drives Lists filtering.

### Step 3: Read and Filter Lists Sheet

For each row in the Lists sheet:

1. **Check if the parent field is relevant** — the List Name must be referenced by an included Interview field (from Step 2). If the parent field was excluded, exclude the entire list.

2. **Filter values by Carrier States** (Column F):
```python
def is_value_for_state(carrier_states, target_state):
    if carrier_states is None or str(carrier_states).strip() == '' or str(carrier_states).strip() == 'None':
        return True  # Blank = all states
    
    cs = str(carrier_states).strip()
    
    # "Not in TX" / "not in TX" pattern
    if cs.lower().startswith('not in '):
        excluded = cs[7:].strip()
        return target_state.upper() not in [s.strip().upper() for s in excluded.split(',')]
    
    # Plain state codes
    included = [s.strip().upper() for s in cs.replace(' ', '').split(',')]
    return target_state.upper() in included
```

3. **Bolt Condition** (Column D) — used for occupation sublists. Include the condition in the output for reference but do NOT exclude the value.

### Step 4: Read Defaults and Extras

Read all rows. Filter by the `Condition` column (Column B) for state applicability:
- `True` or blank → all states → INCLUDE
- `"TRUE\nnot used for state VA"` → exclude for VA
- `"if state is IL..."` → only for IL
- `"if state is VA, AZ"` → only for VA and AZ
- `"If PersonalLineReplacementCost..."` → general condition, include for all states
- `"All states"` → INCLUDE

### Step 5: Copy UUD Sheet
Copy the UUD sheet as-is. It contains ineligibility rules that apply to all states:
1. PLConstructionType in {Log, Asbestos, EFIS} → INELIGIBLE
2. PLNumberOfStories in {3.5, 4} → INELIGIBLE

### Step 6: Read Result Page Coverages and Details
- **Result Page Coverages:** Filter by `Bolt States` column (Column C) using the same state parsing logic
- **Result Page Details:** Include all rows (no state filter)

### Step 7: Generate Excel Workbook

Create workbook with these sheets (in this order):
1. **Interview** — filtered rows with valid XPath + state-relevant
2. **Lists** — filtered values (field-relevant + state-relevant)
3. **Defaults and Extras** — state-filtered defaults
4. **UUD** — copied as-is
5. **Result Page Coverages** — state-filtered
6. **Result Page Details** — all rows

**For each sheet:**
- Keep ALL original columns from the requirement document
- Add ONE column at the end: `Reference` — format: `(Sheet: {SheetName}, Row: {original_row_number})`
- Preserve the formatting style from the requirement document (header colors, fonts, borders, column widths)

### Step 8: Add Hyperlinks (List Column → Lists Sheet)

In the Interview sheet output, for each row that has a `List` column value:
1. Find the first row in the Lists output sheet where `List Name` matches
2. Create an internal hyperlink in the Interview cell pointing to that Lists row
3. Format: `=HYPERLINK("#Lists!A{row}", "{ListName}")` or use openpyxl's hyperlink feature
4. Style the hyperlinked cell with blue underlined font (standard hyperlink style)

This enables click-to-navigate from any Interview field to its list of valid values.

### Step 9: Apply Formatting

- **Header rows:** Match the requirement document's header style (colors, bold, borders)
- **Data cells:** Borders on all cells, appropriate column widths
- **Freeze panes:** Freeze the header row (row 1)
- **Auto-filter:** Enable auto-filter on header rows
- **Group list values:** In the Lists sheet, visually group rows by List Name (alternating background or separator rows)
- **Hyperlink style:** Blue underlined font for List column cells that link to Lists sheet

# Architecture Considerations

## Database

- SQL Server
- Use Temporal Tables for automatic auditing
- Unless this is hard to do, use a separate "history" schema for the history tables and make the table name the same

---

## Data Models

- All entities have a surrogate primary key named Id
- All entities have OrganizationId, UserId, SysStart (temporal period from), SysEnd (temporal period to)

---

## REST Service Layer

- All communication with the database happens through the REST Service Layer
- The API is exposed via Swagger or equivalent (there are some modern alternatives)
- The API exists in a separate project so that it can be deployed to a separate web server
- Uses Entity Framework Core for data persistence and querying
- Note: Delete operations are always preceded by an update operation with the same OrganizationId and UserId in order to capture the deleting user in the temporal history table
- .NET 10 (LTS), until the next LTS becomes available, then ask human

---

## Web Application Layer

- Implemented as Blazor Server application
- .NET 10 (LTS), until the next LTS becomes available, then ask human

---

## Accessibility

- All reusable UI components and application pages should conform to Web
  Content Accessibility Guidelines (WCAG) Version 2.1, Level AA
- Accessibility verification should happen before documentation-focused session
  work begins

---

## Security

### Authentication

- For internal users, implemented as SSO using Entra ID
- For external users, implemented as OAuth and/or TOTP (but open to simpler options)

### Authorization

- Runtime system for assigning ...
    - users to roles
    - API capabilities to roles
    - UI control functionality to roles
    - Page access to roles
- Organization and Role switching capability
    - System admins can switch roles and organizations without limit
    - Other users can switch organizations if they are associated with multiple organizations
- Predefined Roles
    - User (by default ...)
        - Can enter new data records
        - Can edit any data record tagged to their assigned (and active, if multiple) organization
        - Can delete records if the role allows
    - User Admin
        - Can do anything a User can do
        - Can associate users with their (active) Organization
        - Can associate users with any role, except System Admin
        - Can deactivate users in their (active) Organization
        - Can provide organization-specific registration link to new users
    - System Admin
        - Can associate users with any Organization
        - Can associate users with any Role
        - Can deactivate any user
        - Can add, update, or deactivate any value in a reference entity (e.g. product)
        - Can provide organization-specific registration link to new users        
- Look at existing Security capabilities for more information

---

## Testing

- All functionality that can be strictly unit tested should be strictly unit tested
- When methods contain one or more sections that could be unit tested and one or more other sections that can only be integration tested, those methods should be split into unit-testable and non-unit-testable methods.
- Any method that is not strictly unit testable but is integration testable should be integrate tested
- Accessibility conformance should be checked with automated tooling plus
  focused manual validation of keyboard navigation, visible focus behavior,
  labeling, and other WCAG 2.1 Level AA expectations
- All UI functionality should be end-to-end (E2E) tested, where E2E is fully E2E:
    - User actions in the UI that should impact the database are evaluated by examining relevant data in the database  
    - E2E should also test validation checks and navigation between controls (tab order) and navigation to pages (with potential parameters)
    - E2E tests should utilize Docker containers (alternatively, we could do a research spike on .NET Aspire)
    
---

## Documentation

- All classes and methods (except model classes with no additional functonality) should have documentation comments
- The application should have both technical documentation and user documentation
- There should be a README.md file that gives an overview of the solution 

---

# Grids

## DabblerFilterGrid

- Top has filter grid with single row of same columns as Result (Data) Grid
- Additional rows can be added
- The logic is similar to an Access QBE grid:
    - Within rows, criteria are treated as ANDs
    - Across rows, criteria are treated as ORs
    - e.g., (R1C1_Value >= 3 AND R1C2_Value Contains "AB") OR (R2C2_Value == True AND R2C3_Value Between "1/1/2024" and "3/31/2024")
    - Note a status text area below the Filter Grid displays the filter conditions in English

---

## DabblerDataGrid

- At the top of each Data Grid is an Add button for adding records 
- It is helpful to be able to edit inline, but we can defer this functionality
    - If the grid has inline edit capabilities, it must support all of these features:
        - Column templates for Checkbox, DabblerComboBox, Date Picker
        - Default values for columns
        - Support for validation rules
        - Clicking the Add button creates a row at the bottom of the grid and takes the user to the first editable column
        - (Not sure if input masks are really needed if Date Picker and validation rules are present)        
- The first column always has the Id with a hyperlink
    - By default, clicking on the hyperlinked Id
        - Opens the record as a form in another tab and navigates to that tab.
    - By configuration, clicking on the hyperlinked Id can open a pop-up modal edit form (note that this is not appropriate for entities that have child entities or entities with many fields)
- All the way to the right of each row is a delete button
    - Clicking on the delete icon deletes the record after a confirmation dialog is presented and confirmed
    - Note: some roles may prevent deleting records

---

# Forms
  
## DabblerSearchForms (possibly called DabblerQueryForm, but I would rename this to avoid confusion)

- Used for many different things:
    - Search for transaction records (e.g., customers or orders)
    - Search for reference entity types (e.g., products, roles)
    - Search for reference entity records (e.g., specific products or roles)
    - Search for named reports
    - Run a report with parameters/criteria (Run Report)
    - Search for named queries
    - Run a query with parameters/criteria (Run Query)
    - Search for users    
- Users should be able to save named queries and named reports for later reuse
- Consists of Filter Grid and Data Grid
    - The Data Grid in a Search Grid mode does not have a delete button on any row
- In some cases, a search form may take a user to another search form
    - Search for named reports -> clicking on the Id link takes the user to the Run Report form
    - Search for named queries -> clicking on the Id link takes the user to the Run Query form
    - search for reference entity types -> click on the Id link takes the user to a search form for reference entity records for the selected reference entity.  This allows admins to update lookup lists.
- Query/search result workflows should support CSV export when users need to take the filtered result set into another tool or process

---

# Reports

## Report Output

- Reports should support banded output rather than only flat tabular rendering
- Reports should support grouping and sorting control in the report definition
- Reports should support group headers for grouped data presentation
- Reports should support PDF export for user-facing distribution and printing workflows
- Reports should be able to show subtotals and totals when the report definition calls for them
- Reports should support running totals where that is part of the report design
- Reports should support hiding duplicate values within a grouped output when that improves readability
- Reports should support conditional visibility and conditional formatting for common output-driven scenarios
- Reports should support saving named report definitions so users can find and rerun them later

---

## Detail Forms

- Generally have a sequence of form controls (textbox, combobox, checkbox, option group ... etc.) with a particular tab order
- Form controls are generally laid out in two-column format with labels as placemarkers that elevate to a label in the standard MudForm approach
- Have data validation rules and display feedback
- Have a Save and Cancel button
- Has a tab control with standard Data grids for each 
    - one-to-many or many-to-many child entity
    - NOTE: many-to-many child entities are treated like one-to-many child entities; however any data columns in the join table are shown along with the child entity columns in the associated row.  Save operations tease this apart and produce the correct inserts or updates.  Deletes don't delete the child entity -- just the association.  

---

## Pop-up forms

- Modal
- Default to a single column, but may have two columns
- Unlike Detail Forms, do not display child entities (normally displayed in tabbed data grids)
- Have Save and Cancel button
- Have validation rules

---

# Form Controls

## DabblerComboBox

- Supports Multiselect (as configuration)
- Supports Multiple Columns (as configuration)
- Has bound column
- Support Cascading coordination with other combobox instances

---

# Dashboards

- Configurable form that can serve as a landing page for an application
- Can be different for different roles (but probably not different users)
- See DabblerDashboardPage in the code

---

# Navigation Controls

## Top Menu

- Menu items include Dashboards, Forms, Reports, Queries, Admin 
- Submenu items under each menu include pinned items, as well as a "more" link
- Admin menu always has Users at the top of the submenu items; otherwise, it
  works the same as the other menu items 

---
 
## Left Nav Menu

- By Default, used for top-level entities or high-level workflows
- Top-level entities can be Implemented as Accordion control
    - When expanded, includes 
        - an add link for a new record
        - a search link for searching for records
        - one or more other links (e.g., most recently edited record by the user in the org)
- High-level workflows can be implemented as an Access-like switchboard with icons and labels        

---

# Demo project

The demo project should be a full-featured application that demonstrates all the capabilities of the platform.  It should be tested thoroughly and be presentation worthy

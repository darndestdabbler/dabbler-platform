# Phase 3 Platform Roadmap

## Evaluating and Refining Access Metadata Documentation Solution

1. Evaluate and potentially refine Microsoft Access metadata discovery project, including the CLI
2. Develop and test a feature that generates test data as JSON files with reference data preserved from Microsoft Access (a reference data harvester) and transactional totally data made up.

---

## Evaluating and Refining Access Migration Workflow

### Sample Access databases Used for Testing

1. Northwind Developer Edition
2. Home Health Care
3. Project Board
4. Mini Complex

---

### Draft Workflow

1. Human uses CLI to generate metadata from legacy Access database as JSON files
2. AI generates test data as JSON files
3. Human execute project template CLI to scaffold VS code solution with AI support
4. AI creates session sets for remainder of AI work
4. AI builds stubbed functionality (including pages and components) based upon Access metadata
5. AI generate Stubbed UAT Checklist (JSON file)
6. Human does Stubbed UAT (for design/navigation feedback from human)
    a. AI reads feedback and makes changes as necessary
    b. AI/human repeat steps 5 & 6 with refinements to the checklist, as needed
7. AI builds out full functionality with automated unit, integration, and bUnit tests
8. AI designs E2E workflow 
9. AI executes E2E tests and makes refinements as needed
10. AI generates Full App UAT Checklist 
11. One or more humans (perhaps developers first, then business users) do Full App UAT
    a. AI reads feedback, makes changes to source and tests as necessary, and runs all automated tests
    b. AI/human repeat steps 10 & 11 with refinements to the checklist, as needed
12. Separate AI engine does security scan
    a. Main AI engine reads security scan results, makes changes to source and tests as necessary, and runs all automated tests
    b. The two AI engines repeat 12/a until there are no remaining issues
13. AI generates documentation
14. AI generates and executes data migration code and builds and runs comprehensive tests to ensure that data migration was successful
    - Note: this is in staging environment
15. AI generates Data Migration and Documentation UAT Checklists
16. Human does Data Migration and Documentation UATs
    a. AI reads feedback, makes changes to source and tests as necessary, and re-runs all data migration
    b. AI/human repeat steps 13, 14, 15, and 16 with refinements to the checklists, as needed
  
  
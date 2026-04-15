# Example of End-To-End Mini Workflow Test

1. Open the application
2. Click on the Admin Menu item
3. Click on the Users Submenu item
4. Click on Add Button
5. Enter a new User 
    - Variation A (Happy Path): all field values are valid
    - Variation B (Valid Edge Case): enter valid field values just inside of valid value boundaries
    - Variation C (Invalid Edge Case): enter invalid field values just outside of valid value boundaries
6. Assign the new User one or two roles (in tabbed grid)
7. Save the new User
8. Check the database to ensure that new User and roles have been added and column data are correct

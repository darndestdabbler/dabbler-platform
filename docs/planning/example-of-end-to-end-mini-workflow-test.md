# Example of End-To-End Mini-Workflow Test

*Note: Each Variation below should be expanded into a full N-step mini-workflow

0. Set up the Test
    - Ensure database is reset
    - Ensure that there is a test user with User Admin role
    - Ensure API and Web application are running and that the Web application knows the port for the API 
1. Login to the application as a test user with the User Admin Role and some organization
    - Check relevant intermediate assertions
        - Login user is displayed
        - Admin menu item is visible
2. Click on the Admin Menu item
    - Check relevant intermediate assertions
        - Users submenu item is available
3. Click on the Users Submenu item
    - Check relevant intermediate assertions
        - Users Search Form opens in new browser tab
        - Test user's focus is taken to the control designated with the lowest tab order 
        - Add button is visible and above Search Form
4. Click on Add Button
    - Check relevant intermediate assertions
        - User Detail Form opens in the current browser tab (Note: we can discuss whether it make sense to do this or open in a new tab)
        - Test user's focus is taken to the control designated with the lowest tab order 
5. Enter a new User using TAB to navigate from one control to the next
    - Variation A (Happy Path): all field values are valid
        - Check relevant intermediate assertions 
            - TODO
    - Variation B (Valid Edge Case): enter valid field values just inside of valid value boundaries
        - Check relevant intermediate assertions 
            - TODO
    - Variation C (Invalid Edge Case): enter invalid field values just outside of valid value boundaries
        - Check relevant intermediate assertions 
            - TODO
6. Tab into the User Roles Grid (in a tab control)
    - Check relevant intermediate assertions 
        - TODO
7. Assign the new User one (Variation A) or two (Variation B) roles in the Data Grid
    - Check relevant intermediate assertions 
        - TODO
8. Save (Variation A) or Cancel (Variation B) the new User
    - Check relevant intermediate assertions 
        - TODO
9. Close the the User Details form tab
    - Check relevant intermediate assertions 
        - TODO 
10. Check the database to ensure that new User and roles have been added (Variation A) or not added (Variation B) and column data are correct

# Contributors guidelines

If you want to report bugs or request new features, see [sbox-issues](https://github.com/Facepunch/sbox-issues/)

## Making Changes

### Adding new features

Before you start trying to add a new feature, it should be something people want and has been discussed in a proposal issue ideally.

### Fixing bugs

If you're fixing a bug, make sure you reference any applicable bug reports, explain what the problem was and how it was solved.

Unit tests are always great where applicable.

### Guidelines

A few guidelines that will make it easier to review and merge your changes:

* **Scope**
    * Keep your pull requests in scope and avoid unnecessary changes.
* **Commits**
    * Should group relevant changes together, the message should explain concisely what it's doing, there should be a longer summary elaborating if required.
    * Remove unnecessary commits and squash commits together where appropriate.
* **Formatting**
    * Your IDE should adhere to the style set in `.editorconfig`
    * Auto formatting can be done with `dotnet format`
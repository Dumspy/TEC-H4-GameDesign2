# AGENTS.md

## Project Overview
This repository is a Unity project. The main codebase is in C# and follows standard Unity conventions.

## Build, Lint, and Test Commands
- **Build:** Use the Unity Editor to build the project. No custom build scripts are present.
- **Test:** Run tests using Unity's built-in test runner (NUnit-based). Open the Unity Editor, go to `Window > General > Test Runner`, and run PlayMode or EditMode tests as needed.
- **Lint:** No explicit linting tools or configuration files were found. Follow Unity/C# best practices.

## Code Style Guidelines
- **Naming:** Use PascalCase for class names and methods. Use camelCase for local variables and parameters.
- **Braces:** Place opening braces on a new line.
- **Comments:** Use `//` for single-line comments and `/* ... */` for multi-line comments.
- **File Organization:** Each class should be in its own `.cs` file, named after the class.
- **Unity Conventions:** Follow Unity's recommended practices for MonoBehaviour scripts and asset organization.

## Agent Rules (Cursor/Copilot)
No Cursor or Copilot rules or instructions were found in this repository.

## Getting Started
1. Open the project in Unity Editor (recommended version: check `ProjectSettings/ProjectVersion.txt`).
2. To build, use `File > Build Settings` in the Editor.
3. To run tests, use `Window > General > Test Runner`.
4. Follow the code style guidelines above for new contributions.

---
This file summarizes the build, test, and style conventions for agents and contributors. Update this file if new scripts, rules, or guidelines are added.
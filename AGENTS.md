# Guidelines for Coding Agents

This repository contains a small WPF application located in the `RAMDrive Runner` directory.

## Expectations

- Keep all source files within the existing directory structure.
- After making changes run:
  
  ```bash
  dotnet build "RAMDrive Runner/RAMDrive Runner.csproj" -nologo
  ```

  If the build fails because the `dotnet` CLI is unavailable, mention that in the testing section of your pull request.
- Update `README.md` when behavior or usage changes. The final line stating that the author relied on GPT-4 for WPF guidance should remain in the README.

## Pull Request Guidelines

- Summarize your changes clearly in the PR body.
- Include the build output or note the failure as described above.
- Leave the working tree clean with all changes committed.

# Agent Instructions

- Make sure to kill all other instances before start.
- Always test the code by building.
- When starting, always use command prompt so we can see what is going on.
- If a failure is found, make sure to document it in docs/lesson.md to avoid repeating the same mistake and respect the developer's preferences.
- Always plan tasks into todo.md and update it at each stage. Delete todo.md when all tasks are completed.

## Build and Test Workflow

1. **Kill existing processes first**
   ```
   Stop-Process -Name MyAutocount -Force -ErrorAction SilentlyContinue
   ```

2. **Build the solution**
   ```
   dotnet build MyAutocount.sln -c Release
   ```

3. **Start the server**
   ```
   Start-Process "C:\gcr\MyAutocount-master\MyAutocount\bin\Release\net48\MyAutocount.exe"
   ```

4. **Run tests**
   ```
   .\test_e2e_all.ps1 -SkipServerStart
   ```

5. **Repeat**: Build → Test → Fix until all tests pass

## Task Management

- If cannot complete a fix, update `todo.md` with remaining work
- Can spawn sub-agents for complex tasks by updating `todo.md` with specific sub-tasks
- Always mark task status: `pending`, `in_progress`, or `completed`
- Delete `todo.md` only when ALL tasks are completed

## Release Workflow (run automatically when any task is done)

Do NOT wait to be asked — when a task is completed and verified, release it:

1. **Verify**: build has 0 errors and `.\test_e2e_all.ps1 -SkipServerStart` is fully green.
2. **Document**: append any new lesson/finding to `docs/lesson.md`.
3. **Commit & push** (only with real changes — never empty commits):
   ```
   git add <changed files>
   git commit -m "<what changed>"
   git push origin main
   ```
   Pushing to `main` triggers the CI release workflow (`.github/workflows/release.yml`), which auto-bumps the patch version and publishes a GitHub release with zip (`v1.0.x` keeps increasing on every push — no manual version edits).
4. **Zip locally** with the NEW version: `git fetch --tags`, next version = max `v*` tag + 1, then
   ```
   dotnet build GCR-autocount-api.sln -c Release -p:Version=<next>
   ```
   Restage `MyAutocount\bin\Release\net48` into a clean folder, EXCLUDING `logs`, `publish`, `*.pdb`, `fr.zip`, and the real `settings.json`; ADD `settings.example.json` (placeholder values), `version.txt` (`<next>`), `README.txt`. Zip as `releases\GCR-autocount-api-v<next>.zip`. Never commit zips (`releases/` is git-ignored) and never ship real credentials.
5. **Report**: new version number + zip path + e2e result.

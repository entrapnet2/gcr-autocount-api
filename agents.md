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

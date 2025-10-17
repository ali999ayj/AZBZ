# Attendance Management System

This repository contains a .NET 8 web API and static front-end for managing university attendance.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) installed on your machine.

## Project layout
- `AttendanceApp.csproj` — main ASP.NET Core project.
- `AttendanceApp.sln` — convenience solution file that references the project.
- `wwwroot/` — static front-end assets served by the API.

## Running the application
1. Open PowerShell and navigate to the folder that contains the solution. For example:
   ```powershell
   cd "C:\Users\hilla university\Downloads\AZBZ-codex-build-attendance-management-system (1)"
   ```
   Use quotes around the path because it contains spaces and parentheses.

2. Verify that the project files are present:
   ```powershell
   dir AttendanceApp.csproj
   dir AttendanceApp.sln
   ```
   If the files are listed, you are in the correct directory. If not, navigate into the extracted folder that contains them.

3. Restore packages (optional because `dotnet run` performs this automatically):
   ```powershell
   dotnet restore AttendanceApp.sln
   ```

4. Build the project (optional):
   ```powershell
   dotnet build AttendanceApp.sln
   ```

5. Run the API on port 5000:
   ```powershell
   dotnet run --project AttendanceApp.csproj --urls "http://localhost:5000"
   ```

6. Once the application starts, open Swagger at [http://localhost:5000/swagger](http://localhost:5000/swagger) to explore the API. The static front-end is available at [http://localhost:5000](http://localhost:5000).

## Database
The app uses SQLite and automatically creates `attendance.db` in the project directory on first run.

## Useful commands
- `dotnet clean AttendanceApp.sln`
- `dotnet test` (no tests yet, but the command will succeed once tests are added)

## Troubleshooting
- If you see `MSBUILD : error MSB1009: Project file does not exist`, confirm that `AttendanceApp.csproj` is in the current directory using `dir AttendanceApp.csproj`.
- Ensure that the file is not blocked by antivirus or permissions after extracting the ZIP.
- Re-extract the ZIP if files are missing or corrupted.


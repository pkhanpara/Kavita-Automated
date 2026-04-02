# Kavita Development Guidelines

## Build, Lint, and Test Commands

### Build Modes

The build system supports three execution modes for flexible development workflows:

**1. Full Build (Default)**
```bash
# Build all projects and UI
./build.sh

# Build with specific runtime (e.g., linux-x64, win-x64)
./build.sh linux-x64
```

**2. Runtime-Only Mode**
```bash
# Build only the specified runtime without rebuilding UI
./build.sh --runtime-only linux-x64

# Build only linux-arm64 runtime
./build.sh --runtime-only linux-arm64

Make sure there are no errors in the output. Ignore warnings unless told so. 

```

**3. UI-Only Mode**
```bash
# Build only the UI without packaging runtimes
./build.sh --ui-only
```

### Running Tests (Single Test Focus)

### Running Tests (Single Test Focus)

**C# Tests (xUnit):**
```bash
# Run all tests
dotnet test Kavita.sln

# Run tests in specific project
dotnet test Kavita.Common.Tests/Kavita.Common.Tests.csproj

# Run a single test class
dotnet test --filter "FullyQualifiedName~CronConverterTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~ConvertTest"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in Release configuration
dotnet test -c Release
```

**UI Tests (Angular):**
```bash
# Navigate to UI directory
cd UI/Web

# Install dependencies
npm ci

# Run lint
npm run lint

# Run production build
npm run prod

# Run development server
npm run start
```

### Test Commands Quick Reference
- **Quick test run:** `dotnet test --no-build`
- **Watch mode:** `dotnet test --watch`
- **Output test results:** `dotnet test --logger "console;verbosity=detailed"`

## Code Style Guidelines

### C# Coding Standards

**Naming Conventions:**
- Classes: PascalCase (e.g., `CronConverter`, `BookImportService`)
- Methods: PascalCase (e.g., `ConvertToCronNotation`, `ProcessBookData`)
- Properties: PascalCase with clear names (e.g., `IsEnabled`, `MaxRetries`)
- Private fields: camelCase with underscore prefix (e.g., `_imageService`, `_config`)
- Interfaces: IPrefix naming (e.g., `IService`, `IRepository`)
- Constants: PascalCase, all caps for values (e.g., `MAX_RETRY_COUNT`)

**Import Organization:**
- Group imports by namespace (framework, project, third-party)
- Use meaningful aliases for long namespace paths
- Prefer explicit imports over `using static` for clarity

**Code Formatting:**
- Use 4-space indentation for C# files
- Place opening braces on new line for methods and control statements
- Add newline before return statements in complex logic blocks
- Always use braces for if/else statements except simple one-liners
- Maintain spaces around operators: `var a = b + c` not `var a=b+c`

**Example Structure:**
```csharp
# Simple one-liner without braces
if (isValid) return;

# Complex logic with braces
if (user != null)
{
    _imageService.Resize(image);
    return user;
}

# Return with newline above
var result = ProcessData(input);
_logger.LogInformation("Processed: {Item}", result);

return result;
```

**Error Handling:**
- Use `var` for type inference when type is obvious
- Implement proper exception handling with specific exception types
- Use async/await for I/O operations
- Validate inputs early with meaningful error messages
- Follow CA1873 guideline (logging never disabled)

**Nullable Reference Types:**
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use nullable annotations for method parameters and return types
- Handle null cases explicitly in public APIs

### TypeScript/Angular Standards

**Import Organization:**
- Use single quotes for imports
- Organize imports: Angular core, third-party libraries, local modules
- Prefer barrel exports for module organization

**Code Formatting:**
- Use 2-space indentation for TypeScript files
- Apply single quotes for strings
- Use implicit returns where possible
- Follow Angular style guide for component structure

**Type Safety:**
- Leverage TypeScript strict mode
- Define interfaces for data contracts
- Use type guards for runtime type checking

### File Structure

**Project Layout:**
```
Kavita/
├── Kavita.API/          # API layer and controllers
├── Kavita.Common/       # Shared utilities and helpers
├── Kavita.Database/     # Data access and EF Core
├── Kavita.Models/       # Domain models and DTOs
├── Kavita.Services/     # Business logic and services
├── Kavita.Server/       # Application host and startup
├── UI/Web/              # Angular frontend
├── Kavita.Common.Tests/
├── Kavita.Database.Tests/
├── Kavita.Models.Tests/
├── Kavita.Services.Tests/
├── Kavita.Server.Tests/
└── Kavita.Benchmark/
```

### Editor Configuration

**EditorConfig Settings:**
- UTF-8 encoding with Unix line endings
- Trim trailing whitespace
- Insert final newline
- `.ts` files: 2-space indentation with single quotes
- `.cs` files: 4-space indentation
- SonarLint rules enabled (S1075, CA1873)

**Visual Studio Code:**
- Enable better-comments tags (note, ?, //, todo, *)
- Word wrap off for diff view
- Configure C# and TypeScript extensions

### Git Workflow

**Branch Strategy:**
- Target `develop` branch for all features
- Use feature branch naming: `feature/component-name`
- Use bugfix branch naming: `bugfix/issue-description`
- Rebase instead of merge for cleaner history

**Commit Guidelines:**
- Use meaningful commit messages
- Keep commits focused on single concerns
- Maintain *nix line endings across platforms
- One feature or fix per pull request

### Documentation

**Resources:**
- API Documentation: http://localhost:5000/swagger/index.html
- Wiki: https://wiki.kavitareader.com/
- OpenAPI Spec: `openapi.json`

**Required Tools:**
- .NET 10.0.0+ SDK
- Node.js 18.13+ with npm
- Angular CLI globally installed
- Swagger CLI for API documentation

### Continuous Integration

**GitHub Workflows:**
- Build and test on push/PR
- Code quality analysis with CodeQL
- Automated documentation generation
- Release pipeline with versioning

**Quality Gates:**
- Code coverage targets maintained
- Static analysis rules enforced
- Performance benchmarks tracked

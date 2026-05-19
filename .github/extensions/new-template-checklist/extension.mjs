import { joinSession } from "@github/copilot-sdk/extension";

const CHECKLIST = `
## New Template Checklist

When adding a new \`dotnet new\` template to this repo, make sure you have:

### Template structure
- [ ] Created \`templates/<Category>/<Name>/\` with all template files
- [ ] Added \`.template.config/template.json\` with short name, parameters, and source modifiers
- [ ] Added \`global.json\` (SDK version + MTP runner config)
- [ ] Added \`Directory.Build.props\` (LangVersion 14, net10.0, NuGet metadata)
- [ ] Added \`Directory.Packages.props\` (central package management, correct package versions)
- [ ] Added \`.editorconfig\` (end_of_line = lf, insert_final_newline = true)
- [ ] Added \`.gitignore\`
- [ ] Added \`README.md\`
- [ ] Added \`NuGet.config\`

### CI workflows (in \`.github/workflows/\`)
- [ ] \`build-and-test.yml\` with: checkout@v6, dotnet tool restore, format check, build, test (MTP flags), TRX playlist generation, upload-artifact@v7
- [ ] \`deploy.yml\` with permissions: contents: read, checkout@v6
- [ ] \`copilot-setup-steps.yml\` with checkout@v6
- [ ] \`.config/dotnet-tools.json\` with trx-to-vsplaylist 1.3.0

### Root-level wiring
- [ ] Added template CI matrix job to \`.github/workflows/build.yml\` (covering all parameter variants)
- [ ] Added template to \`all-tests\` gate job needs list in \`.github/workflows/build.yml\`
- [ ] Added template to \`.github/dependabot.yml\`:
      - \`github-actions\` ecosystem (the template's \`.github/workflows\` directory)
      - \`nuget\` ecosystem (the template root directory)
      - \`dotnet-sdk\` ecosystem (the template root directory)
- [ ] Added template row to root \`README.md\` table and repo layout section

### Validation
- [ ] Run \`dotnet pack --configuration Release -o .\` at repo root - builds clean
- [ ] Install and generate each variant: \`dotnet new install <pkg>.nupkg --force\`
- [ ] Generated project builds and tests pass for all matrix variants
- [ ] \`dotnet format --verify-no-changes\` passes on ubuntu (lf line endings)
`;

await joinSession({
    tools: [
        {
            name: "new_template_checklist",
            description:
                "Show the checklist of steps required when adding a new dotnet new template to this repository. Call this whenever starting work on a new template.",
            parameters: {
                type: "object",
                properties: {},
                required: [],
            },
            handler: async () => CHECKLIST,
        },
    ],
});

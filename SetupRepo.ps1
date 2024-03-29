# This script leverages the GitHub CLI.
# Ensure you have installed and updated it by following the directions here: https://github.com/cli/cli
# In windows you can run `winget install GitHub.cli` to install it and then run `gh login` to authenticate to GitHub

# This script assumes it is being executed from the root of a repository 

# Setup acceptable merge types
gh repo edit --enable-merge-commit=false
gh repo edit --enable-squash-merge
gh repo edit --enable-rebase-merge

# Enable PR Auto Merge
 gh repo edit --enable-auto-merge

 #TODO: Setup branch protection rule for default branch
 #TODO: Set default commit message presented when merging a pull request with squash.
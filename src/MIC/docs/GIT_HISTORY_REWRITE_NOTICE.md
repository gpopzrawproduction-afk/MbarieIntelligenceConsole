 # Git History Rewrite Notice

 Date: 2026-02-05

 Summary:

 - A private key file (artifacts/micdev.pfx) was accidentally committed and has been removed from the repository history.
 - A backup tag was created before the purge: pre-purge-20260205081126.
 - The repository was repacked and force-pushed with rewritten history to remove the private key.

 Important actions for team members:

 1. Fresh clone (recommended):

 ```bash
 git clone https://github.com/gpopzrawproduction-afk/MbarieIntelligenceConsole.git
 ```

 2. If you must reuse an existing local clone (advanced):

 ```bash
 git fetch origin
 git reset --hard origin/main
 git clean -fdx
 ```

 Notes:

 - Treat the removed certificate as compromised and rotate/revoke it immediately.
 - Do not attempt to recover the private key from backups — generate a new certificate for signing.
 - The .gitignore has been updated to prevent committing private keys and sensitive configuration files. Ensure you do not add secrets to the repo.

 If you need assistance re-cloning or validating your local workspace, contact the repository maintainers.

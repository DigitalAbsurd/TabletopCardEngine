#!/bin/bash
set -euo pipefail

# Verify that a release tag is signed by an SSH key in the allowed_signers file.
#
# Usage:
#   verify-tag.sh <tag> [allowed-signers-file]
#
# Arguments:
#   <tag>                       The tag to verify (e.g., v1.0.0).
#   [allowed-signers-file]      Optional path to an allowed-signers file. If not provided,
#                               it is fetched from origin/main:.github/allowed_signers.
#
# Exit codes:
#   0  Tag is signed and valid.
#   1  Tag is invalid, unsigned, lightweight, or signed by an unknown key.

if [[ $# -lt 1 || $# -gt 2 ]]; then
	echo "Usage: $0 <tag> [allowed-signers-file]" >&2
	exit 1
fi

TAG="$1"
SIGNERS_FILE="${2:-}"
LOCAL_TEST_MODE=false

# If a signers file is provided as second argument, we're in local test mode
if [[ -n "$SIGNERS_FILE" ]]; then
	LOCAL_TEST_MODE=true
fi

# Re-fetch the tag object to ensure we have the annotated tag and not a lightweight one
# (actions/checkout can leave a lightweight tag in place of the annotated one).
# Skip this step in local test mode since there's no remote origin.
if [[ "$LOCAL_TEST_MODE" == false ]]; then
	git fetch --force origin "refs/tags/$TAG:refs/tags/$TAG" >/dev/null 2>&1 || {
		echo "::error::Failed to fetch tag $TAG from origin." >&2
		exit 1
	}
fi

# Check that the tag is annotated, not lightweight.
tag_type=$(git cat-file -t "refs/tags/$TAG" 2>/dev/null || echo "")
if [[ "$tag_type" != "tag" ]]; then
	echo "::error::Tag $TAG is not an annotated tag." >&2
	exit 1
fi

# If no allowed-signers file was provided, fetch it from origin/main.
if [[ -z "$SIGNERS_FILE" ]]; then
	# Make sure we have origin/main.
	git fetch origin main:main >/dev/null 2>&1 || true

	# Create a temporary file and ensure it is cleaned up on exit.
	SIGNERS_FILE=$(mktemp)
	trap "rm -f '$SIGNERS_FILE'" EXIT

	git show origin/main:.github/allowed_signers >"$SIGNERS_FILE" || {
		echo "::error::Failed to fetch .github/allowed_signers from origin/main." >&2
		exit 1
	}
fi

# Verify the tag signature using SSH keys from the allowed_signers file.
if ! git -c gpg.ssh.allowedSignersFile="$SIGNERS_FILE" verify-tag "$TAG" 2>&1; then
	echo "::error::Tag $TAG signature verification failed." >&2
	exit 1
fi

# Print which key signed the tag by extracting it from the signature output.
echo "✓ Tag $TAG is signed and verified."
exit 0

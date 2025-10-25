#!/usr/bin/env bash
# Recursively remove UTF-8 BOMs from text files

# Directory to start from (default: current directory)
DIR="${1:-.}"

find "$DIR" -type f -name "*.dm" | while read -r file; do
    # Check if the file starts with a UTF-8 BOM
    if head -c 3 "$file" | grep -q $'^\xEF\xBB\xBF'; then
        echo "Removing BOM from: $file"
        # Use tail to skip the first 3 bytes and rewrite the file
        tail --bytes=+4 "$file" > "${file}.nobom" && mv "${file}.nobom" "$file"
    fi
done

echo "Done."

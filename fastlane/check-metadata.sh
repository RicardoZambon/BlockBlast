#!/usr/bin/env bash
# Checks the store listings in fastlane/metadata (master plan 4.3): every field within its store's limit, and no other
# game's name anywhere (Global Constraints). Run it from the repository root; the listing workflow runs it on changes.
set -euo pipefail
export LC_ALL=C.UTF-8   # count characters, not bytes
fail=0

check() {   # check FILE LIMIT [bytes]: characters, or bytes for Apple's keywords field
  local count unit=characters flag=-m
  [ "${3:-}" = bytes ] && unit=bytes && flag=-c
  count=$(printf %s "$(cat "$1")" | wc "$flag")   # $(...) drops the final newline, which the stores don't count
  if [ "$count" -gt "$2" ]; then
    echo "::error file=$1::$count $unit, over the limit of $2"
    fail=1
  else
    echo "ok  $count/$2 $unit  $1"
  fi
}

# App Store (fastlane deliver): one folder per language.
for dir in fastlane/metadata/*/; do
  [ "$(basename "$dir")" = android ] && continue
  check "${dir}name.txt" 30
  check "${dir}subtitle.txt" 30
  check "${dir}promotional_text.txt" 170
  check "${dir}keywords.txt" 100 bytes
  check "${dir}description.txt" 4000
done

# Google Play (fastlane supply).
for dir in fastlane/metadata/android/*/; do
  check "${dir}title.txt" 30
  check "${dir}short_description.txt" 80
  check "${dir}full_description.txt" 4000
done

if grep -rniE 'tetris|blast|1010|woodoku|bejeweled|candy crush' fastlane/metadata; then
  echo "::error::Another game's name is in the store listing: the Global Constraints forbid it."
  fail=1
fi

exit $fail

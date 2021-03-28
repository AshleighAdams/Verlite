#!/bin/bash
# tag-filter.sh
body="$(git tag -n999 --format='%(contents:body)' "$VERLITE_TAG")"
if [[ "$body" == *"#auto-tag"* ]]; then
	echo tag contains keyword
	exit 1
else
	echo tag does not contain keyword
	exit 0
fi